// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record class DocumentResponse(
    string Name,
    string ContentType,
    long Size,
    DateTimeOffset? LastModified,
    Uri Url,
    DocumentProcessingStatus Status,
    EmbeddingType EmbeddingType);
