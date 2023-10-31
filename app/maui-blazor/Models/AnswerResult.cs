// Copyright (c) Microsoft. All rights reserved.

namespace MauiBlazor.Models;

public readonly record struct AnswerResult<TRequest>(
    bool IsSuccessful,
    ApproachResponse? Response,
    Approach Approach,
    TRequest Request) where TRequest : ApproachRequest;
