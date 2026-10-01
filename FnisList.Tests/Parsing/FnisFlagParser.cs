using System;
using Xunit;

namespace FnisList.Tests;

public sealed class FnisFlagParserTests {
    [Theory]
    [InlineData("-a,ac,h Attack", FnisAnimFlags.Acyclic | FnisAnimFlags.AnimatedCamera | FnisAnimFlags.HeadTracking)]
    public void Parse_ParsesLineFlags(string input, FnisAnimFlags expected) {
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.Parse(input.AsSpan(), new TextSpan(0, input.Length));

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Flags);
    }

    [Theory]
    [InlineData("a", FnisAnimFlags.Acyclic)]
    [InlineData("o", FnisAnimFlags.AnimObjects)]
    [InlineData("ac", FnisAnimFlags.AnimatedCamera)]
    [InlineData("ac1", FnisAnimFlags.AnimatedCameraSet)]
    [InlineData("ac0", FnisAnimFlags.AnimatedCameraReset)]
    [InlineData("bsa", FnisAnimFlags.BSA)]
    [InlineData("h", FnisAnimFlags.HeadTracking)]
    [InlineData("k", FnisAnimFlags.Known)]
    [InlineData("md", FnisAnimFlags.MotionDriven)]
    [InlineData("st", FnisAnimFlags.Sticky)]
    [InlineData("Tn", FnisAnimFlags.TransitionNext)]
    [InlineData("ac,ac1,ac0", FnisAnimFlags.AnimatedCamera | FnisAnimFlags.AnimatedCameraSet | FnisAnimFlags.AnimatedCameraReset)]
    [InlineData("bsa,k,Tn", FnisAnimFlags.BSA | FnisAnimFlags.Known | FnisAnimFlags.TransitionNext)]
    [InlineData("a,o,h,md,st", FnisAnimFlags.Acyclic | FnisAnimFlags.AnimObjects | FnisAnimFlags.HeadTracking | FnisAnimFlags.MotionDriven | FnisAnimFlags.Sticky)]
    [InlineData("", FnisAnimFlags.None)]
    [InlineData(",", FnisAnimFlags.None)]
    [InlineData("a,unknown,md", FnisAnimFlags.Acyclic | FnisAnimFlags.MotionDriven)]
    [InlineData("unknown", FnisAnimFlags.None)]
    public void Parse_ParsesFlags(string input, FnisAnimFlags expected) {
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(input.AsSpan(), new TextSpan(0, input.Length));

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Flags);
    }

    [Theory]
    [InlineData("D1.5", 1.5f)]
    [InlineData("d2.5", 2.5f)]
    [InlineData("a,D0.25,h", 0.25f)]
    [InlineData("D-1.5", -1.5f)]
    [InlineData("D1e2", 100f)]
    public void Parse_ParsesDuration(string input, float expected) {
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(input.AsSpan(), new TextSpan(0, input.Length));

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Duration);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a,h,md")]
    [InlineData("D")]
    [InlineData("Dabc")]
    [InlineData("D.")]
    public void Parse_InvalidDurationReturnsNull(string input) {
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(input.AsSpan(), new TextSpan(0, input.Length));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Duration);
    }

    [Theory]
    [InlineData("B0.75", 0.75f)]
    [InlineData("b1.25", 1.25f)]
    [InlineData("a,B0.5,h", 0.5f)]
    [InlineData("B-1.5", -1.5f)]
    [InlineData("B1e2", 100f)]
    public void Parse_ParsesBlendTime(string input, float expected) {
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(input.AsSpan(), new TextSpan(0, input.Length));

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.BlendTime);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a,h,md")]
    [InlineData("B")]
    [InlineData("Babc")]
    [InlineData("B.")]
    public void Parse_InvalidBlendTimeReturnsNull(string input) {
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(input.AsSpan(), new TextSpan(0, input.Length));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.BlendTime);
    }

    [Theory]
    [InlineData("THi/0.5,TEn/1.25", 2)]
    [InlineData("THi/0.5,T2_Hi/1.0,TEn/1.25", 2)]
    [InlineData("a,D1.0,THi/0.5,h,TEn/1.25", 2)]
    [InlineData("", 0)]
    [InlineData("a,h,md", 0)]
    // THi/abc -> invalid
    // THi -> invalid
    // T/1.0 -> missing trigger name
    // T2_Hi/1.0 -> trigger2
    [InlineData("THi/abc,THi,T/1.0,T2_Hi/1.0", 0)]
    public void Parse_ParsesTriggerCount(string input, int expected) {
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(input.AsSpan(), new TextSpan(0, input.Length));

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Triggers.Count);
    }

    [Theory]
    [InlineData("T2_Hi/0.5,T2_En/1.25", 2)]
    [InlineData("THi/0.5,T2_Hi/2.0,TEn/1.25", 1)]
    public void Parse_ParsesTrigger2Count(string input, int expected) {
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(input.AsSpan(), new TextSpan(0, input.Length));

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value.Triggers2.Count);
    }

    [Theory]
    [InlineData("THi/0.5", 0, "Hi", 0.5f)]
    [InlineData("THi/0.5,TEn/1.25", 1, "En", 1.25f)]
    [InlineData("a,D1.0,THi/0.5,h,TEn/1.25", 1, "En", 1.25f)]
    public void Parse_ParsesTrigger(string input, int index, string expectedEvent, float expectedTime) {
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(input.AsSpan(), new TextSpan(0, input.Length));
        Assert.True(result.IsSuccess);

        FnisTriggerSpan trigger = result.Value.Triggers[index];
        Assert.Equal(expectedEvent, trigger.Event.Slice(input));
        Assert.Equal(expectedTime, trigger.Time);
    }

    [Theory]
    [InlineData("")]
    [InlineData("THi/abc,THi,T/1.0")]
    [InlineData("T2_Hi/0.5")]
    public void Parse_DoesNotAddInvalidNormalTriggers(string input) {
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(input.AsSpan(), new TextSpan(0, input.Length));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Triggers);
    }

    [Theory]
    [InlineData("T2_Hi/0.5,T2_En/1.25", 0, "2_Hi", 0.5f)]
    [InlineData("T2_Hi/0.5,T2_En/1.25", 1, "2_En", 1.25f)]
    public void Parse_ParsesTrigger2(string input, int index, string expectedEvent, float expectedTime) {
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(input.AsSpan(), new TextSpan(0, input.Length));

        Assert.True(result.IsSuccess);
        FnisTriggerSpan trigger = result.Value.Triggers2[index];

        Assert.Equal(expectedEvent, trigger.Event.Slice(input));
        Assert.Equal(expectedTime, trigger.Time);
    }

    [Theory]
    [InlineData("prefix a,h,md", 7)]
    [InlineData("xxx,a,h,md", 4)]
    [InlineData("012345a,h,md", 6)]
    public void Parse_StartsAtPosition(string source, int position) {
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(source.AsSpan(), new TextSpan(position, source.Length - position));

        Assert.True(result.IsSuccess);
        Assert.Equal(FnisAnimFlags.Acyclic | FnisAnimFlags.HeadTracking | FnisAnimFlags.MotionDriven, result.Value.Flags);
    }

    [Fact]
    public void Parse_ReturnsNextPos() {
        const string source = "a,h,md";
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(source.AsSpan(), new TextSpan(0, source.Length));

        Assert.True(result.IsSuccess);
        Assert.Equal(source.Length, result.Value.NextPos);
    }

    [Fact]
    public void Parse_NextPosIsAbsolute() {
        const string source = "prefix,a,h,md";
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(source.AsSpan(), new TextSpan(7, source.Length - 7));

        Assert.True(result.IsSuccess);
        Assert.Equal(source.Length, result.Value.NextPos);
    }

    [Fact]
    public void Parse_NextPosCanBeUsedToReadNextPart() {
        const string source = "a,h,md Attack  attack.hkx";
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(source.AsSpan(), new TextSpan(0, source.Length));

        Assert.True(result.IsSuccess);
        int nextPos = result.Value.NextPos;

        Assert.Equal(' ', source[nextPos]);
        Assert.Equal("Attack  attack.hkx", source[(nextPos + 1)..]);
    }

    [Theory]
    [InlineData("a,h,md Attack  attack.hkx")]
    [InlineData("a, h, md Attack  attack.hkx")]
    [InlineData("a ,h ,md Attack  attack.hkx")]
    [InlineData("a , h , md Attack  attack.hkx")]
    [InlineData("a,   h,   md Attack  attack.hkx")]
    [InlineData("a   ,h   ,md Attack  attack.hkx")]
    [InlineData("a   ,   h   ,   md Attack  attack.hkx")]
    public void Parse_AllowsWhitespaceAroundFlagSeparators(string source) {
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(source.AsSpan(), new TextSpan(0, source.Length));

        Assert.True(result.IsSuccess);
        Assert.Equal(FnisAnimFlags.Acyclic | FnisAnimFlags.HeadTracking | FnisAnimFlags.MotionDriven, result.Value.Flags);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(10)]
    public void Parse_InvalidPositionReturnsFailure(int position) {
        const string source = "abc";

        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(source.AsSpan(), new TextSpan(position, 0));

        Assert.True(result.IsFailure);
        Assert.Equal(FnisListParseErrorKind.InvalidSourceRange, result.Error);
        Assert.Equal(position, result.Pos);
    }

    [Fact]
    public void Parse_StopsAtWhitespace() {
        const string source = "a,h,md Event";
        FnisListParseResult<FnisFlagSpan> result = FnisFlagParser.ParseRaw(source.AsSpan(), new TextSpan(0, source.Length));

        Assert.True(result.IsSuccess);
        Assert.Equal(FnisAnimFlags.Acyclic | FnisAnimFlags.HeadTracking | FnisAnimFlags.MotionDriven, result.Value.Flags);
        Assert.Equal(6, result.Value.NextPos);
        Assert.Equal(' ', source[result.Value.NextPos]);
    }
}
