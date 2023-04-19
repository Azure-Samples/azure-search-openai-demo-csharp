// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class Index
{
    private readonly string[] _images = Enumerable.Range(0, 10)
        .Select(i => $"media/bing-generated-{i}.jpg")
        .ToArray();

    [Inject] public required IStringLocalizer<Index> Localizer { get; set; }

    private string HomeTitle => Localizer[nameof(HomeTitle)];
    private string BingImageText => Localizer[nameof(BingImageText)];
    private string BingImageCreatorLinkText => Localizer[nameof(BingImageCreatorLinkText)];
    private string AzureSdkGitHubLinkTitle => Localizer[nameof(AzureSdkGitHubLinkTitle)];
    private string NuGetLinkAzureOpenAI => Localizer[nameof(NuGetLinkAzureOpenAI)];
    private string MicrosoftLearnContentLinkTitle => Localizer[nameof(MicrosoftLearnContentLinkTitle)];
    private string MudBlazorLink => Localizer[nameof(MudBlazorLink)];
    private string MarkdigLink => Localizer[nameof(MarkdigLink)];
}
