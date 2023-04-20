// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class AnswerError
{
    [Parameter, EditorRequired] public required string Question { get; set; }
    [Parameter, EditorRequired] public required ApproachResponse Error { get; set; }
    [Parameter, EditorRequired] public required EventCallback<string> OnRetryClicked { get; set; }

    private async Task OnRetryClickedAsync()
    {
        if (OnRetryClicked.HasDelegate)
        {
            await OnRetryClicked.InvokeAsync(Question);
        }
    }
}
