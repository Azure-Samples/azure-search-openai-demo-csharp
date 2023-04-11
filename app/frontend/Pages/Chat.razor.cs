// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class Chat
{
    private string _userPrompt = "";
    private bool _isReceivingResponse = false;
    private string? _intermediateResponse = null;
    private readonly HashSet<string> _responses = new();
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .ConfigureNewLine("\n")
        .UseAdvancedExtensions()
        .UseEmojiAndSmiley()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    [Inject] public required IStringLocalizer<Chat> Localizer { get; set; }

    private string Prompt => Localizer[nameof(Prompt)];

    private string ChatTitle => Localizer[nameof(ChatTitle)];

    private string ChatPrompt => Localizer[nameof(ChatPrompt)];

    private string Ask => Localizer[nameof(Ask)];

    private void OnSendPrompt()
    {
    }
}
