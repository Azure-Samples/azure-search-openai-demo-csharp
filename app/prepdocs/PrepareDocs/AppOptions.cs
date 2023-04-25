// Copyright (c) Microsoft. All rights reserved.

namespace PrepareDocs;

internal record class AppOptions(
    string Files,
    string? Category,
    bool SkipBlobs,
    string? StorageAccount,
    string? Container,
    string? StorageKey,
    string? TenantId,
    string? SearchService,
    string? Index,
    string? SearchKey,
    bool Remove,
    bool RemoveAll,
    string? FormRecognizerService,
    string? FormRecognizerKey,
    bool Verbose,
    IConsole Console) : AppConsole(Console);

internal record class AppConsole(IConsole Console);
