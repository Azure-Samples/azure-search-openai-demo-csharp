// Copyright (c) Microsoft. All rights reserved.

namespace PrepareDocs;

internal record class AppOptions(
    string Files,
    string? Category,
    bool SkipBlobs,
    string? StorageAccount,
    string? Container,
    string? TenantId,
    string? SearchService,
    string? Index,
    bool Remove,
    bool RemoveAll,
    string? FormRecognizerService,
    bool Verbose,
    IConsole Console) : AppConsole(Console);

internal record class AppConsole(IConsole Console);
