using System;

namespace fnis_list;

internal readonly struct FnisTokenData
{
    public TextSpan Span { get; }

    public int NextPos { get; }

    public FnisTokenData(TextSpan span, int nextPos)
    {
        this.Span = span;
        this.NextPos = nextPos;
    }
}

internal static class FnisTokenParser
{
    public static FnisListParseResult<FnisTokenData> Parse(ReadOnlySpan<char> source, TextSpan input)
    {
        int position = input.Pos;
        int end = input.End;

        SkipWhitespace(source, ref position, end);

        if (position >= end)
        {
            return FnisListParseResult<FnisTokenData>.Failure(FnisListParseErrorKind.UnexpectedEnd, position);
        }

        int start = position;

        while (position < end && !char.IsWhiteSpace(source[position]))
        {
            position++;
        }

        return FnisListParseResult<FnisTokenData>.Success(new FnisTokenData(TextSpan.FromRange(start, position), position), position);
    }

    public static int CountTokens(ReadOnlySpan<char> source)
    {
        int count = 0;
        int position = 0;

        while (TryGetNextToken(source, ref position, out _))
        {
            count++;
        }

        return count;
    }

    public static bool TryGetToken(ReadOnlySpan<char> source, int index, out TextSpan span)
    {
        span = default;

        if (index < 0)
        {
            return false;
        }

        int position = 0;
        int current = 0;

        while (TryGetNextToken(source, ref position, out TextSpan currentSpan))
        {
            if (current == index)
            {
                span = currentSpan;
                return true;
            }

            current++;
        }

        return false;
    }

    private static bool TryGetNextToken(ReadOnlySpan<char> source, ref int position, out TextSpan span)
    {
        while (position < source.Length && char.IsWhiteSpace(source[position]))
        {
            position++;
        }

        if (position >= source.Length)
        {
            span = default;
            return false;
        }

        int start = position;

        while (position < source.Length && !char.IsWhiteSpace(source[position]))
        {
            position++;
        }

        span = TextSpan.FromRange(start, position);

        return true;
    }

    private static void SkipWhitespace(ReadOnlySpan<char> source, ref int position, int end)
    {
        while (position < end && char.IsWhiteSpace(source[position]))
        {
            position++;
        }
    }
}
