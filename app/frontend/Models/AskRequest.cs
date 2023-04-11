// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Models;

public record AskRequest(
    string Question,
    Approach Approach,
    AskRequestOverrides Overrides);
