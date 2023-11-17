// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Models;

public record class AzureCulture
{
    public string Name { get; set; } = null!;
    public string NativeName { get; set; } = null!;
    public LanguageDirection Dir { get; set; }
}
