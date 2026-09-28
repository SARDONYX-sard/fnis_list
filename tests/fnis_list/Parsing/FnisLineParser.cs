using System;
using Xunit;

namespace fnis_list.Tests;

public sealed class FnisLineParserTests
{
    [Fact]
    public void ParsesBasicAnimationWithFlags()
    {
        const string source = "b -a,ac,h Attack attack.hkx";

        FnisListParseResult<FnisAnimation> result = FnisLineParser.Parse(source.AsSpan(), new TextSpan(0, source.Length));

        Assert.True(result.IsSuccess);
        FnisAnimation animation = result.Value;

        Assert.Equal(FnisAnimType.Basic, animation.Type);
        Assert.Equal("Attack", animation.AnimEvent(source));
        Assert.Equal("attack.hkx", animation.AnimFile(source));
        Assert.Equal(
            FnisAnimFlags.Acyclic |
            FnisAnimFlags.AnimatedCamera |
            FnisAnimFlags.HeadTracking,
            animation.Flags);
    }

    [Theory]
    [InlineData("b", "Attack", "attack.hkx", FnisAnimType.Basic)]
    [InlineData("s", "First", "first.hkx", FnisAnimType.Sequenced)]
    [InlineData("so", "SeqStart", "seq_start.hkx", FnisAnimType.SequencedOptimized)]
    [InlineData("+", "Second", "second.hkx", FnisAnimType.SequencedContinued)]
    [InlineData("pa", "HugB", "paired_hugb.hkx", FnisAnimType.Paired)]
    [InlineData("km", "KillMove", "killmove.hkx", FnisAnimType.KillMove)]
    public void ParsesAnimationType(string typeText, string eventText, string fileText, FnisAnimType expectedType)
    {
        string source = $"{typeText} {eventText} {fileText}";

        FnisAnimation animation = ParseSingle(source);

        Assert.Equal(expectedType, animation.Type);
        Assert.Equal(eventText, animation.AnimEvent(source));
        Assert.Equal(fileText, animation.AnimFile(source));
    }

    [Theory]
    [InlineData("b", "-a", FnisAnimFlags.Acyclic)]
    [InlineData("b", "-ac", FnisAnimFlags.AnimatedCamera)]
    [InlineData("b", "-h", FnisAnimFlags.HeadTracking)]
    [InlineData("b", "-a,ac,h", FnisAnimFlags.Acyclic | FnisAnimFlags.AnimatedCamera | FnisAnimFlags.HeadTracking)]
    [InlineData("s", "-a", FnisAnimFlags.Acyclic)]
    [InlineData("so", "", FnisAnimFlags.None)]
    [InlineData("+", "", FnisAnimFlags.None)]
    public void ParsesSimpleFlags(string typeText, string flagsText, FnisAnimFlags expectedFlags)
    {
        FnisAnimation animation = ParseSingle($"{typeText} {flagsText} Attack attack.hkx");
        Assert.Equal(expectedFlags, animation.Flags);
    }

    [Fact]
    public void ParsesTriggers()
    {
        const string source = "b -ac,THit/0.5,TEnd/1.25 Simple simple.hkx";

        FnisAnimation animation = ParseSingle(source);

        Assert.Equal(FnisAnimFlags.AnimatedCamera, animation.Flags);
        Assert.Equal(2, animation.TriggerCount);

        Assert.True(animation.TryGetTrigger(source, 0, out FnisTrigger trigger0));
        Assert.Equal("Hit", trigger0.Event);
        Assert.Equal(0.5, trigger0.Time);

        Assert.True(animation.TryGetTrigger(source, 1, out FnisTrigger trigger1));
        Assert.Equal("End", trigger1.Event);
        Assert.Equal(1.25, trigger1.Time);

        Assert.Equal(0, animation.ObjectCount);
    }

    [Fact]
    public void ParsesPairedAnimation()
    {
        const string source = "pa -o,D20.0,THit/2.5,T2_Kill/3.25 HugB paired_hugb.hkx Sword/1 Axe/2";

        FnisAnimation animation = ParseSingle(source);

        Assert.Equal(FnisAnimType.Paired, animation.Type);
        Assert.Equal("HugB", animation.AnimEvent(source));
        Assert.Equal("paired_hugb.hkx", animation.AnimFile(source));
        Assert.Equal(FnisAnimFlags.AnimObjects, animation.Flags);
        Assert.Equal(20.0f, animation.Duration);
        Assert.Equal(1, animation.TriggerCount);
        Assert.Equal(1, animation.Trigger2Count);

        Assert.True(animation.TryGetTrigger(source, 0, out FnisTrigger trigger));
        Assert.Equal("Hit", trigger.Event);
        Assert.Equal(2.5, trigger.Time);

        Assert.True(animation.TryGetTrigger2(source, 0, out FnisTrigger trigger2));
        Assert.Equal("2_Kill", trigger2.Event);
        Assert.Equal(3.25, trigger2.Time);

        Assert.Equal(2, animation.ObjectCount);

        Assert.True(animation.TryGetObject(source, 0, out FnisAnimObject sword));
        Assert.Equal("Sword", sword.Name);
        Assert.Equal(FnisActorRole.Active, sword.Role);

        Assert.True(animation.TryGetObject(source, 1, out FnisAnimObject axe));
        Assert.Equal("Axe", axe.Name);
        Assert.Equal(FnisActorRole.Passive, axe.Role);
    }

