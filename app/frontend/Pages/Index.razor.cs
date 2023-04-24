// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class Index
{
    private readonly Image[] _images = new Image[]
    {
        new("media/bing-generated-0.jpg", "The birth of generative AI, color pop"),
        new("media/bing-generated-1.jpg", ".NET, pop art"),
        new("media/bing-generated-2.jpg", "Computers evolving beyond humanity retro futurism"),
        new("media/bing-generated-3.jpg", "Futuristic robots playing chess, Banksy art, color splash"),
        new("media/bing-generated-4.jpg", "Artificial intelligence, geometric art, bright and vibrant"),
        new("media/bing-generated-5.jpg", "Robot made of analog stereo equipment, digital art"),
        new("media/bing-generated-6.jpg", "Steaming cup of coffee with text on side, pop art"),
        new("media/bing-generated-7.jpg", "Futuristic scene with skyscrapers, hovercrafts and robots"),
        new("media/bing-generated-8.jpg", "Robots playing chess, Banksy art"),
        new("media/bing-generated-9.jpg", "Artificial intelligence, geometric art, bright and vivid"),
        new("media/bing-generated-10.jpg", "Old 1950s computer on pick background, retro futurism")
    };
}

internal readonly record struct Image(string Src, string Alt);
