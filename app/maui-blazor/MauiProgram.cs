namespace MauiBlazor;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

        builder.Services.AddHttpClient<ApiClient>(client =>
        {
            // TODO: Configure this to point to your deployed API. For example, https://MY_HOSTED_APP.example.azurecontainerapps.io/
            client.BaseAddress = new Uri("TODO");
        });
        builder.Services.AddScoped<OpenAIPromptQueue>();
        builder.Services.AddSingleton<ILocalStorageService, LocalStorageService>();
        builder.Services.AddSingleton<ISessionStorageService, SessionStorageService>();
        builder.Services.AddSingleton<ISpeechRecognitionService, SpeechRecognitionService>();
        builder.Services.AddSingleton<ISpeechSynthesisService, SpeechSynthesisService>();
        builder.Services.AddMudServices();

        return builder.Build();
	}
}
