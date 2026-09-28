using System;
using System.Globalization;

namespace fnis_list;

/// <summary>
/// Parses FNIS <c>AnimVar</c> definitions.
/// </summary>
public static class FnisAnimVarParser
{
    /// <summary>
    /// Parses an <c>AnimVar</c> line.
    /// </summary>
    public static FnisListParseResult<FnisAnimVarData> Parse(ReadOnlySpan<char> source, TextSpan input)
    {
        int position = input.Pos;
        int end = input.End;

        if ((uint)position > (uint)source.Length || (uint)end > (uint)source.Length)
        {
            return FnisListParseResult<FnisAnimVarData>.Failure(FnisListParseErrorKind.InvalidSource, position);
        }

        SkipWhitespace(source, ref position, end);

        if (!TryConsumeToken(source, ref position, end, "AnimVar"))
        {
            return Failure(position);
        }

        if (!TryReadToken(source, ref position, end, out TextSpan name))
        {
            return Failure(position);
        }

        if (!TryReadToken(source, ref position, end, out TextSpan type))
        {
            return Failure(position);
        }

        if (!TryReadToken(source, ref position, end, out TextSpan value))
        {
            return Failure(position);
        }

        ReadOnlySpan<char> typeText = type.Slice(source);
        ReadOnlySpan<char> valueText = value.Slice(source);

        if (typeText.Equals("BOOL", StringComparison.OrdinalIgnoreCase))
        {
            if (valueText.SequenceEqual("0"))
            {
                return Success(new FnisAnimVarData(name, FnisAnimVarValue.FromBool(false)), position);
            }

            if (valueText.SequenceEqual("1"))
            {
                return Success(new FnisAnimVarData(name, FnisAnimVarValue.FromBool(true)), position);
            }

            return Failure(value.Pos);
        }

        if (typeText.Equals("INT32", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            {
                return Failure(value.Pos);
            }

            return Success(new FnisAnimVarData(name, FnisAnimVarValue.FromInt32(intValue)), position);
        }

        if (typeText.Equals("REAL", StringComparison.OrdinalIgnoreCase))
        {
            if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float realValue))
            {
                return Failure(value.Pos);
            }

            return Success(new FnisAnimVarData(name, FnisAnimVarValue.FromReal(realValue)), position);
        }

        return Failure(type.Pos);
    }

    private static FnisListParseResult<FnisAnimVarData> Success(FnisAnimVarData value, int position)
    {
        return FnisListParseResult<FnisAnimVarData>.Success(value, position);
    }

    private static FnisListParseResult<FnisAnimVarData> Failure(int position)
    {
        return FnisListParseResult<FnisAnimVarData>.Failure(FnisListParseErrorKind.InvalidSyntax, position);
    }

    private static void SkipWhitespace(ReadOnlySpan<char> source, ref int position, int end)
    {
        while (position < end && char.IsWhiteSpace(source[position]))
        {
            position++;
        }
    }

    private static bool TryConsumeToken(ReadOnlySpan<char> source, ref int position, int end, ReadOnlySpan<char> token)
    {
        SkipWhitespace(source, ref position, end);

        if (position + token.Length > end)
        {
            return false;
        }

        if (!source.Slice(position, token.Length).Equals(token, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        position += token.Length;

        if (position < end && !char.IsWhiteSpace(source[position]))
        {
            return false;
        }

        return true;
    }

    private static bool TryReadToken(ReadOnlySpan<char> source, ref int position, int end, out TextSpan token)
    {
        SkipWhitespace(source, ref position, end);

        if (position >= end)
        {
            token = default;
            return false;
        }

        int start = position;

        while (position < end && !char.IsWhiteSpace(source[position]))
        {
            position++;
        }

        token = new TextSpan(start, position - start);
        return true;
    }
}
