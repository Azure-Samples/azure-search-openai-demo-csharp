// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record class ImageResponse(
    DateTimeOffset Created,
    List<Uri> ImageUrls);
