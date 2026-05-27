using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class WildcardTests
{
    [Fact]
    public void IsFullMatch_EmptyPatternMatchesEmptyText()
    {
        Assert.True(Wildcard.IsFullMatch("", ""));
    }

    [Fact]
    public void IsFullMatch_EmptyPatternDoesNotMatchNonEmptyText()
    {
        Assert.False(Wildcard.IsFullMatch("a", ""));
    }

    [Fact]
    public void IsFullMatch_StarMatchesEmptyText()
    {
        Assert.True(Wildcard.IsFullMatch("", "*"));
    }

    [Fact]
    public void IsFullMatch_StarMatchesAnyText()
    {
        Assert.True(Wildcard.IsFullMatch("anything", "*"));
    }

    [Fact]
    public void IsFullMatch_QuestionMarkMatchesSingleCharacter()
    {
        Assert.True(Wildcard.IsFullMatch("a", "?"));
        Assert.False(Wildcard.IsFullMatch("", "?"));
        Assert.False(Wildcard.IsFullMatch("ab", "?"));
    }

    [Fact]
    public void IsFullMatch_LiteralPrefixStar()
    {
        Assert.True(Wildcard.IsFullMatch("a", "a*"));
        Assert.True(Wildcard.IsFullMatch("abc", "a*"));
        Assert.False(Wildcard.IsFullMatch("", "a*"));
        Assert.False(Wildcard.IsFullMatch("b", "a*"));
    }

    [Fact]
    public void IsFullMatch_IsCaseInsensitive()
    {
        Assert.True(Wildcard.IsFullMatch("ABC", "abc"));
        Assert.True(Wildcard.IsFullMatch("abc", "ABC"));
        Assert.True(Wildcard.IsFullMatch("AbC", "aBc"));
    }

    [Fact]
    public void IsFullMatch_TildeEscapesStar()
    {
        Assert.True(Wildcard.IsFullMatch("*", "~*"));
        Assert.False(Wildcard.IsFullMatch("a", "~*"));
    }

    [Fact]
    public void IsFullMatch_TildeEscapesQuestionMark()
    {
        Assert.True(Wildcard.IsFullMatch("?", "~?"));
        Assert.False(Wildcard.IsFullMatch("a", "~?"));
    }

    [Fact]
    public void IsFullMatch_TildeEscapesTilde()
    {
        Assert.True(Wildcard.IsFullMatch("~", "~~"));
        Assert.False(Wildcard.IsFullMatch("a", "~~"));
    }

    [Fact]
    public void IsFullMatch_MultipleWildcards()
    {
        Assert.True(Wildcard.IsFullMatch("aXbYc", "a*b*c"));
        Assert.True(Wildcard.IsFullMatch("abc", "a*b*c"));
        Assert.True(Wildcard.IsFullMatch("axyzbxc", "a*b*c"));
        Assert.False(Wildcard.IsFullMatch("axyc", "a*b*c"));
    }

    [Fact]
    public void IsFullMatch_MixedEscapeAndWildcards()
    {
        Assert.True(Wildcard.IsFullMatch("*X", "~*?"));
        Assert.True(Wildcard.IsFullMatch("*Z", "~*?"));
        Assert.False(Wildcard.IsFullMatch("aX", "~*?"));
        Assert.False(Wildcard.IsFullMatch("*", "~*?"));
    }

    [Fact]
    public void IsFullMatch_LiteralCharacters()
    {
        Assert.True(Wildcard.IsFullMatch("hello", "hello"));
        Assert.False(Wildcard.IsFullMatch("helloo", "hello"));
        Assert.False(Wildcard.IsFullMatch("hell", "hello"));
    }

    [Fact]
    public void IsFullMatch_DoesNotTreatRegexMetacharactersSpecially()
    {
        Assert.True(Wildcard.IsFullMatch("a.b", "a.b"));
        Assert.False(Wildcard.IsFullMatch("aXb", "a.b"));
        Assert.True(Wildcard.IsFullMatch("a+b", "a+b"));
        Assert.True(Wildcard.IsFullMatch("(a)", "(a)"));
    }

    [Fact]
    public void FindFirstIndex_FindsSubstring()
    {
        Assert.Equal(1, Wildcard.FindFirstIndex("hello", "ell", 0));
    }

    [Fact]
    public void FindFirstIndex_PatternAtStart()
    {
        Assert.Equal(0, Wildcard.FindFirstIndex("hello", "hel", 0));
        Assert.Equal(0, Wildcard.FindFirstIndex("hello", "*ell*", 0));
    }

    [Fact]
    public void FindFirstIndex_PatternAtEnd()
    {
        Assert.Equal(2, Wildcard.FindFirstIndex("hello", "llo", 0));
    }

    [Fact]
    public void FindFirstIndex_PatternNotFound()
    {
        Assert.Equal(-1, Wildcard.FindFirstIndex("hello", "xyz", 0));
    }

    [Fact]
    public void FindFirstIndex_RespectsStartIndex()
    {
        Assert.Equal(2, Wildcard.FindFirstIndex("ababab", "ab", 2));
        Assert.Equal(2, Wildcard.FindFirstIndex("ababab", "ab", 1));
        Assert.Equal(-1, Wildcard.FindFirstIndex("hello", "h", 1));
    }

    [Fact]
    public void FindFirstIndex_WithWildcard()
    {
        Assert.Equal(0, Wildcard.FindFirstIndex("hello world", "h*o", 0));
        Assert.Equal(1, Wildcard.FindFirstIndex("ahello", "h?llo", 0));
    }

    [Fact]
    public void FindFirstIndex_CaseInsensitive()
    {
        Assert.Equal(1, Wildcard.FindFirstIndex("aBCd", "bc", 0));
    }

    [Fact]
    public void FindFirstIndex_NegativeStartIndexTreatedAsZero()
    {
        Assert.Equal(0, Wildcard.FindFirstIndex("hello", "hel", -5));
    }

    [Fact]
    public void IsFullMatch_StarsOnlyMatchesAnything()
    {
        Assert.True(Wildcard.IsFullMatch("", "***"));
        Assert.True(Wildcard.IsFullMatch("abc", "***"));
    }
}
