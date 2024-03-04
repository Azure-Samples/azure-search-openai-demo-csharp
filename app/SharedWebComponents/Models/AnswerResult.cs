namespace SharedWebComponents.Models;

public readonly record struct AnswerResult<TRequest>(
    bool IsSuccessful,
    ApproachResponse? Response,
    Approach Approach,
    TRequest Request) where TRequest : ApproachRequest;
