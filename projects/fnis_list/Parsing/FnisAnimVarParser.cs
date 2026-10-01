using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace fnis_list;

/// <summary>
/// Parses FNIS <c>AnimVar</c> definitions.
/// </summary>
public static class FnisAnimVarParser {
    /// <summary>
    /// Parses an <c>AnimVar</c> line.
    /// </summary>
    public static FnisListParseResult<FnisAnimVarSpan> Parse(ReadOnlySpan<char> source, TextSpan lineSpan) {
        int position = lineSpan.Pos;
        int end = lineSpan.End;

        if ((uint)position > (uint)source.Length || (uint)end > (uint)source.Length) {
            return FnisListParseResult<FnisAnimVarSpan>.Failure(FnisListParseErrorKind.InvalidSourceRange, position);
        }

        // AnimVar <Name> [ BOOL | INT32 | REAL ] <numeric_value>
        FnisTokenParser.SkipWhitespace(source, ref position, end);

        // keyword
        if (!TryConsumeToken(source, ref position, end, "AnimVar")) {
            return Failure(FnisListParseErrorKind.InvalidAnimVarDefinition, position);
        }

        // Name
        if (!TryReadToken(source, ref position, end, out TextSpan name)) {
            return Failure(FnisListParseErrorKind.InvalidAnimVarDefinition, position);
        }

        // Type
        if (!TryReadToken(source, ref position, end, out TextSpan type)) {
            return Failure(FnisListParseErrorKind.InvalidAnimVarType, position);
        }

        // Value
        if (!TryReadToken(source, ref position, end, out TextSpan value)) {
            return Failure(FnisListParseErrorKind.InvalidAnimVarValue, position);
        }

        ReadOnlySpan<char> typeText = type.Slice(source);
        ReadOnlySpan<char> valueText = value.Slice(source);

        if (typeText.Equals("BOOL", StringComparison.OrdinalIgnoreCase)) {
            if (valueText.SequenceEqual("0")) {
                return Success(new FnisAnimVarSpan(name, FnisAnimVarValue.FromBool(false)), position);
            }

            if (valueText.SequenceEqual("1")) {
                return Success(new FnisAnimVarSpan(name, FnisAnimVarValue.FromBool(true)), position);
            }

            return Failure(FnisListParseErrorKind.InvalidAnimVarValue, value.Pos);
        }

        if (typeText.Equals("INT32", StringComparison.OrdinalIgnoreCase)) {
            if (!int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue)) {
                return Failure(FnisListParseErrorKind.InvalidAnimVarValue, value.Pos);
            }

            return Success(new FnisAnimVarSpan(name, FnisAnimVarValue.FromInt32(intValue)), position);
        }

        if (typeText.Equals("REAL", StringComparison.OrdinalIgnoreCase)) {
            if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float realValue)) {
                return Failure(FnisListParseErrorKind.InvalidAnimVarValue, value.Pos);
            }

            return Success(new FnisAnimVarSpan(name, FnisAnimVarValue.FromReal(realValue)), position);
        }

        return Failure(FnisListParseErrorKind.InvalidAnimVarValue, type.Pos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FnisListParseResult<FnisAnimVarSpan> Success(FnisAnimVarSpan value, int position) {
        return FnisListParseResult<FnisAnimVarSpan>.Success(value, position);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static FnisListParseResult<FnisAnimVarSpan> Failure(FnisListParseErrorKind error, int position) {
        return FnisListParseResult<FnisAnimVarSpan>.Failure(error, position);
    }

    private static bool TryConsumeToken(ReadOnlySpan<char> source, ref int position, int end, ReadOnlySpan<char> token) {
        FnisTokenParser.SkipWhitespace(source, ref position, end);

        if (position + token.Length > end) {
            return false;
        }

        if (!source.Slice(position, token.Length).Equals(token, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        position += token.Length;

        if (position < end && !char.IsWhiteSpace(source[position])) {
            return false;
        }

        return true;
    }

    private static bool TryReadToken(ReadOnlySpan<char> source, ref int position, int end, out TextSpan token) {
        FnisTokenParser.SkipWhitespace(source, ref position, end);

        if (position >= end) {
            token = default;
            return false;
        }

        int start = position;

        while (position < end && !char.IsWhiteSpace(source[position])) {
            position++;
        }

        token = new TextSpan(start, position - start);
        return true;
    }
}
