using System;
using Xunit;

namespace fnis_list.Tests;

public sealed class FnisListReaderTests
{
    [Theory]
    [InlineData("b Attack attack.hkx", FnisAnimType.Basic)]
    [InlineData("s First first.hkx", FnisAnimType.Sequenced)]
    [InlineData("so SeqStart seq_start.hkx", FnisAnimType.SequencedOptimized)]
    [InlineData("pa HugB paired_hugb.hkx", FnisAnimType.Paired)]
    [InlineData("km KillMove killmove.hkx", FnisAnimType.KillMove)]
    public void ReadsAnimation(string source, FnisAnimType expectedType)
    {
        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Read();

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedType, result.Value.Type);
    }

    [Fact]
    public void ReadsAnimationData()
    {
        const string source = """
            b -a,ac,h Attack attack.hkx
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Read();

        Assert.True(result.IsSuccess);

        FnisAnimation animation = result.Value.Animation;

        Assert.Equal(FnisAnimType.Basic, animation.Type);
        Assert.Equal(
            FnisAnimFlags.Acyclic |
            FnisAnimFlags.AnimatedCamera |
            FnisAnimFlags.HeadTracking,
            animation.Flags);
        Assert.Equal("Attack", animation.AnimEvent(source));
        Assert.Equal("attack.hkx", animation.AnimFile(source));
    }

    [Fact]
    public void ReadsSequenceLinesIndividually()
    {
        const string source = """
            s First first.hkx
            + Second second.hkx
            + Third third.hkx
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> first = reader.Read();

        Assert.True(first.IsSuccess);
        Assert.Equal(FnisAnimType.Sequenced, first.Value.Type);
        Assert.Equal("First", first.Value.Animation.AnimEvent(source));
        Assert.Equal("first.hkx", first.Value.Animation.AnimFile(source));

        FnisListParseResult<FnisPattern> second = reader.Read();

        Assert.True(second.IsSuccess);
        Assert.Equal(FnisAnimType.SequencedContinued, second.Value.Type);
        Assert.Equal("Second", second.Value.Animation.AnimEvent(source));
        Assert.Equal("second.hkx", second.Value.Animation.AnimFile(source));

        FnisListParseResult<FnisPattern> third = reader.Read();

        Assert.True(third.IsSuccess);
        Assert.Equal(FnisAnimType.SequencedContinued, third.Value.Type);
        Assert.Equal("Third", third.Value.Animation.AnimEvent(source));
        Assert.Equal("third.hkx", third.Value.Animation.AnimFile(source));

        Assert.True(reader.Read().IsEndOfInput);
    }

    [Fact]
    public void ReadsOptimizedSequenceLinesIndividually()
    {
        const string source = """
            so First first.hkx
            + Second second.hkx
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> first = reader.Read();

        Assert.True(first.IsSuccess);
        Assert.Equal(FnisAnimType.SequencedOptimized, first.Value.Type);

        FnisListParseResult<FnisPattern> second = reader.Read();

        Assert.True(second.IsSuccess);
        Assert.Equal(FnisAnimType.SequencedContinued, second.Value.Type);
    }

    [Fact]
    public void ReadsMultiplePatterns()
    {
        const string source = """
            b Attack attack.hkx
            s First first.hkx
            + Second second.hkx
            km KillMove killmove.hkx
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> basic = reader.Read();

        Assert.True(basic.IsSuccess);
        Assert.Equal(FnisAnimType.Basic, basic.Value.Type);
        Assert.Equal("Attack", basic.Value.Animation.AnimEvent(source));

        FnisListParseResult<FnisPattern> sequence = reader.Read();

        Assert.True(sequence.IsSuccess);
        Assert.Equal(FnisAnimType.Sequenced, sequence.Value.Type);
        Assert.Equal("First", sequence.Value.Animation.AnimEvent(source));

        FnisListParseResult<FnisPattern> continuation = reader.Read();

        Assert.True(continuation.IsSuccess);
        Assert.Equal(FnisAnimType.SequencedContinued, continuation.Value.Type);
        Assert.Equal("Second", continuation.Value.Animation.AnimEvent(source));

        FnisListParseResult<FnisPattern> killMove = reader.Read();

        Assert.True(killMove.IsSuccess);
        Assert.Equal(FnisAnimType.KillMove, killMove.Value.Type);
        Assert.Equal("KillMove", killMove.Value.Animation.AnimEvent(source));

        Assert.True(reader.Read().IsEndOfInput);
    }

    [Fact]
    public void ReadsPairedAnimation()
    {
        const string source = """
            pa -o,D20.0,THit/2.5,T2_Kill/3.25 HugB paired_hugb.hkx Sword/1 Axe/2
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Read();

        Assert.True(result.IsSuccess);

        FnisAnimation animation = result.Value.Animation;

        Assert.Equal(FnisAnimType.Paired, animation.Type);
        Assert.Equal("HugB", animation.AnimEvent(source));
        Assert.Equal("paired_hugb.hkx", animation.AnimFile(source));
        Assert.Equal(FnisAnimFlags.AnimObjects, animation.Flags);
        Assert.Equal(20.0f, animation.Duration);
        Assert.Equal(1, animation.TriggerCount);
        Assert.Equal(1, animation.Trigger2Count);
        Assert.Equal(2, animation.ObjectCount);
    }

    [Fact]
    public void ReadsKillMoveAnimation()
    {
        const string source = """
            km -ac,h,D35.5,TStart/1.0,TEnd/20.0 KillMove killmove.hkx Weapon/1 Victim/2
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Read();

        Assert.True(result.IsSuccess);

        FnisAnimation animation = result.Value.Animation;

        Assert.Equal(FnisAnimType.KillMove, animation.Type);
        Assert.Equal("KillMove", animation.AnimEvent(source));
        Assert.Equal("killmove.hkx", animation.AnimFile(source));
        Assert.Equal(
            FnisAnimFlags.AnimatedCamera | FnisAnimFlags.HeadTracking,
            animation.Flags);
        Assert.Equal(35.5f, animation.Duration);
        Assert.Equal(2, animation.TriggerCount);
        Assert.Equal(2, animation.ObjectCount);
    }

    [Fact]
    public void SkipsVersion()
    {
        const string source = """
            Version V7.2
                'comment

            b Attack attack.hkx
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Read();

        Assert.True(result.IsSuccess);
        Assert.Equal(7, reader.VersionMajor);
        Assert.Equal(2, reader.VersionMinor);
        Assert.Equal(FnisAnimType.Basic, result.Value.Type);
        Assert.Equal("Attack", result.Value.Animation.AnimEvent(source));
    }

    [Fact]
    public void SkipsCommentsAndBlankLines()
    {
        const string source = """
            ' comment

              ' another comment

            b Attack attack.hkx
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Read();

        Assert.True(result.IsSuccess);
        Assert.Equal(FnisAnimType.Basic, result.Value.Type);
        Assert.Equal("Attack", result.Value.Animation.AnimEvent(source));
        Assert.True(reader.Read().IsEndOfInput);
    }

    [Fact]
    public void SkipsCommentsBetweenAnimations()
    {
        const string source = """
            b Attack attack.hkx
            ' comment

            b Other other.hkx
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> first = reader.Read();

        Assert.True(first.IsSuccess);
        Assert.Equal("Attack", first.Value.Animation.AnimEvent(source));

        FnisListParseResult<FnisPattern> second = reader.Read();

        Assert.True(second.IsSuccess);
        Assert.Equal("Other", second.Value.Animation.AnimEvent(source));

        Assert.True(reader.Read().IsEndOfInput);
    }

    [Fact]
    public void AllowsCommentsAndBlankLinesInsideSequence()
    {
        const string source = """
            s First first.hkx

            ' comment

            + Second second.hkx

            ' comment

            + Third third.hkx
            """;

        FnisListReader reader = new(source);

        Assert.True(reader.Read().IsSuccess);
        Assert.True(reader.Read().IsSuccess);
        Assert.True(reader.Read().IsSuccess);
        Assert.True(reader.Read().IsEndOfInput);
    }

    [Fact]
    public void RejectsContinuationWithoutSequence()
    {
        const string source = """
            + Second second.hkx
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Read();

        Assert.True(result.IsFailure);
        Assert.Equal(FnisListParseErrorKind.InvalidSequence, result.Error);
    }

    [Theory]
    [InlineData("""
        b Attack attack.hkx
        + Second second.hkx
        """)]
    [InlineData("""
        pa HugB paired_hugb.hkx
        + Second second.hkx
        """)]
    [InlineData("""
        km KillMove killmove.hkx
        + Second second.hkx
        """)]
    public void RejectsContinuationAfterNonSequence(string source)
    {
        FnisListReader reader = new(source);

        Assert.True(reader.Read().IsSuccess);

        FnisListParseResult<FnisPattern> result = reader.Read();

        Assert.True(result.IsFailure);
        Assert.Equal(FnisListParseErrorKind.InvalidSequence, result.Error);
    }

    [Theory]
    [InlineData("x Attack attack.hkx", FnisListParseErrorKind.InvalidSyntax)]
    [InlineData("invalid Attack attack.hkx", FnisListParseErrorKind.InvalidSyntax)]
    public void RejectsInvalidLine(string source, FnisListParseErrorKind expectedError)
    {
        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Read();

        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error);
    }

    [Fact]
    public void ReturnsUnexpectedEndForIncompleteAnimation()
    {
        const string source = """
            b Attack
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Read();

        Assert.True(result.IsFailure);
        Assert.Equal(FnisListParseErrorKind.UnexpectedEnd, result.Error);
    }

    [Fact]
    public void ReturnsEndOfInputAfterLastAnimation()
    {
        const string source = """
            b Attack attack.hkx
            """;

        FnisListReader reader = new(source);

        Assert.True(reader.Read().IsSuccess);
        Assert.True(reader.Read().IsEndOfInput);
        Assert.True(reader.Read().IsEndOfInput);
    }

    [Fact]
    public void HandlesCrLf()
    {
        const string source = "b Attack attack.hkx\r\nb Other other.hkx\r\n";

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> first = reader.Read();

        Assert.True(first.IsSuccess);
        Assert.Equal("Attack", first.Value.Animation.AnimEvent(source));

        FnisListParseResult<FnisPattern> second = reader.Read();

        Assert.True(second.IsSuccess);
        Assert.Equal("Other", second.Value.Animation.AnimEvent(source));

        Assert.True(reader.Read().IsEndOfInput);
    }

    [Fact]
    public void HandlesCr()
    {
        const string source = "b Attack attack.hkx\rb Other other.hkx";

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> first = reader.Read();

        Assert.True(first.IsSuccess);
        Assert.Equal("Attack", first.Value.Animation.AnimEvent(source));

        FnisListParseResult<FnisPattern> second = reader.Read();

        Assert.True(second.IsSuccess);
        Assert.Equal("Other", second.Value.Animation.AnimEvent(source));
    }

    [Fact]
    public void HandlesLf()
    {
        const string source = "b Attack attack.hkx\nb Other other.hkx";

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> first = reader.Read();

        Assert.True(first.IsSuccess);
        Assert.Equal("Attack", first.Value.Animation.AnimEvent(source));

        FnisListParseResult<FnisPattern> second = reader.Read();

        Assert.True(second.IsSuccess);
        Assert.Equal("Other", second.Value.Animation.AnimEvent(source));
    }

    private static string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n");
    }

    [Fact]
    public void ReadableErrorShowsLocationAndSourceLine()
    {
        const string source = """
        b Attack attack.hkx
        + Second second.hkx
        """;

        FnisListReader reader = new(source);

        Assert.True(reader.Read().IsSuccess);

        FnisListParseResult<FnisPattern> result = reader.Read();

        Assert.True(result.IsFailure);

        static string GetCallerFilePath([System.Runtime.CompilerServices.CallerFilePath] string path = "")
        {
            return path;
        }

        Assert.Equal(NormalizeLineEndings(
            $"""
      ┌─ {GetCallerFilePath()}:2:1
      │
     2 │ + Second second.hkx
      │ ^ invalid sequence
    """),
            result.ReadableError(source, GetCallerFilePath()));
    }
}
