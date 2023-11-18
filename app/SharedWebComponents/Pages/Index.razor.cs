// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Pages;

public sealed partial class Index : IDisposable
{
    private int _currentPrompt = 0;

    private readonly CancellationTokenSource _cancellation = new();
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(30));
    private readonly string[] _prompts = new[]
    {
        "The birth of generative AI, color pop",
        ".NET, pop art",
        "Computers evolving beyond humanity retro futurism",
        "Futuristic robots playing chess, Banksy art, color splash",
        "Artificial intelligence, geometric art, bright and vibrant",
        "Robot made of analog stereo equipment, digital art",
        "Steaming cup of coffee with text on side, pop art",
        "Futuristic scene with skyscrapers, hovercrafts and robots",
        "Robots playing chess, Banksy art",
        "Artificial intelligence, geometric art, bright and vivid",
         "Old 1950s computer on pick background, retro futurism"
    };
    private readonly Queue<Image> _images = new();

    [Inject]
    public required ApiClient Client { get; set; }

    protected override void OnInitialized()
    {
        _images.Enqueue(new("_content/SharedWebComponents/media/bing-create-0.jpg", "The birth of generative AI, color pop"));
        _images.Enqueue(new("_content/SharedWebComponents/media/bing-create-1.jpg", ".NET, pop art"));
        _images.Enqueue(new("_content/SharedWebComponents/media/bing-create-2.jpg", "Computers evolving beyond humanity retro futurism"));
        _images.Enqueue(new("_content/SharedWebComponents/media/bing-create-3.jpg", "Futuristic robots playing chess, Banksy art, color splash"));
        _images.Enqueue(new("_content/SharedWebComponents/media/bing-create-4.jpg", "Artificial intelligence, geometric art, bright and vibrant"));
        _images.Enqueue(new("_content/SharedWebComponents/media/bing-create-5.jpg", "Robot made of analog stereo equipment, digital art"));
        _images.Enqueue(new("_content/SharedWebComponents/media/bing-create-6.jpg", "Steaming cup of coffee with text on side, pop art"));
        _images.Enqueue(new("_content/SharedWebComponents/media/bing-create-7.jpg", "Futuristic scene with skyscrapers, hovercrafts and robots"));
        _images.Enqueue(new("_content/SharedWebComponents/media/bing-create-8.jpg", "Robots playing chess, Banksy art"));
        _images.Enqueue(new("_content/SharedWebComponents/media/bing-create-9.jpg", "Artificial intelligence, geometric art, bright and vivid"));
        _images.Enqueue(new("_content/SharedWebComponents/media/bing-create-10.jpg", "Old 1950s computer on pick background, retro futurism"));

        _ = UpdateImageAsync();
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
        _timer.Dispose();
    }

    private async Task UpdateImageAsync()
    {
        do
        {
            var prompt = _prompts[_currentPrompt++ % _prompts.Length];
            var images = await Client.RequestImageAsync(new PromptRequest { Prompt = prompt });
            if (images is { ImageUrls.Count: > 0 })
            {
                foreach (var image in images.ImageUrls)
                {
                    _ = _images.Dequeue();
                    _images.Enqueue(new Image(image.ToString(), prompt));
                }

                await InvokeAsync(StateHasChanged);
            }
        }
        while (await _timer.WaitForNextTickAsync(_cancellation.Token));
    }
}

internal readonly record struct Image(string Src, string Alt);
