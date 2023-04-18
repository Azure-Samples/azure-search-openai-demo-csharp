var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection(nameof(AppSettings)));

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<ApiClient>((sp, client) =>
{
    var config = sp.GetService<IOptions<AppSettings>>();
    var backendUri = config?.Value?.BackendUri;
    ArgumentNullException.ThrowIfNullOrEmpty(backendUri);

    client.BaseAddress = new Uri(backendUri);
});
builder.Services.AddScoped<OpenAIPromptQueue>();
builder.Services.AddLocalStorageServices();
builder.Services.AddSessionStorageServices();
builder.Services.AddSpeechSynthesisServices();
builder.Services.AddSpeechRecognitionServices();
builder.Services.AddMudServices();
builder.Services.AddLocalization();
builder.Services.AddScoped<CultureService>();

await JSHost.ImportAsync(
    moduleName: nameof(JavaScriptModule),
    moduleUrl: $"../js/iframe.js?{Guid.NewGuid()}" /* cache bust */);

var host = builder.Build()
    .DetectClientCulture();

await host.RunAsync();
