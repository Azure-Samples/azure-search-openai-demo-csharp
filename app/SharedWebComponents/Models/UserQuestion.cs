// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Models;

public readonly record struct UserQuestion(
    string Question,
    DateTime AskedOn);
