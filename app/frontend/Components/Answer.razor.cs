// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class Answer
{
    [Inject] public required IStringLocalizer<Answer> Localizer { get; set; }

    private string ShowThoughts => Localizer[nameof(ShowThoughts)];
    private string ShowContent => Localizer[nameof(ShowContent)];

    private void OnShowThoughtsClicked() { }
    private void OnShowContentClicked() { }
}
