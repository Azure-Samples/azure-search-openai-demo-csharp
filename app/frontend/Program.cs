var builder = WebAssemblyHostBuilder.CreateDefault(args);

// load backend from embedded resource
var assembly = typeof(Program).Assembly;
var resourceName = "ClientApp.BackendUri";
using (Stream stream = assembly.GetManifestResourceStream(resourceName)!)
using (StreamReader reader = new StreamReader(stream))
{
    // and set environment variables
    var backendUri = await reader.ReadToEndAsync();
    Environment.SetEnvironmentVariable("BACKEND_URI", backendUri ?? "https://localhost:7181");
}

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection(nameof(AppSettings)));
builder.Services.AddHttpClient<ApiClient>((sp, client) =>
{
    var backendUri = builder.Configuration["BACKEND_URI"] ??
        Environment.GetEnvironmentVariable("BACKEND_URI");
    ArgumentNullException.ThrowIfNullOrEmpty(backendUri);

    client.BaseAddress = new Uri(backendUri);
});
builder.Services.AddScoped<OpenAIPromptQueue>();
builder.Services.AddLocalStorageServices();
builder.Services.AddSessionStorageServices();
builder.Services.AddSpeechSynthesisServices();
builder.Services.AddSpeechRecognitionServices();
builder.Services.AddMudServices();

await JSHost.ImportAsync(
    moduleName: nameof(JavaScriptModule),
    moduleUrl: $"../js/iframe.js?{Guid.NewGuid()}" /* cache bust */);

var host = builder.Build();
await host.RunAsync();
