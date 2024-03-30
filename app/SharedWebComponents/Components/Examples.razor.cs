// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Components;

public sealed partial class Examples
{
    [Parameter, EditorRequired] public required string Message { get; set; }
    [Parameter, EditorRequired] public EventCallback<string> OnExampleClicked { get; set; }

    private string WhatIsIncluded { get; } = "List the upcoming developments in Atlanta";
    private string WhatIsPerfReview { get; } = "Show me properties that are 5-bed 4-bath and less than 0.5 million";
    private string WhatDoesPmDo { get; } = "Share some design ideas for my Kitchen";

    private async Task OnClickedAsync(string exampleText)
    {
        if (OnExampleClicked.HasDelegate)
        {
            await OnExampleClicked.InvokeAsync(exampleText);
        }
    }
}
