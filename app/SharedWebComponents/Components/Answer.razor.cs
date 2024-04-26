// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Components;

public sealed partial class Answer
{
    [Parameter, EditorRequired] public required ResponseChoice Retort { get; set; }
    [Parameter, EditorRequired] public required EventCallback<string> FollowupQuestionClicked { get; set; }

    [Inject] public required IPdfViewer PdfViewer { get; set; }

    private HtmlParsedAnswer? _parsedAnswer;

    protected override void OnParametersSet()
    {
        _parsedAnswer = ParseAnswerToHtml(
            Retort.Message.Content, Retort.CitationBaseUrl);

        base.OnParametersSet();
    }

    private async Task OnAskFollowupAsync(string followupQuestion)
    {
        if (FollowupQuestionClicked.HasDelegate)
        {
            await FollowupQuestionClicked.InvokeAsync(followupQuestion);
        }
    }
    private ValueTask OnShowCitationAsync(CitationDetails citation) => PdfViewer.ShowDocumentAsync(citation.Name, citation.BaseUrl);

    private MarkupString RemoveLeadingAndTrailingLineBreaks(string input) => (MarkupString)HtmlLineBreakRegex().Replace(input, "");

    [GeneratedRegex("^(\\s*<br\\s*/?>\\s*)+|(\\s*<br\\s*/?>\\s*)+$", RegexOptions.Multiline)]
    private static partial Regex HtmlLineBreakRegex();
}
