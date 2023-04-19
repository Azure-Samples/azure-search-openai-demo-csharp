// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class AnswerError
{
    [Parameter, EditorRequired] public required string Question { get; set; }
    [Parameter, EditorRequired] public required ApproachResponse Error { get; set; }
    [Parameter, EditorRequired] public required EventCallback<string> OnRetryClicked { get; set; }

    [Inject] public required IStringLocalizer<AnswerError> Localizer { get; set; }

    private string Retry => Localizer[nameof(Retry)];

    private async Task OnRetryClickedAsync()
    {
        if (OnRetryClicked.HasDelegate)
        {
            await OnRetryClicked.InvokeAsync(Question);
        }
    }
}
