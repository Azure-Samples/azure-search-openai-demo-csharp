// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class Answer
{
    [Parameter, EditorRequired] public required ApproachResponse Retort { get; set; }
    [Parameter, EditorRequired] public required EventCallback<string> FollowupQuestionClicked { get; set; }

    [Inject] public required IDialogService Dialog { get; set; }

    [Inject] public required IStringLocalizer<Answer> Localizer { get; set; }

    private HtmlParsedAnswer? _parsedAnswer; 

    private string ShowThoughts => Localizer[nameof(ShowThoughts)];
    private string ShowContent => Localizer[nameof(ShowContent)];

    protected override void OnParametersSet()
    {
        _parsedAnswer = ParseAnswerToHtml(Retort.Answer);

        base.OnParametersSet();
    }

    private async Task OnAskFollowupAsync(string followupQuestion)
    {
        if (FollowupQuestionClicked.HasDelegate)
        {
            await FollowupQuestionClicked.InvokeAsync(followupQuestion);
        }
    }

    private void OnShowCitation(CitationDetails citation) =>
        Dialog.Show<PdfViewerDialog>(
            $"📄 {citation.Name}",
            new DialogParameters
            {
                [nameof(PdfViewerDialog.Title)] = citation.Name,
            },
            new DialogOptions
            {
                MaxWidth = MaxWidth.Large,
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = true
            });
}
