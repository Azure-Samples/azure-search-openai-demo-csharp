// Copyright (c) Microsoft. All rights reserved.

namespace MauiBlazor.Services;

public class MauiLocalStorageService(IPreferences prefs) : ILocalStorageService
{
    public double Length => 0;

    public void Clear() => prefs.Clear();

    public TValue? GetItem<TValue>(string key, JsonSerializerOptions? options = null)
    {
        // if the type is a nullable, then use the underlying type for parsing
        if (Nullable.GetUnderlyingType(typeof(TValue)) is Type under)
        {
            var get = prefs.GetType().GetMethod("Get")!;
            var args = new object?[] { key, Activator.CreateInstance(under), default(string) };
            return (TValue?)get.MakeGenericMethod(under).Invoke(prefs, args);
        }
        else
        {
#pragma warning disable CS8604 // Possible null reference argument.
            return prefs.Get<TValue>(key, default);
#pragma warning restore CS8604 // Possible null reference argument.
        }
    }

    public string? Key(double index) => default;

    public void RemoveItem(string key) => prefs.Remove(key);

    public void SetItem<TValue>(string key, TValue value, JsonSerializerOptions? options = null)
    {
        // if the type is a nullable, then use the underlying type for parsing
        if (Nullable.GetUnderlyingType(typeof(TValue)) is Type under)
        {
            var set = prefs.GetType().GetMethod("Set")!;
            var args = new object?[] { key, value, default(string) };
            set.MakeGenericMethod(under).Invoke(prefs, args);
        }
        else
        {
            prefs.Set<TValue>(key, value);
        }
    }
}
