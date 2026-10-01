using System;

namespace fnis_list;

public readonly struct FnisTypeSpan {
    public FnisAnimType Type { get; }

    public int NextPos { get; }

    public FnisTypeSpan(FnisAnimType type, int nextPos) {
        this.Type = type;
        this.NextPos = nextPos;
    }
}

public static class FnisTypeParser {
    public static FnisListParseResult<FnisTypeSpan> Parse(ReadOnlySpan<char> source, TextSpan input) {
        int position = input.Pos;
        int end = input.End;

        FnisTokenParser.SkipWhitespace(source, ref position, end);

        if (position >= end) {
            return FnisListParseResult<FnisTypeSpan>.Failure(FnisListParseErrorKind.UnexpectedEnd, position);
        }

        if (TryMatchTokenIgnoreCase(source, ref position, end, "AnimVar")) {
            return Success(FnisAnimType.AnimVar, position);
        }

        if (TryMatchTokenIgnoreCase(source, ref position, end, "AAPrefix")) {
            return Success(FnisAnimType.Alternate, position);
        }

        if (TryMatchTokenIgnoreCase(source, ref position, end, "fuo")) {
            return Success(FnisAnimType.FurnitureOptimized, position);
        }

        if (TryMatchTokenIgnoreCase(source, ref position, end, "ofa")) {
            return Success(FnisAnimType.OffsetArm, position);
        }


        if (TryMatchTokenIgnoreCase(source, ref position, end, "ch")) {
            return Success(FnisAnimType.Chair, position);
        }

        if (TryMatchTokenIgnoreCase(source, ref position, end, "fu")) {
            return Success(FnisAnimType.Furniture, position);
        }

        if (TryMatchTokenIgnoreCase(source, ref position, end, "km")) {
            return Success(FnisAnimType.KillMove, position);
        }

        if (TryMatchTokenIgnoreCase(source, ref position, end, "pa")) {
            return Success(FnisAnimType.Paired, position);
        }

        if (TryMatchTokenIgnoreCase(source, ref position, end, "so")) {
            return Success(FnisAnimType.SequencedOptimized, position);
        }

        if (TryMatchTokenIgnoreCase(source, ref position, end, "+")) {
            return Success(FnisAnimType.SequencedContinued, position);
        }

        if (TryMatchTokenIgnoreCase(source, ref position, end, "b")) {
            return Success(FnisAnimType.Basic, position);
        }

        if (TryMatchTokenIgnoreCase(source, ref position, end, "o")) {
            return Success(FnisAnimType.AnimObject, position);
        }

        if (TryMatchTokenIgnoreCase(source, ref position, end, "s")) {
            return Success(FnisAnimType.Sequenced, position);
        }

        return FnisListParseResult<FnisTypeSpan>.Failure(FnisListParseErrorKind.InvalidAnimationType, position);
    }

    private static FnisListParseResult<FnisTypeSpan> Success(FnisAnimType type, int nextPos) {
        return FnisListParseResult<FnisTypeSpan>.Success(new FnisTypeSpan(type, nextPos), nextPos);
    }

    private static bool TryMatchTokenIgnoreCase(ReadOnlySpan<char> source, ref int position, int end, ReadOnlySpan<char> token) {
        if (position + token.Length > end) {
            return false;
        }

        int start = position;

        if (!source.Slice(position, token.Length).Equals(token, StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        position += token.Length;

        if (position < end && !char.IsWhiteSpace(source[position]) && source[position] != ',') {
            position = start;
            return false;
        }

        return true;
    }
}
