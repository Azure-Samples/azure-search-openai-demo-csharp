// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class PdfViewerDialog
{
    [Parameter] public required string Title { get; set; }

    [CascadingParameter] public required MudDialogInstance Dialog { get; set; }

    private void OnCloseClick() => Dialog.Close(DialogResult.Ok(true));
}
