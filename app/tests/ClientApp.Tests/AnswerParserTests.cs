// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Tests;

#pragma warning disable CA1416 // Validate platform compatibility

public class AnswerParserTests
{
    public static IEnumerable<object[]> AnswerParserInput
    {
        get
        {
            yield return new object[]
            {
                "",
                "",
                new List<CitationDetails>(),
                new HashSet<string>()
            };

            yield return new object[]
            {
                """
                Northwind Health Plus does not guarantee the amount charged by an out-of-network provider, and the member is responsible for any balance remaining after the plan has paid its portion [Northwind_Health_Plus_Benefits_Details-70.pdf].
                Northwind Health Plus also pays for services that are not listed in the plan documents, if the health care provider determines that such services are medically necessary [Northwind_Health_Plus_Benefits_Details-102.pdf].
                This includes services that are not covered under the plan, such as experimental treatments and services for cosmetic purposes [Northwind_Health_Plus_Benefits_Details-25.pdf].
                """,
                """
                Northwind Health Plus does not guarantee the amount charged by an out-of-network provider, and the member is responsible for any balance remaining after the plan has paid its portion <sup class="mud-chip mud-chip-text mud-chip-color-info rounded pa-1">1</sup>.
                Northwind Health Plus also pays for services that are not listed in the plan documents, if the health care provider determines that such services are medically necessary <sup class="mud-chip mud-chip-text mud-chip-color-info rounded pa-1">2</sup>.
                This includes services that are not covered under the plan, such as experimental treatments and services for cosmetic purposes <sup class="mud-chip mud-chip-text mud-chip-color-info rounded pa-1">3</sup>.
                """,
                new List<CitationDetails>
                {
                    new CitationDetails("Northwind_Health_Plus_Benefits_Details-70.pdf", "content", 1),
                    new CitationDetails("Northwind_Health_Plus_Benefits_Details-102.pdf", "content", 2),
                    new CitationDetails("Northwind_Health_Plus_Benefits_Details-25.pdf", "content", 3)
                },
                new HashSet<string>()
            };
        }
    }

    [Theory, MemberData(nameof(AnswerParserInput))]
    public void AnswerCorrectlyParsesText(
        string answerText,
        string expectedHtml,
        List<CitationDetails> expectedCitations,
        HashSet<string> expectedFollowups)
    {
        var html = Answer.ParseAnswerToHtml(answerText, "content");
        Assert.Equal(expectedHtml, html.AnswerHtml);
        Assert.Equal(html.Citations, expectedCitations);
        Assert.Equal(html.FollowupQuestions, expectedFollowups);
    }
}

#pragma warning restore CA1416 // Validate platform compatibility
