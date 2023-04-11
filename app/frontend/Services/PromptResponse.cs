// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Services;

public record PromptResponse(string Prompt, string Response, bool IsComplete = false);
