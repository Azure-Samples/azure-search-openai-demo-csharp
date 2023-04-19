// Copyright (c) Microsoft. All rights reserved.

var builder = WebApplication.CreateBuilder(args);

// See: https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOutputCache();

builder.AddCrossOriginResourceSharing();
builder.AddAzureServices();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseOutputCache();
app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapApi();

app.Run();
