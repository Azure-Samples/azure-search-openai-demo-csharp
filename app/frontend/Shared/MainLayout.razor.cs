// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Shared;

public sealed partial class MainLayout
{
    private readonly MudTheme _theme = new();
    private bool _drawerOpen = true;
    private bool _settingsOpen = false;

    private bool _isDarkTheme
    {
        get => LocalStorage.GetItem<bool>(StorageKeys.PrefersDarkThemeKey);
        set => LocalStorage.SetItem<bool>(StorageKeys.PrefersDarkThemeKey, value);
    }

    private bool _isRightToLeft =>
        Thread.CurrentThread.CurrentUICulture is { TextInfo.IsRightToLeft: true };

    [Inject] public required ILocalStorageService LocalStorage { get; set; }
    [Inject] public required IDialogService Dialog { get; set; }
    [Inject] public required IStringLocalizer<MainLayout> Localizer { get; set; }

    private string SelectLanguageTitle => Localizer[nameof(SelectLanguageTitle)];
    private string SwitchToDarkTheme => Localizer[nameof(SwitchToDarkTheme)];
    private string SwitchToLightTheme => Localizer[nameof(SwitchToLightTheme)];
    private string ToggleNavBar => Localizer[nameof(ToggleNavBar)];
    private string VisitGitHubRepository => Localizer[nameof(VisitGitHubRepository)];

    private void ShowCultureDialog() => Dialog.Show<CultureDialog>(SelectLanguageTitle);

    private void DrawerToggle() => _drawerOpen = !_drawerOpen;

    private void OnToggledChanged() => _isDarkTheme = !_isDarkTheme;
}
