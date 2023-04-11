// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Models;

public record PromptResponse(string Prompt, string Response, bool IsComplete = false);
