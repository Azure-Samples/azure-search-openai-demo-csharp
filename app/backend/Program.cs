// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.ConfigureAzureKeyVault();

// See: https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOutputCache();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddCrossOriginResourceSharing();
builder.Services.AddAzureServices();
builder.Services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN-HEADER"; options.FormFieldName = "X-CSRF-TOKEN-FORM"; });
builder.Services.AddHttpClient();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    static string? GetEnvVar(string key) => Environment.GetEnvironmentVariable(key);

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        var name = builder.Configuration["AzureRedisCacheName"] +
            ".redis.cache.windows.net";
        var key = builder.Configuration["AzureRedisCachePrimaryKey"];
        var ssl = "true";


        if (GetEnvVar("REDIS_HOST") is string redisHost)
        {
            name = $"{redisHost}:{GetEnvVar("REDIS_PORT")}";
            key = GetEnvVar("REDIS_PASSWORD");
            ssl = "false";
        }

        if (GetEnvVar("AZURE_REDIS_HOST") is string azureRedisHost)
        {
            name = $"{azureRedisHost}:{GetEnvVar("AZURE_REDIS_PORT")}";
            key = GetEnvVar("AZURE_REDIS_PASSWORD");
            ssl = "false";
        }

        options.Configuration = $"""
            {name},abortConnect=false,ssl={ssl},allowAdmin=true,password={key}
            """;
        options.InstanceName = "content";

        
    });

    // set application telemetry
    if (GetEnvVar("APPLICATIONINSIGHTS_CONNECTION_STRING") is string appInsightsConnectionString && !string.IsNullOrEmpty(appInsightsConnectionString))
    {
        builder.Services.AddApplicationInsightsTelemetry((option) =>
        {
            option.ConnectionString = appInsightsConnectionString;
        });
    }
}

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseOutputCache();
app.UseRouting();
app.UseStaticFiles();
app.UseCors();
app.UseBlazorFrameworkFiles();
app.UseAntiforgery();
app.MapRazorPages();
app.MapControllers();

app.Use(next => context =>
{
    var antiforgery = app.Services.GetRequiredService<IAntiforgery>();
    var tokens = antiforgery.GetAndStoreTokens(context);
    context.Response.Cookies.Append("XSRF-TOKEN", tokens?.RequestToken ?? string.Empty, new CookieOptions() { HttpOnly = false });
    return next(context);
});
app.MapFallbackToFile("index.html");

app.MapApi();

app.Run();
