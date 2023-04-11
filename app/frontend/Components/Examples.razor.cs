// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class Examples
{
    [Inject] public required IStringLocalizer<Examples> Localizer { get; set; }

    private string WhatsIncluded => Localizer[nameof(WhatsIncluded)];
    private string WhatsPerfReview => Localizer[nameof(WhatsPerfReview)];
    private string WhatsProductManagerDo => Localizer[nameof(WhatsProductManagerDo)];
}
