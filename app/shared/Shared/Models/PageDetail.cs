// Copyright (c) Microsoft. All rights reserved.

namespace Shared.Models;

public readonly record struct PageDetail(
    int Index,
    int Offset,
    string Text);
