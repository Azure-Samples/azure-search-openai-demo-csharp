// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Services;

public interface IPdfViewer
{
    Task ShowDocument(string name, string url);
}
