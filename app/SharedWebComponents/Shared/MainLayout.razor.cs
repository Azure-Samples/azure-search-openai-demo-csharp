// Copyright (c) Microsoft. All rights reserved.

using System.Net.Http;
using System.Reflection.Metadata;
using System.Threading;
using Azure.Storage.Blobs;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using static MudBlazor.Colors;



namespace SharedWebComponents.Shared;

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
        get => LocalStorage.GetItem<bool?>(StorageKeys.PrefersReversedConversationSorting) ?? false;
        set => LocalStorage.SetItem<bool>(StorageKeys.PrefersReversedConversationSorting, value);
    }

    private bool _isRightToLeft =>
        Thread.CurrentThread.CurrentUICulture is { TextInfo.IsRightToLeft: true };

    [Inject] public required NavigationManager Nav { get; set; }
    [Inject] public required ILocalStorageService LocalStorage { get; set; }
    [Inject] public required IDialogService Dialog { get; set; }

    public RequestSettingsOverrides Settings2 { get; set; } = new();


    public List<string>? cList = null;


    [Inject]
    public required ApiClient Client { get; set; }

    // Store a cancelation token that will be used to cancel if the user disposes of this component.
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly HashSet<DocumentResponse> _documents = [];
    private readonly HashSet<string> _categories = [];
    private Task _getCategoriesTask = null!;
    private bool _isLoadingCategories = false;

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

    private void OnMenuClicked() => _drawerOpen = !_drawerOpen;

    private void OnThemeChanged() => _isDarkTheme = !_isDarkTheme;

    private void OnIsReversedChanged() => _isReversed = !_isReversed;

    public List<string> _items = new List<string>() { "Abbvie", "Agile", "BioHaven" };
    public string jsonResponse = string.Empty;

    public async Task<IEnumerable<string>> MySearchFuncAsync(string search)
    {
        if (string.IsNullOrEmpty(search))
        {
            return cList.AsEnumerable<string>();
        }
        return await Task.FromResult(cList.Where(x => x.Contains(search, StringComparison.OrdinalIgnoreCase)));
    }


    protected override async Task OnInitializedAsync()
    {
        Settings2.Overrides.ExcludeCategory = new List<string>();
        var httpClient = new HttpClient();
        //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        //httpClient.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        HttpRequestMessage request = new();
        request.RequestUri = new Uri("https://gptkb-r6lomx22dqabk.search.windows.net/indexes/gptkbindex/docs?api-version=2024-05-01-preview&facet=category,count:1000");
        request.Method = HttpMethod.Get;
      
      // FIXME : Doesn't work locally
        request.Headers.Add("api-key", Environment.GetEnvironmentVariable("SEARCH_SERVICE_KEY"));
      
 
        //request.Headers.Add("Access-Control-Allow-Origin", "*");
        
        //request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var endpoint = new Uri("https://gptkb-r6lomx22dqabk.search.windows.net/indexes/gptkbindex/docs?api-version=2024-05-01-preview&facet=category,count:1000");

        try
        {

            // Assuming GetCategoriesAsync returns a List<string> or similar collection of category names.
            
            HttpResponseMessage response = await httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                //jsonResponse = await response.Content.ReadAsStringAsync();

                // Assuming the API returns a JSON array of strings.
                //cList = JsonConvert.DeserializeObject<List<string>>(jsonResponse);
                jsonResponse = await response.Content.ReadAsStringAsync();
                var categories = JObject.Parse(jsonResponse)["@search.facets"]["category"]
                    .Select(c => c["value"].ToString())
                    .ToList();
                cList = categories;
            }
            else
            {
                // Handle error or throw an exception
                throw new HttpRequestException($"Failed to fetch categories. Status code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching categories: {ex.Message}");
            throw new Exception($"Error fetching categories: {ex.Message}");
        }

    }

}
