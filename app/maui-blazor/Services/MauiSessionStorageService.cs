// Copyright (c) Microsoft. All rights reserved.

namespace MauiBlazor.Services;

public class MauiSessionStorageService : ISessionStorageService
{
    private readonly ConcurrentDictionary<string, object> _storage = new(StringComparer.OrdinalIgnoreCase);

    public double Length => _storage.Count;

    public void Clear() => _storage.Clear();

    public TValue? GetItem<TValue>(string key, JsonSerializerOptions? options = null)
    {
        // Ignore these...
        _ = options;
        
        return _storage.TryGetValue(key, out var value)
            ? (TValue?) value
            : default;
    }

    public string? Key(double index) => default;

    public void RemoveItem(string key)
    {
        _storage.TryRemove(key, out var _);
    }

    public void SetItem<TValue>(string key, TValue value, JsonSerializerOptions? options = null)
    {
        // Ignore these...
        _ = options;
        
        _storage.AddOrUpdate(key, value!, (oldValue, newValue) => newValue);
    }
}
