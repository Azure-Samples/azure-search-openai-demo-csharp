// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Shared;

public sealed partial class MainLayout
{
    private readonly MudTheme _theme = new();
    private bool _drawerOpen = true;
    private bool _settingsOpen = false;
    private SettingsPanel? _settingsPanel;

    private bool _isDarkTheme
    {
        get => LocalStorage.GetItem<bool>(StorageKeys.PrefersDarkTheme);
        set => LocalStorage.SetItem<bool>(StorageKeys.PrefersDarkTheme, value);
    }

    private bool _isReversed
    {
        get => LocalStorage.GetItem<bool?>(StorageKeys.PrefersReversedConversationSorting) ?? true;
        set => LocalStorage.SetItem<bool>(StorageKeys.PrefersReversedConversationSorting, value);
    }

    private bool _isRightToLeft =>
        Thread.CurrentThread.CurrentUICulture is { TextInfo.IsRightToLeft: true };

    [Inject] public required NavigationManager Nav { get; set; }
    [Inject] public required ILocalStorageService LocalStorage { get; set; }
    [Inject] public required IDialogService Dialog { get; set; }
    [Inject] public required IStringLocalizer<MainLayout> Localizer { get; set; }

    private string SelectLanguageTitle => Localizer[nameof(SelectLanguageTitle)];
    private string SwitchToDarkTheme => Localizer[nameof(SwitchToDarkTheme)];
    private string SwitchToLightTheme => Localizer[nameof(SwitchToLightTheme)];
    private string ToggleNavBar => Localizer[nameof(ToggleNavBar)];
    private string VisitGitHubRepository => Localizer[nameof(VisitGitHubRepository)];
    private bool SettingsDisabled => new Uri(Nav.Uri).Segments.LastOrDefault() switch
    {
        "ask" or "chat" => false,
        _ => true
    };
    private bool SortDisabled => new Uri(Nav.Uri).Segments.LastOrDefault() switch
    {
        "voicechat" or "chat" => false,
        _ => true
    };

    private void ShowCultureDialog() => Dialog.Show<CultureDialog>($"🌐 {SelectLanguageTitle}");

    private void OnMenuClicked() => _drawerOpen = !_drawerOpen;
    private void OnThemeChanged() => _isDarkTheme = !_isDarkTheme;
    private void OnIsReversedChanged() => _isReversed = !_isReversed;
}
