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

		builder.UseMauiCommunityToolkit();

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		builder.Services.AddHttpClient<ApiClient>(client =>
		{
			// TODO: Configure this to point to your deployed backend API.
			// For example: https://MY_HOSTED_APP.example.azurecontainerapps.io/
			client.BaseAddress = new Uri("TODO");
		});
		builder.Services.AddScoped<OpenAIPromptQueue>();

		builder.Services.AddSingleton<ILocalStorageService, MauiLocalStorageService>();
		builder.Services.AddSingleton<ISessionStorageService, MauiSessionStorageService>();
		builder.Services.AddSingleton<ISpeechRecognitionService, MauiSpeechRecognitionService>();
		builder.Services.AddSingleton<ISpeechSynthesisService, MauiSpeechSynthesisService>();
        builder.Services.AddSingleton<ITextToSpeechPreferencesListener, MauiSpeechSynthesisService>();
		builder.Services.AddTransient<IPdfViewer, MauiPdfViewer>();

		builder.Services.AddMudServices();

		builder.Services.AddSingleton<IPreferences>(Preferences.Default);
		builder.Services.AddSingleton<ISpeechToText>(SpeechToText.Default);
		builder.Services.AddSingleton<ITextToSpeech>(TextToSpeech.Default);

		return builder.Build();
	}
}
