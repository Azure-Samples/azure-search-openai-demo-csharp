// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public record class UploadDocumentsResponse(
    string[] UploadedFiles,
    string? Error = null)
{
    public bool IsSuccessful => this is
    {
        Error: null,
        UploadedFiles.Length: > 0
    };

    public static UploadDocumentsResponse FromError(string error) =>
        new([], error);
}
