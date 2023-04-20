var builder = WebAssemblyHostBuilder.CreateDefault(args);

await LoadBackendUriFromResourceAsync();

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

static async ValueTask LoadBackendUriFromResourceAsync()
{
#if DEBUG
    // When debugging, use localhost.
    var backendUri = "https://localhost:7181";

    await ValueTask.CompletedTask;
#else
    // when in release mode, read from embedded resource.
    using Stream stream = typeof(Program).Assembly.GetManifestResourceStream("ClientApp.BackendUri")!;
    using StreamReader reader = new(stream);

    var backendUri = await reader.ReadToEndAsync();
#endif

    Environment.SetEnvironmentVariable(
        "BACKEND_URI", backendUri ?? "https://localhost:7181");
}
