using System;
using System.Numerics;
using Xunit;

namespace fnis_list.Tests;

public sealed class FnisRotationParserTests {
    [Theory]
    [InlineData("MD 2.5 0 0 30", 2.5f, 0.0f, 0.0f, 30.0f)]
    [InlineData("md 1.25 -1.5 0.5 3.0", 1.25f, -1.5f, 0.5f, 3.0f)]
    public void ParseMotion_ParsesValidData(string source, float expectedTime, float expectedDeltaX, float expectedDeltaY, float expectedDeltaZ) {
        TextSpan line = TextSpan.FromRange(0, source.Length);
        FnisListParseResult<FnisMotion> result = FnisRotationParser.ParseMotion(source, line);

        Assert.False(result.IsFailure);

        FnisMotion motion = result.Value;

        Assert.Equal(expectedTime, motion.Time);
        Assert.Equal(expectedDeltaX, motion.DeltaX);
        Assert.Equal(expectedDeltaY, motion.DeltaY);
        Assert.Equal(expectedDeltaZ, motion.DeltaZ);
    }

    [Theory]
    [InlineData("MD")]
    [InlineData("MD 1.0")]
    [InlineData("MD 1.0 0.0")]
    [InlineData("MD 1.0 0.0 0.0")]
    [InlineData("MD abc 0 0 30")]
    [InlineData("MD 1.0 0 0 30 40")]
    public void ParseMotion_RejectsInvalidData(string source) {
        TextSpan line = TextSpan.FromRange(0, source.Length);
        FnisListParseResult<FnisMotion> result = FnisRotationParser.ParseMotion(source, line);

        Assert.True(result.IsFailure);
        Assert.Equal(FnisListParseErrorKind.InvalidSyntax, result.Error);
    }

    [Theory]
    [InlineData("RD 1.5 0.0 0.0 0.0 1.0", 1.5f, 0.0f, 0.0f, 0.0f, 1.0f)]
    [InlineData("rd 2.0 0.0 0.5 -0.5 1.0", 2.0f, 0.0f, 0.5f, -0.5f, 1.0f)]
    public void ParseRotation_ParsesQuaternion(string source, float expectedTime, float expectedX, float expectedY, float expectedZ, float expectedW) {
        TextSpan line = TextSpan.FromRange(0, source.Length);
        FnisListParseResult<FnisRotation> result = FnisRotationParser.ParseRotation(source, line);

        Assert.False(result.IsFailure);

        FnisRotation rotation = result.Value;
        Assert.Equal(FnisRotationKind.Quaternion, rotation.Kind);
        Assert.Equal(expectedTime, rotation.Time);

        Quaternion quaternion = rotation.Quaternion;
        Assert.Equal(expectedX, quaternion.X);
        Assert.Equal(expectedY, quaternion.Y);
        Assert.Equal(expectedZ, quaternion.Z);
        Assert.Equal(expectedW, quaternion.W);
    }

    [Theory]
    [InlineData("RD 2.0 90", 2.0f, 90.0f)]
    [InlineData("rd 1.25 -45.0", 1.25f, -45.0f)]
    [InlineData("RD 0.5 0.0", 0.5f, 0.0f)]
    public void ParseRotation_ParsesDeltaZAngle(string source, float expectedTime, float expectedAngle) {
        TextSpan line = TextSpan.FromRange(0, source.Length);
        FnisListParseResult<FnisRotation> result = FnisRotationParser.ParseRotation(source, line);

        Assert.False(result.IsFailure);

        FnisRotation rotation = result.Value;

        Assert.Equal(FnisRotationKind.DeltaZAngle, rotation.Kind);
        Assert.Equal(expectedTime, rotation.Time);
        Assert.Equal(expectedAngle, rotation.ZAngle);
    }

    [Theory]
    [InlineData("RD")]
    [InlineData("RD 1.0")]
    [InlineData("RD 1.0 abc")]
    [InlineData("RD 1.0 1.0 2.0 3.0")]
    [InlineData("RD 1.0 1.0 2.0 3.0 4.0 5.0")]
    public void ParseRotation_RejectsInvalidData(string source) {
        TextSpan line = TextSpan.FromRange(0, source.Length);
        FnisListParseResult<FnisRotation> result = FnisRotationParser.ParseRotation(source, line);

        Assert.True(result.IsFailure);
        Assert.Equal(FnisListParseErrorKind.InvalidSyntax, result.Error);
    }

    [Fact]
    public void Quaternion_ThrowsWhenAccessedAsZAngle() {
        const string source = "RD 1.5 0 0 0 1";
        TextSpan line = TextSpan.FromRange(0, source.Length);
        FnisListParseResult<FnisRotation> result = FnisRotationParser.ParseRotation(source, line);

        FnisRotation rotation = result.Value;

        Assert.Equal(FnisRotationKind.Quaternion, rotation.Kind);
        Assert.Throws<InvalidOperationException>(() => rotation.ZAngle);
    }

    [Fact]
    public void DeltaZAngle_ThrowsWhenAccessedAsQuaternion() {
        const string source = "RD 2.0 90";
        TextSpan line = TextSpan.FromRange(0, source.Length);
        FnisListParseResult<FnisRotation> result = FnisRotationParser.ParseRotation(source, line);

        FnisRotation rotation = result.Value;

        Assert.Equal(FnisRotationKind.DeltaZAngle, rotation.Kind);
        Assert.Throws<InvalidOperationException>(() => rotation.Quaternion);
    }

    [Fact]
    public void Quaternion_ToQuaternion_ReturnsStoredQuaternion() {
        const string source = "RD 1.5 0 0.5 -0.5 1";
        TextSpan line = TextSpan.FromRange(0, source.Length);
        FnisListParseResult<FnisRotation> result = FnisRotationParser.ParseRotation(source, line);

        FnisRotation rotation = result.Value;
        Quaternion quaternion = rotation.ToQuaternion();

        Assert.Equal(0.0f, quaternion.X);
        Assert.Equal(0.5f, quaternion.Y);
        Assert.Equal(-0.5f, quaternion.Z);
        Assert.Equal(1.0f, quaternion.W);
    }

    [Fact]
    public void DeltaZAngle_ToQuaternion_ConvertsDegreesToQuaternion() {
        const string source = "RD 2.0 90";
        TextSpan line = TextSpan.FromRange(0, source.Length);
        FnisListParseResult<FnisRotation> result = FnisRotationParser.ParseRotation(source, line);

        FnisRotation rotation = result.Value;

        Quaternion quaternion = rotation.ToQuaternion();

        float expected = MathF.Sqrt(0.5f);

        Assert.Equal(0.0f, quaternion.X);
        Assert.Equal(0.0f, quaternion.Y);
        Assert.Equal(expected, quaternion.Z, 5);
        Assert.Equal(expected, quaternion.W, 5);
    }

    [Fact]
    public void ZeroDeltaZAngle_ToQuaternion_ReturnsIdentity() {
        const string source = "RD 0.5 0.0";
        TextSpan line = TextSpan.FromRange(0, source.Length);
        FnisListParseResult<FnisRotation> result = FnisRotationParser.ParseRotation(source, line);

        FnisRotation rotation = result.Value;

        Quaternion quaternion = rotation.ToQuaternion();

        Assert.Equal(0.0f, quaternion.X);
        Assert.Equal(0.0f, quaternion.Y);
        Assert.Equal(0.0f, quaternion.Z);
        Assert.Equal(1.0f, quaternion.W);
    }
}
