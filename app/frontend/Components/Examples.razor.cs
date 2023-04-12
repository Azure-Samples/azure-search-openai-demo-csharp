// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class Examples
{
    [Parameter, EditorRequired] public required string Message { get; set; }
    [Parameter, EditorRequired] public EventCallback<string> OnExampleClicked { get; set; }

    [Inject] public required IStringLocalizer<Examples> Localizer { get; set; }

    private string WhatsIncluded => Localizer[nameof(WhatsIncluded)];
    private string WhatsPerfReview => Localizer[nameof(WhatsPerfReview)];
    private string WhatsProductManagerDo => Localizer[nameof(WhatsProductManagerDo)];

    private async Task OnClickedAsync(string exampleText)
    {
        if (OnExampleClicked.HasDelegate)
        {
            await OnExampleClicked.InvokeAsync(exampleText);
        }
    }
}
