// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class Loading
{
    [Inject] public required IStringLocalizer<Loading> Localizer { get; set; }
    
    private string Busy => Localizer[nameof(Busy)];
}
