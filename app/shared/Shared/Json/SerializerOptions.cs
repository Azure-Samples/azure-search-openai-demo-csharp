// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;

namespace Shared.Json;

public static class SerializerOptions
{
    public static JsonSerializerOptions Default { get; } =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);
}
