// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Models;

public record RequestSettingsOverrides
{
    public Approach Approach { get; set; }
    public RequestOverrides Overrides { get; set; } = new();
}