    [Fact]
    public void ParsesKillMoveAnimation()
    {
        const string source = "km -ac,h,o,D35.5,TStart/1.0,TEnd/20.0 KillMove killmove.hkx Weapon/1 Victim/2";

        FnisAnimation animation = ParseSingle(source);

        Assert.Equal(FnisAnimType.KillMove, animation.Type);
        Assert.Equal("KillMove", animation.AnimEvent(source));
        Assert.Equal("killmove.hkx", animation.AnimFile(source));
        Assert.Equal(FnisAnimFlags.AnimatedCamera | FnisAnimFlags.HeadTracking | FnisAnimFlags.AnimObjects, animation.Flags);
        Assert.Equal(35.5f, animation.Duration);
        Assert.Equal(2, animation.TriggerCount);
        Assert.Equal(0, animation.Trigger2Count);
        Assert.Equal(2, animation.ObjectCount);

        Assert.True(animation.TryGetTrigger(source, 0, out FnisTrigger start));
        Assert.Equal("Start", start.Event);
        Assert.Equal(1.0, start.Time);

        Assert.True(animation.TryGetTrigger(source, 1, out FnisTrigger end));
        Assert.Equal("End", end.Event);
        Assert.Equal(20.0, end.Time);

        Assert.True(animation.TryGetObject(source, 0, out FnisAnimObject weapon));
        Assert.Equal("Weapon", weapon.Name);
        Assert.Equal(FnisActorRole.Active, weapon.Role);

        Assert.True(animation.TryGetObject(source, 1, out FnisAnimObject victim));
        Assert.Equal("Victim", victim.Name);
        Assert.Equal(FnisActorRole.Passive, victim.Role);
    }


    [Theory]
    [InlineData("b -o Attack attack.hkx Sword Shield")]
    [InlineData("pa -o Attack attack.hkx Sword/1 Shield/2")]
    public void ParsesObjectsOk(string source)
    {
        FnisAnimation animation = ParseSingle(source);
        Assert.True(animation.ObjectCount > 0);
    }

    [Theory]
    [InlineData("b -o Attack attack.hkx Sword/1", FnisListParseErrorKind.NumberedAnimationObjectRequiresPairAndKill)]
    [InlineData("pa -o Attack attack.hkx Sword/3", FnisListParseErrorKind.InvalidPairedAndKillRoleNumber)]
    [InlineData("pa -o Attack attack.hkx Sword/foo", FnisListParseErrorKind.InvalidPairedAndKillRoleNumber)]
    [InlineData("pa -o Attack attack.hkx Sword", FnisListParseErrorKind.PairAndKillRoleRequiresSlash)]
    public void ParsesObjectsError(string source, FnisListParseErrorKind expectedError)
    {
        FnisListParseResult<FnisAnimation> result = FnisLineParser.Parse(source, TextSpan.FromRange(0, source.Length));

        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error);
    }

    [Fact]
    public void DoesNotReadPastLineEnd()
    {
        const string source = "b Attack attack.hkx\nb Other other.hkx";

        int lineEnd = source.IndexOf('\n');

        FnisListParseResult<FnisAnimation> result =
            FnisLineParser.Parse(source, TextSpan.FromRange(0, lineEnd));

        Assert.True(result.IsSuccess);

        FnisAnimation animation = result.Value;

        Assert.Equal("Attack", animation.AnimEvent(source));
        Assert.Equal("attack.hkx", animation.AnimFile(source));
        Assert.Equal(0, animation.ObjectCount);
    }

    [Fact]
    public void PreservesAbsoluteTextSpanPositions()
    {
        const string source = "header\n\nb -a,o Attack attack.hkx Sword Shield\n";

        int lineStart = source.IndexOf("b -a", StringComparison.Ordinal);
        int lineEnd = source.IndexOf('\n', lineStart);

        FnisListParseResult<FnisAnimation> result =
            FnisLineParser.Parse(source, TextSpan.FromRange(lineStart, lineEnd));

        Assert.True(result.IsSuccess);

        FnisAnimation animation = result.Value;

        Assert.Equal("Attack", animation.AnimEvent(source));
        Assert.Equal("attack.hkx", animation.AnimFile(source));
        Assert.Equal(2, animation.ObjectCount);
    }

    [Theory]
    [InlineData("", FnisListParseErrorKind.UnexpectedEnd)]
    [InlineData("x", FnisListParseErrorKind.InvalidSyntax)]
    [InlineData("invalid Attack attack.hkx", FnisListParseErrorKind.InvalidSyntax)]
    public void RejectsInvalidType(string source, FnisListParseErrorKind expectedError)
    {
        FnisListParseResult<FnisAnimation> result = FnisLineParser.Parse(source, new TextSpan(0, source.Length));
        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error);
    }

    [Fact]
    public void FailsWhenEventIsMissing()
    {
        const string source = "b";

        FnisListParseResult<FnisAnimation> result = FnisLineParser.Parse(source, new TextSpan(0, source.Length));

        Assert.True(result.IsFailure);
        Assert.Equal(FnisListParseErrorKind.UnexpectedEnd, result.Error);
    }

    [Fact]
    public void FailsWhenFileIsMissing()
    {
        const string source = "b Attack";

        FnisListParseResult<FnisAnimation> result = FnisLineParser.Parse(source, new TextSpan(0, source.Length));

        Assert.True(result.IsFailure);
        Assert.Equal(FnisListParseErrorKind.UnexpectedEnd, result.Error);
    }

    private static FnisAnimation ParseSingle(string source)
    {
        FnisListParseResult<FnisAnimation> result = FnisLineParser.Parse(source, new TextSpan(0, source.Length));

        if (result.IsFailure)
        {
            Assert.Fail(result.ReadableError(source.AsSpan()));
        }

        return result.Value;
    }
}
