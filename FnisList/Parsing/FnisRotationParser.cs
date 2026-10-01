using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace FnisList;

/// <summary>
/// Parses FNIS motion and rotation data.
/// </summary>
public static class FnisRotationParser {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMotionLine(ReadOnlySpan<char> line) {
        return line.Length >= 2 && line[0] is 'M' or 'm' && line[1] is 'D' or 'd' && (line.Length == 2 || char.IsWhiteSpace(line[2]));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsRotationLine(ReadOnlySpan<char> line) {
        return line.Length >= 2 && line[0] is 'R' or 'r' && line[1] is 'D' or 'd' && (line.Length == 2 || char.IsWhiteSpace(line[2]));
    }

    /// <summary>
    /// Parses an FNIS <c>MD</c> definition.
    /// </summary>
    public static FnisListParseResult<FnisMotion> ParseMotion(ReadOnlySpan<char> source, TextSpan line) {
        ReadOnlySpan<char> text = line.Slice(source);
        int position = 0;

        if (!TryMatchTokenIgnoreCase(text, ref position, "MD")) {
            return FailureMotion<FnisMotion>(line.Pos + position);
        }

        if (!TryReadFloat(text, ref position, out float time) ||
            !TryReadFloat(text, ref position, out float deltaX) ||
            !TryReadFloat(text, ref position, out float deltaY) ||
            !TryReadFloat(text, ref position, out float deltaZ)) {
            return FailureMotion<FnisMotion>(line.Pos + position);
        }

        SkipWhitespace(text, ref position);

        if (position != text.Length) {
            return FailureMotion<FnisMotion>(line.Pos + position);
        }

        return FnisListParseResult<FnisMotion>.Success(new FnisMotion(time, deltaX, deltaY, deltaZ), line.End);
    }

    /// <summary>
    /// Parses an FNIS <c>RD</c> definition.
    /// </summary>
    public static FnisListParseResult<FnisRotation> ParseRotation(ReadOnlySpan<char> source, TextSpan line) {
        ReadOnlySpan<char> text = line.Slice(source); int position = 0;

        if (!TryMatchTokenIgnoreCase(text, ref position, "RD")) {
            return FailureRotation<FnisRotation>(line.Pos + position);
        }

        if (!TryReadFloat(text, ref position, out float time)) {
            return FailureRotation<FnisRotation>(line.Pos + position);
        }

        int dataStart = position;

        // The quaternion format is attempted first. If it fails,
        // the parser falls back to the Z-axis angle format.
        if (TryParseQuaternion(text, ref position, time, out FnisRotation quaternion)) {
            return FnisListParseResult<FnisRotation>.Success(quaternion, line.End);
        }

        position = dataStart;

        if (TryParseZAngle(text, ref position, time, out FnisRotation zAngle)) {
            return FnisListParseResult<FnisRotation>.Success(zAngle, line.End);
        }

        return FailureRotation<FnisRotation>(line.Pos + dataStart);
    }

    private static bool TryParseQuaternion(ReadOnlySpan<char> source, ref int position, float time, out FnisRotation rotation) {
        rotation = default;

        int start = position;

        if (!TryReadFloat(source, ref position, out float x) ||
            !TryReadFloat(source, ref position, out float y) ||
            !TryReadFloat(source, ref position, out float z) ||
            !TryReadFloat(source, ref position, out float w)) {
            position = start;
            return false;
        }

        SkipWhitespace(source, ref position);

        if (position != source.Length) {
            position = start;
            return false;
        }

        rotation = new FnisRotation(time, FnisRotationKind.Quaternion, x, y, z, w);

        return true;
    }

    private static bool TryParseZAngle(ReadOnlySpan<char> source, ref int position, float time, out FnisRotation rotation) {
        rotation = default;

        if (!TryReadFloat(source, ref position, out float angle)) {
            return false;
        }

        SkipWhitespace(source, ref position);

        if (position != source.Length) {
            return false;
        }

        rotation = new FnisRotation(time, FnisRotationKind.DeltaZAngle, default, default, angle, default);

        return true;
    }

    private static bool TryMatchTokenIgnoreCase(ReadOnlySpan<char> source, ref int position, ReadOnlySpan<char> token) {
        SkipWhitespace(source, ref position);

        if (position + token.Length > source.Length) {
            return false;
        }

        if (!source.Slice(position, token.Length).Equals(token, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        position += token.Length;

        if (position < source.Length && !char.IsWhiteSpace(source[position])) {
            return false;
        }

        return true;
    }

    private static bool TryReadFloat(ReadOnlySpan<char> source, ref int position, out float value) {
        SkipWhitespace(source, ref position);

        int start = position;

        while (position < source.Length && !char.IsWhiteSpace(source[position])) {
            position++;
        }

        if (start == position) {
            value = default;
            return false;
        }

        return float.TryParse(source.Slice(start, position - start), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SkipWhitespace(ReadOnlySpan<char> source, ref int position) {
        while (position < source.Length && char.IsWhiteSpace(source[position])) {
            position++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FnisListParseResult<T> FailureMotion<T>(int position) {
        return FnisListParseResult<T>.Failure(FnisListParseErrorKind.InvalidMotion, position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FnisListParseResult<T> FailureRotation<T>(int position) {
        return FnisListParseResult<T>.Failure(FnisListParseErrorKind.InvalidRotation, position);
    }
}
