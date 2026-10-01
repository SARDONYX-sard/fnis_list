using System;
using System.Runtime.CompilerServices;

namespace FnisList;

internal readonly struct FnisTokenSpan {
    public TextSpan Span { get; }

    public int NextPos { get; }

    public FnisTokenSpan(TextSpan span, int nextPos) {
        this.Span = span;
        this.NextPos = nextPos;
    }
}

internal static class FnisTokenParser {
    public static FnisListParseResult<FnisTokenSpan> Parse(ReadOnlySpan<char> source, TextSpan input) {
        int position = input.Pos;
        int end = input.End;

        SkipWhitespace(source, ref position, end);

        if (position >= end) {
            return FnisListParseResult<FnisTokenSpan>.Failure(FnisListParseErrorKind.UnexpectedEnd, position);
        }

        int start = position;

        while (position < end && !char.IsWhiteSpace(source[position])) {
            position++;
        }

        return FnisListParseResult<FnisTokenSpan>.Success(new FnisTokenSpan(TextSpan.FromRange(start, position), position), position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipWhitespace(ReadOnlySpan<char> source, ref int position, int end) {
        while (position < end && char.IsWhiteSpace(source[position])) {
            position++;
        }
    }
}
