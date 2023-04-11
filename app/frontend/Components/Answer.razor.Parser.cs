// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class Answer
{
    internal HtmlParsedAnswer ParseAnswerToHtml(string answer)
    {
        var citations = new List<CitationDetails>();
        var followupQuestions = new HashSet<string>();

        var parsedAnswer = ReplacementRegex().Replace(answer, match =>
        {
            followupQuestions.Add(match.Value);
            return "";
        });
        
        parsedAnswer = parsedAnswer.Trim();

        var parts = SplitRegex().Split(parsedAnswer);

        var fragments = parts.Select((part, index) =>
        {
            if (index % 2 is 0)
            {
                return part;
            }
            else
            {
                var citationIndex = citations.Count + 1;
                var citation = new CitationDetails(part, citationIndex);
                citations.Add(citation);

                return $"""
                    <a class="sup-container"
                        title="{part}" href="api/content/{part}" target="_blank">
                        <sup>{citationIndex}</sup>
                    </a>
                    """;
            }
        });

        return new HtmlParsedAnswer(
            string.Join("", fragments),
            citations,
            followupQuestions);
    }

    [GeneratedRegex("<<([^>>]+)>>", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex ReplacementRegex();
    
    [GeneratedRegex("\\[[^\\]]+\\]", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex SplitRegex();
}

internal readonly record struct HtmlParsedAnswer(
    string AnswerHtml,
    List<CitationDetails> Citations,
    HashSet<string> FollowupQuestions);
