// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Models;

public record AskRequest(
    string Question,
    Approaches Approach,
    AskRequestOverrides Overrides);
