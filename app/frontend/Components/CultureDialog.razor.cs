// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class CultureDialog
{
    private static readonly Lazy<RegionInfo?> s_lazyRegion = new(
        () => TryNewRegionInfoCtor(CultureInfo.CurrentCulture.LCID));

    [Inject] public required NavigationManager Navigation { get; set; }
    [Inject] public required ILocalStorageService LocalStorage { get; set; }
    [Inject] public required ILogger<CultureDialog> Logger { get; set; }
    [Inject] public required IStringLocalizer<CultureDialog> Localizer { get; set; }
    [Inject] public required CultureService CultureService { get; set; }

    private string Select => Localizer[nameof(Select)];
    private string SelectLanguageLabel => Localizer[nameof(SelectLanguageLabel)];

    private IDictionary<CultureInfo, AzureCulture>? _supportedCultures;
    private CultureInfo _selectedCulture = CultureInfo.CurrentCulture;

    protected override async Task OnInitializedAsync() =>
        _supportedCultures = await CultureService.GetAvailableCulturesAsync();

    private void OnSaveCulture()
    {
        if (CultureInfo.CurrentCulture != _selectedCulture)
        {
            CultureInfo.CurrentCulture = _selectedCulture;
            LocalStorage.SetItem(StorageKeys.ClientCulture, _selectedCulture.Name);
            Navigation.NavigateTo(Navigation.Uri, forceLoad: true);
        }
    }

    private static string GetCultureTwoLetterRegionName(CultureInfo? culture = null) =>
        (culture is null ? s_lazyRegion.Value ?? TryNewRegionInfoCtor() : TryNewRegionInfoCtor(culture.LCID))
            ?.TwoLetterISORegionName
            ?.ToLowerInvariant() ?? "en";

    private static RegionInfo? TryNewRegionInfoCtor(int? lcid = null)
    {
        try
        {
            return new RegionInfo(lcid.GetValueOrDefault());
        }
        catch (ArgumentException ex)
        {
            _ = ex;
            return default!;
        }
    }
}
