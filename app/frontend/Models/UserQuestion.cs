// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Models;

public readonly record struct UserQuestion(
    string Question,
    DateTime AskedOn);
