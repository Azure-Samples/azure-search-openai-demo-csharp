// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class Index
{
    [Inject] public required IStringLocalizer<Index> Localizer { get; set; }

    private string HomeTitle => Localizer[nameof(HomeTitle)];

    private string BingImageText => Localizer[nameof(BingImageText)];

    private string BingImageCreatorLinkText => Localizer[nameof(BingImageCreatorLinkText)];

    private string AzureSdkGitHubLinkTitle => Localizer[nameof(AzureSdkGitHubLinkTitle)];

    private string NuGetLinkAzureOpenAI => Localizer[nameof(NuGetLinkAzureOpenAI)];

    private string MicrosoftLearnContentLinkTitle => Localizer[nameof(MicrosoftLearnContentLinkTitle)];

    private string MudBlazorLink => Localizer[nameof(MudBlazorLink)];

    private readonly HashSet<ImageAltPair> _images = new()
    {
        new("bing-generated-0.jpg", "Remote conference experience."),
        new("bing-generated-1.jpg", "Smile community friends."),
        new("bing-generated-2.jpg", "Drone flying fun."),
        new("bing-generated-3.jpg", "Camera crew."),
    };
}

internal readonly record struct ImageAltPair(string Image, string Alt);
