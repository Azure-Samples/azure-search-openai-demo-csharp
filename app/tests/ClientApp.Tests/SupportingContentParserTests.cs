// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Tests;

#pragma warning disable CA1416 // Validate platform compatibility

public class SupportingContentParserTests
{
    public static IEnumerable<object[]> ParserInput
    {
        get
        {
            yield return new object[]
            {
                "test.pdf:blah blah",
                "test.pdf",
                "blah blah",
            };

            yield return new object[]
            {
                "sdp_corporate.pdf: this is the content that follows",
                "sdp_corporate.pdf",
                "this is the content that follows",
            };
        }
    }

    [Theory, MemberData(nameof(ParserInput))]
    public void SupportingContentCorrectlyParsesText(
        string supportingContent,
        string expectedTitle,
        string? expectedContent)
    {
        var actual = SupportingContent.ParseSupportingContent(supportingContent);
        var expected = new ParsedSupportingContentItem(expectedTitle, expectedContent);
        Assert.Equal(actual, expected);
    }
}


#pragma warning restore CA1416 // Validate platform compatibility
