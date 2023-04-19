// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Shared;

public sealed partial class NavMenu
{
    [Inject] public required IStringLocalizer<NavMenu> Localizer { get; set; }

    private string HomeNavLabel => Localizer[nameof(HomeNavLabel)];

    private string VoiceChatNavLabel => Localizer[nameof(VoiceChatNavLabel)];

    private string ChatNavLabel => Localizer[nameof(ChatNavLabel)];

    private string AskNavLabel => Localizer[nameof(AskNavLabel)];
}
