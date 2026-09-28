using System;
using Xunit;
using Xunit.Sdk;

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

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Animations);
        Assert.Equal(expectedType, result.Value.Animations[0].Type);
    }

    [Fact]
    public void ReadsAnimationData()
    {
        const string source = """
            b -a,ac,h Attack attack.hkx
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);

        FnisAnimation animation = Assert.Single(result.Value.Animations);

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
    public void ReadsSequenceLines()
    {
        const string source = """
            s First first.hkx
            + Second second.hkx
            + Third third.hkx
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Animations.Count);

        FnisAnimation first = result.Value.Animations[0];
        Assert.Equal(FnisAnimType.Sequenced, first.Type);
        Assert.Equal("First", first.AnimEvent(source));
        Assert.Equal("first.hkx", first.AnimFile(source));

        FnisAnimation second = result.Value.Animations[1];
        Assert.Equal(FnisAnimType.SequencedContinued, second.Type);
        Assert.Equal("Second", second.AnimEvent(source));
        Assert.Equal("second.hkx", second.AnimFile(source));

        FnisAnimation third = result.Value.Animations[2];
        Assert.Equal(FnisAnimType.SequencedContinued, third.Type);
        Assert.Equal("Third", third.AnimEvent(source));
        Assert.Equal("third.hkx", third.AnimFile(source));
    }

    [Fact]
    public void ReadsOptimizedSequenceLines()
    {
        const string source = """
            so First first.hkx
            + Second second.hkx
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Animations.Count);
        Assert.Equal(
            FnisAnimType.SequencedOptimized,
            result.Value.Animations[0].Type);
        Assert.Equal(
            FnisAnimType.SequencedContinued,
            result.Value.Animations[1].Type);
    }

    [Fact]
    public void ReadsMultipleAnimations()
    {
        const string source = """
            b Attack attack.hkx
            s First first.hkx
            + Second second.hkx
            km KillMove killmove.hkx
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.Animations.Count);

        Assert.Equal(FnisAnimType.Basic, result.Value.Animations[0].Type);
        Assert.Equal("Attack", result.Value.Animations[0].AnimEvent(source));

        Assert.Equal(FnisAnimType.Sequenced, result.Value.Animations[1].Type);
        Assert.Equal("First", result.Value.Animations[1].AnimEvent(source));

        Assert.Equal(
            FnisAnimType.SequencedContinued,
            result.Value.Animations[2].Type);
        Assert.Equal("Second", result.Value.Animations[2].AnimEvent(source));

        Assert.Equal(FnisAnimType.KillMove, result.Value.Animations[3].Type);
        Assert.Equal("KillMove", result.Value.Animations[3].AnimEvent(source));

        Assert.Empty(result.Value.AnimVars);
        Assert.Empty(result.Value.AlternateAnimations);
    }

    [Fact]
    public void ReadsPairedAnimation()
    {
        const string source = """
            pa -o,D20.0,THit/2.5,T2_Kill/3.25 HugB paired_hugb.hkx Sword/1 Axe/2
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);

        FnisAnimation animation = Assert.Single(result.Value.Animations);

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
            km -ac,h,o,D35.5,TStart/1.0,TEnd/20.0 KillMove killmove.hkx Weapon/1 Victim/2
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();
        if (result.IsFailure)
        {
            Assert.Fail(result.ReadableError(source.AsSpan()));
        }

        Assert.True(result.IsSuccess);

        FnisAnimation animation = Assert.Single(result.Value.Animations);

        Assert.Equal(FnisAnimType.KillMove, animation.Type);
        Assert.Equal("KillMove", animation.AnimEvent(source));
        Assert.Equal("killmove.hkx", animation.AnimFile(source));
        Assert.Equal(
            FnisAnimFlags.AnimatedCamera | FnisAnimFlags.HeadTracking | FnisAnimFlags.AnimObjects,
            animation.Flags);
        Assert.Equal(35.5f, animation.Duration);
        Assert.Equal(2, animation.TriggerCount);
        Assert.Equal(2, animation.ObjectCount);
    }

    [Fact]
    public void ReadsChairLines()
    {
        const string source = """
            ch -o PlayFluteSitting PlayFluteSittingStart.hkx AnimObjectFlute
            + PlayFluteSitting_2 PlayFluteSittingIdlebase.hkx
            + PlayFluteSitting_3 PlayFluteSittingIdlevar1.hkx
            + PlayFluteSitting_4 PlayFluteSittingIdlevar2.hkx
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.Animations.Count);

        FnisAnimation first = result.Value.Animations[0];
        Assert.Equal(FnisAnimType.Chair, first.Type);
        Assert.Equal("PlayFluteSitting", first.AnimEvent(source));
        Assert.Equal(
            "PlayFluteSittingStart.hkx",
            first.AnimFile(source));
        Assert.Equal(FnisAnimFlags.AnimObjects, first.Flags);

        FnisAnimation second = result.Value.Animations[1];
        Assert.Equal(FnisAnimType.SequencedContinued, second.Type);
        Assert.Equal("PlayFluteSitting_2", second.AnimEvent(source));
        Assert.Equal(
            "PlayFluteSittingIdlebase.hkx",
            second.AnimFile(source));

        FnisAnimation third = result.Value.Animations[2];
        Assert.Equal(FnisAnimType.SequencedContinued, third.Type);
        Assert.Equal("PlayFluteSitting_3", third.AnimEvent(source));
        Assert.Equal(
            "PlayFluteSittingIdlevar1.hkx",
            third.AnimFile(source));

        FnisAnimation fourth = result.Value.Animations[3];
        Assert.Equal(FnisAnimType.SequencedContinued, fourth.Type);
        Assert.Equal("PlayFluteSitting_4", fourth.AnimEvent(source));
        Assert.Equal(
            "PlayFluteSittingIdlevar2.hkx",
            fourth.AnimFile(source));
    }

    [Theory]
    [InlineData("ch -a PlayFluteSitting PlayFluteSittingStart.hkx")]
    [InlineData("ch -a,o PlayFluteSitting PlayFluteSittingStart.hkx AnimObjectFlute")]
    public void RejectsInvalidChairStartFlags(string source)
    {
        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsFailure);
        Assert.Equal(FnisListParseErrorKind.ChairAllowsOnlyNoFlagsOrAnimationObjects, result.Error);
    }

    [Fact]
    public void ReadsFurnitureLines()
    {
        const string source = """
            fu -a Kneel_Enter Kneel_Enter.hkx
            + -o,B1.2 Kneel_Loop1 Kneel_Loop1.hkx myAnimObject1 myAnimObject2
            + -o Kneel_Loop2 Kneel_Loop2.hkx myAnimObject1 myAnimObject2
            + -a,o Kneel_Exit Kneel_Exit.hkx myAnimObject1 myAnimObject2
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.Animations.Count);

        FnisAnimation first = result.Value.Animations[0];
        Assert.Equal(FnisAnimType.Furniture, first.Type);
        Assert.Equal(FnisAnimFlags.Acyclic, first.Flags);
        Assert.Equal("Kneel_Enter", first.AnimEvent(source));
        Assert.Equal("Kneel_Enter.hkx", first.AnimFile(source));

        FnisAnimation second = result.Value.Animations[1];
        Assert.Equal(FnisAnimType.SequencedContinued, second.Type);
        Assert.Equal(FnisAnimFlags.AnimObjects, second.Flags);
        Assert.Equal("Kneel_Loop1", second.AnimEvent(source));
        Assert.Equal("Kneel_Loop1.hkx", second.AnimFile(source));

        FnisAnimation third = result.Value.Animations[2];
        Assert.Equal(FnisAnimType.SequencedContinued, third.Type);
        Assert.Equal(FnisAnimFlags.AnimObjects, third.Flags);
        Assert.Equal("Kneel_Loop2", third.AnimEvent(source));
        Assert.Equal("Kneel_Loop2.hkx", third.AnimFile(source));

        FnisAnimation fourth = result.Value.Animations[3];
        Assert.Equal(FnisAnimType.SequencedContinued, fourth.Type);
        Assert.Equal(
            FnisAnimFlags.Acyclic | FnisAnimFlags.AnimObjects,
            fourth.Flags);
        Assert.Equal("Kneel_Exit", fourth.AnimEvent(source));
        Assert.Equal("Kneel_Exit.hkx", fourth.AnimFile(source));
    }

    [Theory]
    [InlineData("fu Kneel_Enter Kneel_Enter.hkx", FnisListParseErrorKind.FurnitureRequiresAcyclic)]
    [InlineData("fu -a Kneel_Enter Kneel_Enter.hkx", FnisListParseErrorKind.FurnitureRequiresThreeAnimations)]
    public void RejectsInvalidFurnitureStart(string source, FnisListParseErrorKind errKind)
    {
        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsFailure);
        Assert.Equal(errKind, result.Error);
    }

    [Fact]
    public void ReadsAnimVars()
    {
        const string source = """
            AnimVar SomeBool BOOL 1
            AnimVar SomeInt INT32 42
            AnimVar SomeReal REAL 1.25
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.AnimVars.Count);

        FnisAnimVarData boolVar = result.Value.AnimVars[0];
        Assert.Equal("SomeBool", boolVar.Name.Slice(source));
        Assert.Equal(FnisAnimVarValueKind.Bool, boolVar.Value.Kind);
        Assert.True(boolVar.Value.Bool);

        FnisAnimVarData intVar = result.Value.AnimVars[1];
        Assert.Equal("SomeInt", intVar.Name.Slice(source));
        Assert.Equal(FnisAnimVarValueKind.Int32, intVar.Value.Kind);
        Assert.Equal(42, intVar.Value.Int32);

        FnisAnimVarData realVar = result.Value.AnimVars[2];
        Assert.Equal("SomeReal", realVar.Name.Slice(source));
        Assert.Equal(FnisAnimVarValueKind.Real, realVar.Value.Kind);
        Assert.Equal(1.25f, realVar.Value.Real);
    }

    [Fact]
    public void ReadsAlternateAnimation()
    {
        const string source = """
            AAprefix MyPrefix
            AAset GroupA 2
            AAset GroupB 4
            T AnimA THit/0.5 T2_Kill/1.25
            T AnimB TStart/2.0
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Animations);
        Assert.Empty(result.Value.AnimVars);
        Assert.Single(result.Value.AlternateAnimations);

        FnisAlternateAnimation alternate =
            result.Value.AlternateAnimations[0];

        Assert.Equal("MyPrefix", alternate.Prefix.Slice(source));

        Assert.Equal(2, alternate.Sets.Length);
        Assert.Equal("GroupA", alternate.Sets[0].Group.Slice(source));
        Assert.Equal((ulong)2, alternate.Sets[0].Slots);
        Assert.Equal("GroupB", alternate.Sets[1].Group.Slice(source));
        Assert.Equal((ulong)4, alternate.Sets[1].Slots);

        Assert.Equal(2, alternate.Triggers.Length);

        Assert.Equal(
            "AnimA",
            alternate.Triggers[0].AnimName.Slice(source));
        Assert.Equal(2, alternate.Triggers[0].Triggers.Count);

        Assert.Equal(
            "AnimB",
            alternate.Triggers[1].AnimName.Slice(source));
        Assert.Single(alternate.Triggers[1].Triggers);
    }

    [Fact]
    public void ReadsMultipleAlternateAnimations()
    {
        const string source = """
            AAprefix First
            AAset GroupA 2
            T AnimA THit/0.5

            b Attack attack.hkx

            AAprefix Second
            AAset GroupB 4
            T AnimB TStart/1.25
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Animations);
        Assert.Equal(2, result.Value.AlternateAnimations.Count);

        Assert.Equal(
            "Attack",
            result.Value.Animations[0].AnimEvent(source));

        Assert.Equal(
            "First",
            result.Value.AlternateAnimations[0].Prefix.Slice(source));

        Assert.Equal(
            "Second",
            result.Value.AlternateAnimations[1].Prefix.Slice(source));
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

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Equal(7, reader.VersionMajor);
        Assert.Equal(2, reader.VersionMinor);

        FnisAnimation animation = Assert.Single(result.Value.Animations);

        Assert.Equal(FnisAnimType.Basic, animation.Type);
        Assert.Equal("Attack", animation.AnimEvent(source));
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

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);

        FnisAnimation animation = Assert.Single(result.Value.Animations);
        Assert.Equal("Attack", animation.AnimEvent(source));
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

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Animations.Count);
        Assert.Equal(
            "Attack",
            result.Value.Animations[0].AnimEvent(source));
        Assert.Equal(
            "Other",
            result.Value.Animations[1].AnimEvent(source));
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

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Animations.Count);
        Assert.Equal(
            FnisAnimType.Sequenced,
            result.Value.Animations[0].Type);
        Assert.Equal(
            FnisAnimType.SequencedContinued,
            result.Value.Animations[1].Type);
        Assert.Equal(
            FnisAnimType.SequencedContinued,
            result.Value.Animations[2].Type);
    }

    [Fact]
    public void RejectsContinuationWithoutSequence()
    {
        const string source = """
            + Second second.hkx
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsFailure);
        Assert.Equal(
            FnisListParseErrorKind.InvalidSequence,
            result.Error);
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

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsFailure);
        Assert.Equal(
            FnisListParseErrorKind.InvalidSequence,
            result.Error);
    }

    [Theory]
    [InlineData("x Attack attack.hkx", FnisListParseErrorKind.InvalidSyntax)]
    [InlineData("invalid Attack attack.hkx", FnisListParseErrorKind.InvalidSyntax)]
    public void RejectsInvalidLine(
        string source,
        FnisListParseErrorKind expectedError)
    {
        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

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

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsFailure);
        Assert.Equal(
            FnisListParseErrorKind.UnexpectedEnd,
            result.Error);
    }

    [Fact]
    public void ParsesEmptyInput()
    {
        FnisListReader reader = new("");

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Animations);
        Assert.Empty(result.Value.AnimVars);
        Assert.Empty(result.Value.AlternateAnimations);
    }

    [Fact]
    public void HandlesCrLf()
    {
        const string source =
            "b Attack attack.hkx\r\nb Other other.hkx\r\n";

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Animations.Count);
        Assert.Equal("Attack", result.Value.Animations[0].AnimEvent(source));
        Assert.Equal("Other", result.Value.Animations[1].AnimEvent(source));
    }

    [Fact]
    public void HandlesCr()
    {
        const string source = "b Attack attack.hkx\rb Other other.hkx";

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Animations.Count);
        Assert.Equal("Attack", result.Value.Animations[0].AnimEvent(source));
        Assert.Equal("Other", result.Value.Animations[1].AnimEvent(source));
    }

    [Fact]
    public void HandlesLf()
    {
        const string source = "b Attack attack.hkx\nb Other other.hkx";

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Animations.Count);
        Assert.Equal("Attack", result.Value.Animations[0].AnimEvent(source));
        Assert.Equal("Other", result.Value.Animations[1].AnimEvent(source));
    }

    [Fact]
    public void ReadableErrorShowsLocationAndSourceLine()
    {
        const string source = """
            b Attack attack.hkx
            + Second second.hkx
            """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsFailure);

        static string GetCallerFilePath([System.Runtime.CompilerServices.CallerFilePath] string path = "")
        {
            return path;
        }

        Assert.Equal(
            NormalizeLineEndings(
                $"""
                  ┌─ {GetCallerFilePath()}:2:1
                  │
                 2 │ + Second second.hkx
                  │ ^ invalid sequence
                """),
            result.ReadableError(source, GetCallerFilePath()));
    }

    private static string NormalizeLineEndings(string value)
    {
        return value.Replace("\r\n", "\n");
    }

    [Fact]
    public void ReadsMotionAndRotationData()
    {
        const string source = """
        b Attack attack.hkx
        MD 1.25 10 20 30
        RD 1.25 45
        """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);

        FnisAnimation animation = Assert.Single(result.Value.Animations);

        Assert.Equal(FnisAnimType.Basic, animation.Type);
        Assert.Equal(1, animation.MotionDataCount);
        Assert.Equal(1, animation.RotationDataCount);

        Assert.True(animation.TryGetMotionData(0, out FnisMotionData motion));
        Assert.Equal(1.25f, motion.Time);
        Assert.Equal(10.0f, motion.DeltaX);
        Assert.Equal(20.0f, motion.DeltaY);
        Assert.Equal(30.0f, motion.DeltaZ);

        Assert.True(animation.TryGetRotationData(0, out FnisRotationData rotation));
        Assert.Equal(FnisRotationDataKind.DeltaZAngle, rotation.Kind);
        Assert.Equal(45.0f, rotation.ZAngle);
    }

    [Fact]
    public void ReadsMultipleMotionAndRotationData()
    {
        const string source = """
        b Attack attack.hkx
        RD 0.5 30
        MD 1.25 10 20 30
        MD 2.0 -10 0 40
        RD 2.0 -60
        """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);

        FnisAnimation animation = Assert.Single(result.Value.Animations);

        Assert.Equal(2, animation.MotionDataCount);
        Assert.Equal(2, animation.RotationDataCount);

        Assert.True(animation.TryGetMotionData(0, out FnisMotionData firstMotion));
        Assert.Equal(1.25f, firstMotion.Time);
        Assert.Equal(10.0f, firstMotion.DeltaX);
        Assert.Equal(20.0f, firstMotion.DeltaY);
        Assert.Equal(30.0f, firstMotion.DeltaZ);

        Assert.True(animation.TryGetMotionData(1, out FnisMotionData secondMotion));
        Assert.Equal(2.0f, secondMotion.Time);
        Assert.Equal(-10.0f, secondMotion.DeltaX);
        Assert.Equal(0.0f, secondMotion.DeltaY);
        Assert.Equal(40.0f, secondMotion.DeltaZ);

        Assert.True(animation.TryGetRotationData(0, out FnisRotationData firstRotation));
        Assert.Equal(0.5f, firstRotation.Time);
        Assert.Equal(FnisRotationDataKind.DeltaZAngle, firstRotation.Kind);
        Assert.Equal(30.0f, firstRotation.ZAngle);

        Assert.True(animation.TryGetRotationData(1, out FnisRotationData secondRotation));
        Assert.Equal(2.0f, secondRotation.Time);
        Assert.Equal(FnisRotationDataKind.DeltaZAngle, secondRotation.Kind);
        Assert.Equal(-60.0f, secondRotation.ZAngle);
    }

    [Fact]
    public void AssociatesMotionAndRotationDataWithPreviousAnimation()
    {
        const string source = """
        b First first.hkx
        MD 0.5 1 2 3
        RD 0.5 15
        b Second second.hkx
        MD 1.25 4 5 6
        RD 1.25 -30
        """;

        FnisListReader reader = new(source);

        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Animations.Count);

        FnisAnimation first = result.Value.Animations[0];
        Assert.Equal("First", first.AnimEvent(source));
        Assert.Equal(1, first.MotionDataCount);
        Assert.Equal(1, first.RotationDataCount);

        Assert.True(first.TryGetMotionData(0, out FnisMotionData firstMotion));
        Assert.Equal(0.5f, firstMotion.Time);

        Assert.True(first.TryGetRotationData(0, out FnisRotationData firstRotation));
        Assert.Equal(15.0f, firstRotation.ZAngle);

        FnisAnimation second = result.Value.Animations[1];
        Assert.Equal("Second", second.AnimEvent(source));
        Assert.Equal(1, second.MotionDataCount);
        Assert.Equal(1, second.RotationDataCount);

        Assert.True(second.TryGetMotionData(0, out FnisMotionData secondMotion));
        Assert.Equal(1.25f, secondMotion.Time);

        Assert.True(second.TryGetRotationData(0, out FnisRotationData secondRotation));
        Assert.Equal(-30.0f, secondRotation.ZAngle);
    }

    [Fact]
    public void ExampleTest()
    {
        // string source = File.ReadAllText("FNIS_List.txt");
        string source = """
    Version 7.0

    b Attack attack.hkx
    MD 1.25 10 20 30
    RD 1.25 45

    b Walk walk.hkx
    MD 2.0 0 10 0
    RD 2.0 -30
    """;

        ReadOnlySpan<char> span = source.AsSpan();

        FnisListReader reader = new(span);
        FnisListParseResult<FnisPattern> result = reader.Parse();

        Assert.True(result.IsSuccess);

        FnisPattern pattern = result.Value;

        string[] expectedEvents = ["Attack", "Walk"];
        string[] expectedFiles = ["attack.hkx", "walk.hkx"];
        float[][] expectedMotionData = [[1.25f, 10.0f, 20.0f, 30.0f], [2.0f, 0.0f, 10.0f, 0.0f]];
        float[] expectedRotationAngles = [45.0f, -30.0f];

        Assert.Equal(expectedEvents.Length, pattern.Animations.Count);

        for (int i = 0; i < pattern.Animations.Count; i++)
        {
            FnisAnimation animation = pattern.Animations[i];

            Assert.Equal(FnisAnimType.Basic, animation.Type);
            Assert.Equal(expectedEvents[i], animation.AnimEvent(span));
            Assert.Equal(expectedFiles[i], animation.AnimFile(span));


            Assert.Equal(1, animation.MotionDataCount);
            Assert.True(animation.TryGetMotionData(0, out FnisMotionData motion));
            Assert.Equal(expectedMotionData[i][0], motion.Time);
            Assert.Equal(expectedMotionData[i][1], motion.DeltaX);
            Assert.Equal(expectedMotionData[i][2], motion.DeltaY);
            Assert.Equal(expectedMotionData[i][3], motion.DeltaZ);

            Assert.Equal(1, animation.RotationDataCount);

            Assert.True(animation.TryGetRotationData(0, out FnisRotationData rotation));
            Assert.Equal(FnisRotationDataKind.DeltaZAngle, rotation.Kind);
            Assert.Equal(expectedRotationAngles[i], rotation.ZAngle);
        }
    }
}
