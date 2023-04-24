// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class Index
{
    private readonly Image[] _images = new Image[]
    {
        new("media/bing-create-0.jpg", "The birth of generative AI, color pop"),
        new("media/bing-create-1.jpg", ".NET, pop art"),
        new("media/bing-create-2.jpg", "Computers evolving beyond humanity retro futurism"),
        new("media/bing-create-3.jpg", "Futuristic robots playing chess, Banksy art, color splash"),
        new("media/bing-create-4.jpg", "Artificial intelligence, geometric art, bright and vibrant"),
        new("media/bing-create-5.jpg", "Robot made of analog stereo equipment, digital art"),
        new("media/bing-create-6.jpg", "Steaming cup of coffee with text on side, pop art"),
        new("media/bing-create-7.jpg", "Futuristic scene with skyscrapers, hovercrafts and robots"),
        new("media/bing-create-8.jpg", "Robots playing chess, Banksy art"),
        new("media/bing-create-9.jpg", "Artificial intelligence, geometric art, bright and vivid"),
        new("media/bing-create-10.jpg", "Old 1950s computer on pick background, retro futurism")
    };
}

internal readonly record struct Image(string Src, string Alt);
