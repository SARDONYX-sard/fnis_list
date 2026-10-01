using System;
using Xunit;

namespace fnis_list.Tests;

public sealed class FnisLineParserTests {
    [Theory]
    [InlineData("Version 1", 1, 0)]
    [InlineData("Version 1.2", 1, 2)]
    [InlineData("Version   1.2", 1, 2)]
    //
    [InlineData("  Version 1.2", 1, 2)]
    [InlineData("Version 10.20", 10, 20)]
    //
    [InlineData("Version V1", 1, 0)]
    [InlineData("Version v 1.2", 1, 2)]
    [InlineData("Version V10.20", 10, 20)]
    [InlineData("Version V 10.20", 10, 20)]
    [InlineData("Version V 10.20.0", 10, 20)]
    public void TryParseVersion_ParsesValidVersion(string line, int expectedMajor, int expectedMinor) {
        bool result = FnisLineParser.TryParseVersion(line, out int major, out int minor);
        Assert.True(result);
        Assert.Equal(expectedMajor, major);
        Assert.Equal(expectedMinor, minor);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Version")]
    [InlineData("Version v")]
    [InlineData("Version v.")]
    [InlineData("Version .1")]
    [InlineData("Version 1.")]
    [InlineData("Version abc")]
    [InlineData("Version vabc")]
    [InlineData("Version 1.abc")]
    public void TryParseVersion_ReturnsFalseForInvalidVersion(string line) {
        bool result = FnisLineParser.TryParseVersion(line, out int _, out int _);
        Assert.False(result);
    }

    [Fact]
    public void ParsesBasicAnimationWithFlags() {
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
    public void ParsesAnimationType(string typeText, string eventText, string fileText, FnisAnimType expectedType) {
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
    public void ParsesSimpleFlags(string typeText, string flagsText, FnisAnimFlags expectedFlags) {
        FnisAnimation animation = ParseSingle($"{typeText} {flagsText} Attack attack.hkx");
        Assert.Equal(expectedFlags, animation.Flags);
    }

    [Fact]
    public void ParsesTriggers() {
        const string source = "b -ac,THit/0.5,TEnd/1.25 Simple simple.hkx";
        FnisAnimation animation = ParseSingle(source);

        Assert.Equal(FnisAnimFlags.AnimatedCamera, animation.Flags);
        Assert.Equal(2, animation.Triggers.Count);
        Assert.Empty(animation.Objects);

        Assert.Equal("Hit", animation.Triggers[0].Event.Slice(source));
        Assert.Equal(0.5, animation.Triggers[0].Time);
        Assert.Equal("End", animation.Triggers[1].Event.Slice(source));
        Assert.Equal(1.25, animation.Triggers[1].Time);
    }

    [Theory]
    [InlineData("a", true)]
    [InlineData("o", true)]
    [InlineData("bsa", false)]
    [InlineData("k", false)]
    [InlineData("bsa,a", false)]
    [InlineData("k,a", false)]
    [InlineData("bsa,k,a", false)]
    public void ShouldCheckAnimFile_ReturnsExpectedResult(string flags, bool expected) {
        FnisAnimation animation = ParseSingle($"b -{flags} Event animation.hkx");
        Assert.Equal(expected, animation.ShouldCheckAnimFile());
    }

    [Fact]
    public void ParsesPairedAnimation() {
        const string source = "pa -o,D20.0,THit/2.5,T2_Kill/3.25 HugB paired_hugb.hkx Sword/1 Axe/2";

        FnisAnimation animation = ParseSingle(source);

        Assert.Equal(FnisAnimType.Paired, animation.Type);
        Assert.Equal("HugB", animation.AnimEvent(source));
        Assert.Equal("paired_hugb.hkx", animation.AnimFile(source));
        Assert.Equal(FnisAnimFlags.AnimObjects, animation.Flags);
        Assert.Equal(20.0f, animation.Duration);

        Assert.Single(animation.Triggers);
        Assert.Equal("Hit", animation.Triggers[0].Event.Slice(source));
        Assert.Equal(2.5, animation.Triggers[0].Time);

        Assert.Single(animation.Triggers2);
        Assert.Equal("2_Kill", animation.Triggers2[0].Event.Slice(source));
        Assert.Equal(3.25, animation.Triggers2[0].Time);

        Assert.Equal(2, animation.Objects.Count);
        Assert.Equal("Sword", animation.Objects[0].Name.Slice(source));
        Assert.Equal(FnisActorRole.Active, animation.Objects[0].Role);
        Assert.Equal("Axe", animation.Objects[1].Name.Slice(source));
        Assert.Equal(FnisActorRole.Passive, animation.Objects[1].Role);
    }

    [Fact]
    public void ParsesKillMoveAnimation() {
        const string source = "km -ac,h,o,D35.5,TStart/1.0,TEnd/20.0 KillMove killmove.hkx Weapon/1 Victim/2";

        FnisAnimation animation = ParseSingle(source);

        Assert.Equal(FnisAnimType.KillMove, animation.Type);
        Assert.Equal("KillMove", animation.AnimEvent(source));
        Assert.Equal("killmove.hkx", animation.AnimFile(source));
        Assert.Equal(FnisAnimFlags.AnimatedCamera | FnisAnimFlags.HeadTracking | FnisAnimFlags.AnimObjects, animation.Flags);
        Assert.Equal(35.5f, animation.Duration);

        Assert.Equal(2, animation.Triggers.Count);
        Assert.Equal("Start", animation.Triggers[0].Event.Slice(source));
        Assert.Equal(1.0f, animation.Triggers[0].Time);
        Assert.Equal("End", animation.Triggers[1].Event.Slice(source));
        Assert.Equal(20.0f, animation.Triggers[1].Time);

        Assert.Empty(animation.Triggers2);

        Assert.Equal(2, animation.Objects.Count);
        Assert.Equal("Weapon", animation.Objects[0].Name.Slice(source));
        Assert.Equal(FnisActorRole.Active, animation.Objects[0].Role);
        Assert.Equal("Victim", animation.Objects[1].Name.Slice(source));
        Assert.Equal(FnisActorRole.Passive, animation.Objects[1].Role);
    }


    [Theory]
    [InlineData("b -o Attack attack.hkx Sword Shield")]
    [InlineData("pa -o Attack attack.hkx Sword/1 Shield/2")]
    public void ParsesObjectsOk(string source) {
        FnisAnimation animation = ParseSingle(source);
        Assert.True(animation.Objects.Count > 0);
    }

    [Theory]
    [InlineData("b -o Attack attack.hkx Sword/1", FnisListParseErrorKind.NumberedAnimationObjectRequiresPairAndKill)]
    [InlineData("pa -o Attack attack.hkx Sword/3", FnisListParseErrorKind.InvalidPairedAndKillRoleNumber)]
    [InlineData("pa -o Attack attack.hkx Sword/foo", FnisListParseErrorKind.InvalidPairedAndKillRoleNumber)]
    [InlineData("pa -o Attack attack.hkx Sword", FnisListParseErrorKind.PairAndKillRoleRequiresSlash)]
    public void ParsesObjectsError(string source, FnisListParseErrorKind expectedError) {
        FnisListParseResult<FnisAnimation> result = FnisLineParser.Parse(source, TextSpan.FromRange(0, source.Length));

        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error);
    }

    [Fact]
    public void DoesNotReadPastLineEnd() {
        const string source = "b Attack attack.hkx\nb Other other.hkx";

        int lineEnd = source.IndexOf('\n');

        FnisListParseResult<FnisAnimation> result =
            FnisLineParser.Parse(source, TextSpan.FromRange(0, lineEnd));

        Assert.True(result.IsSuccess);

        FnisAnimation animation = result.Value;

        Assert.Equal("Attack", animation.AnimEvent(source));
        Assert.Equal("attack.hkx", animation.AnimFile(source));
        Assert.Empty(animation.Objects);
    }

    [Fact]
    public void PreservesAbsoluteTextSpanPositions() {
        const string source = "header\n\nb -a,o Attack attack.hkx Sword Shield\n";

        int lineStart = source.IndexOf("b -a", StringComparison.Ordinal);
        int lineEnd = source.IndexOf('\n', lineStart);

        FnisListParseResult<FnisAnimation> result =
            FnisLineParser.Parse(source, TextSpan.FromRange(lineStart, lineEnd));

        Assert.True(result.IsSuccess);

        FnisAnimation animation = result.Value;

        Assert.Equal("Attack", animation.AnimEvent(source));
        Assert.Equal("attack.hkx", animation.AnimFile(source));
        Assert.Equal(2, animation.Objects.Count);
    }

    [Theory]
    [InlineData("", FnisListParseErrorKind.UnexpectedEnd)]
    [InlineData("x", FnisListParseErrorKind.InvalidAnimationType)]
    [InlineData("invalid Attack attack.hkx", FnisListParseErrorKind.InvalidAnimationType)]
    public void RejectsInvalidType(string source, FnisListParseErrorKind expectedError) {
        FnisListParseResult<FnisAnimation> result = FnisLineParser.Parse(source, new TextSpan(0, source.Length));
        Assert.True(result.IsFailure);
        Assert.Equal(expectedError, result.Error);
    }

    [Fact]
    public void FailsWhenEventIsMissing() {
        const string source = "b";

        FnisListParseResult<FnisAnimation> result = FnisLineParser.Parse(source, new TextSpan(0, source.Length));

        Assert.True(result.IsFailure);
        Assert.Equal(FnisListParseErrorKind.UnexpectedEnd, result.Error);
    }

    [Fact]
    public void FailsWhenFileIsMissing() {
        const string source = "b Attack";

        FnisListParseResult<FnisAnimation> result = FnisLineParser.Parse(source, new TextSpan(0, source.Length));

        Assert.True(result.IsFailure);
        Assert.Equal(FnisListParseErrorKind.UnexpectedEnd, result.Error);
    }

    private static FnisAnimation ParseSingle(string source) {
        FnisListParseResult<FnisAnimation> result = FnisLineParser.Parse(source, new TextSpan(0, source.Length));

        if (result.IsFailure) {
            Assert.Fail(result.ReadableError(source.AsSpan()));
        }

        return result.Value;
    }
}
