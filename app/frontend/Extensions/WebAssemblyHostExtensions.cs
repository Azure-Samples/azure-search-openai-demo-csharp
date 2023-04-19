// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Extensions;

internal static class WebAssemblyHostExtensions
{
    internal static WebAssemblyHost DetectClientCulture(this WebAssemblyHost host)
    {
        var localStorage = host.Services.GetRequiredService<ILocalStorageService>();
        var clientCulture = localStorage.GetItem<string>(StorageKeys.ClientCulture);
        clientCulture ??= "en";

        CultureInfo culture = new(clientCulture);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        return host;
    }
}
