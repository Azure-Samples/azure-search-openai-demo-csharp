// Copyright (c) Microsoft. All rights reserved.

namespace MauiBlazor.Models;

public readonly record struct UserQuestion(
    string Question,
    DateTime AskedOn);
