// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Components;

public sealed partial class SupportingContent
{
    [Parameter, EditorRequired] public required SupportingContentRecord[] DataPoints { get; set; }

    [Parameter, EditorRequired] public required SupportingImageRecord[] Images { get; set; }

    private ParsedSupportingContentItem[] _supportingContent = [];

    protected override void OnParametersSet()
    {
        if (DataPoints is { Length: > 0 })
        {
            _supportingContent =
                DataPoints.Select(ParseSupportingContent).ToArray();
        }

        base.OnParametersSet();
    }
}
