using System;
using Xunit;

namespace FnisList.Tests;

public sealed class FnisTypeParserTests {
    [Theory]
    [InlineData("AnimVar", FnisAnimType.AnimVar, 7)]
    [InlineData("AAPrefix", FnisAnimType.Alternate, 8)]
    [InlineData("fuo", FnisAnimType.FurnitureOptimized, 3)]
    [InlineData("ofa", FnisAnimType.OffsetArm, 3)]
    [InlineData("ch", FnisAnimType.Chair, 2)]
    [InlineData("fu", FnisAnimType.Furniture, 2)]
    [InlineData("km", FnisAnimType.KillMove, 2)]
    [InlineData("pa", FnisAnimType.Paired, 2)]
    [InlineData("so", FnisAnimType.SequencedOptimized, 2)]
    [InlineData("+", FnisAnimType.SequencedContinued, 1)]
    [InlineData("b", FnisAnimType.Basic, 1)]
    [InlineData("o", FnisAnimType.AnimObject, 1)]
    [InlineData("s", FnisAnimType.Sequenced, 1)]
    public void ParsesType(string source, FnisAnimType expectedType, int expectedNextPos) {
        FnisListParseResult<FnisTypeSpan> result = FnisTypeParser.Parse(source.AsSpan(), new TextSpan(0, source.Length));

        Assert.True(result.IsSuccess);
        FnisTypeSpan value = result.Value;
        Assert.Equal(expectedType, value.Type);
        Assert.Equal(expectedNextPos, value.NextPos);
        Assert.Equal(expectedNextPos, result.Pos);
    }

    [Theory]
    [InlineData("  so", FnisAnimType.SequencedOptimized, 4)]
    [InlineData("  pa", FnisAnimType.Paired, 4)]
    [InlineData("\tpa", FnisAnimType.Paired, 3)]
    [InlineData("\nkm", FnisAnimType.KillMove, 3)]
    public void SkipsLeadingWhitespace(string source, FnisAnimType expectedType, int expectedNextPos) {
        FnisListParseResult<FnisTypeSpan> result = FnisTypeParser.Parse(source.AsSpan(), new TextSpan(0, source.Length));

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedType, result.Value.Type);
        Assert.Equal(expectedNextPos, result.Value.NextPos);
        Assert.Equal(expectedNextPos, result.Pos);
    }

    [Theory]
    [InlineData("b Attack")]
    [InlineData("so -a,ac,h")]
    [InlineData("pa Event")]
    [InlineData("km KillMove")]
    [InlineData("s Sequence")]
    [InlineData("+ Continuation")]
    public void StopsBeforeFollowingWhitespace(string source) {
        FnisListParseResult<FnisTypeSpan> result = FnisTypeParser.Parse(source.AsSpan(), new TextSpan(0, source.Length));

        Assert.True(result.IsSuccess);
        int expectedNextPos = source.IndexOf(' ');

        Assert.Equal(expectedNextPos, result.Value.NextPos);
        Assert.Equal(expectedNextPos, result.Pos);
    }

    [Theory]
    [InlineData("b,Attack", FnisAnimType.Basic, 1)]
    [InlineData("so,-a", FnisAnimType.SequencedOptimized, 2)]
    [InlineData("pa,Event", FnisAnimType.Paired, 2)]
    public void AcceptsCommaAsTokenDelimiter(string source, FnisAnimType expectedType, int expectedNextPos) {
        FnisListParseResult<FnisTypeSpan> result = FnisTypeParser.Parse(source.AsSpan(), new TextSpan(0, source.Length));

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedType, result.Value.Type);
        Assert.Equal(expectedNextPos, result.Value.NextPos);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void RejectsEmptyInputAsUnexpectedEnd(string source) {
        FnisListParseResult<FnisTypeSpan> result = FnisTypeParser.Parse(source.AsSpan(), new TextSpan(0, source.Length));

        Assert.True(result.IsFailure);
        Assert.Equal(FnisListParseErrorKind.UnexpectedEnd, result.Error);
        Assert.Equal(source.Length, result.Pos);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("foo")]
    [InlineData("-")]
    [InlineData("bb")]
    [InlineData("soo")]
    [InlineData("paa")]
    [InlineData("k")]
    public void RejectsInvalidType(string source) {
        FnisListParseResult<FnisTypeSpan> result = FnisTypeParser.Parse(source.AsSpan(), new TextSpan(0, source.Length));
        Assert.True(result.IsFailure);
        Assert.Equal(FnisListParseErrorKind.InvalidAnimationType, result.Error);
        Assert.Equal(0, result.Pos);
    }

    [Fact]
    public void PreservesInputOffset() {
        const string source = "xx b Attack";

        FnisListParseResult<FnisTypeSpan> result = FnisTypeParser.Parse(source.AsSpan(), new TextSpan(3, 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(FnisAnimType.Basic, result.Value.Type);
        Assert.Equal(4, result.Value.NextPos);
        Assert.Equal(4, result.Pos);
    }

    [Fact]
    public void DoesNotReadPastInputSpan() {
        const string source = "b Attack";

        FnisListParseResult<FnisTypeSpan> result = FnisTypeParser.Parse(source.AsSpan(), new TextSpan(0, 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(FnisAnimType.Basic, result.Value.Type);
        Assert.Equal(1, result.Value.NextPos);
    }

    [Fact]
    public void ParsesTypeWithinInputSpan() {
        const string source = "so";

        FnisListParseResult<FnisTypeSpan> result = FnisTypeParser.Parse(source.AsSpan(), new TextSpan(0, 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(FnisAnimType.Sequenced, result.Value.Type);
        Assert.Equal(1, result.Value.NextPos);
        Assert.Equal(1, result.Pos);
    }

    [Fact]
    public void ParsesOnlyWithinInputSpan() {
        const string source = "xx b Attack";

        FnisListParseResult<FnisTypeSpan> result = FnisTypeParser.Parse(source.AsSpan(), new TextSpan(3, 3));

        Assert.True(result.IsSuccess);
        Assert.Equal(FnisAnimType.Basic, result.Value.Type);
        Assert.Equal(4, result.Value.NextPos);
    }
}
