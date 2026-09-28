using System;

namespace fnis_list;

public readonly struct FnisTypeData
{
    public FnisAnimType Type { get; }

    public int NextPos { get; }

    public FnisTypeData(FnisAnimType type, int nextPos)
    {
        this.Type = type;
        this.NextPos = nextPos;
    }
}

public static class FnisTypeParser
{
    public static FnisListParseResult<FnisTypeData> Parse(ReadOnlySpan<char> source, TextSpan input)
    {
        int position = input.Pos;
        int end = input.End;

        SkipWhitespace(source, ref position, end);

        if (position >= end)
        {
            return FnisListParseResult<FnisTypeData>.Failure(FnisListParseErrorKind.UnexpectedEnd, position);
        }

        if (TryConsumeToken(source, ref position, end, "AnimVar"))
        {
            return Success(FnisAnimType.AnimVar, position);
        }

        if (TryConsumeToken(source, ref position, end, "AAPrefix"))
        {
            return Success(FnisAnimType.Alternate, position);
        }

        if (TryConsumeToken(source, ref position, end, "fuo"))
        {
            return Success(FnisAnimType.FurnitureOptimized, position);
        }

        if (TryConsumeToken(source, ref position, end, "ofa"))
        {
            return Success(FnisAnimType.OffsetArm, position);
        }


        if (TryConsumeToken(source, ref position, end, "ch"))
        {
            return Success(FnisAnimType.Chair, position);
        }

        if (TryConsumeToken(source, ref position, end, "fu"))
        {
            return Success(FnisAnimType.Furniture, position);
        }

        if (TryConsumeToken(source, ref position, end, "km"))
        {
            return Success(FnisAnimType.KillMove, position);
        }

        if (TryConsumeToken(source, ref position, end, "pa"))
        {
            return Success(FnisAnimType.Paired, position);
        }

        if (TryConsumeToken(source, ref position, end, "so"))
        {
            return Success(FnisAnimType.SequencedOptimized, position);
        }

        if (TryConsumeToken(source, ref position, end, "+"))
        {
            return Success(FnisAnimType.SequencedContinued, position);
        }

        if (TryConsumeToken(source, ref position, end, "b"))
        {
            return Success(FnisAnimType.Basic, position);
        }

        if (TryConsumeToken(source, ref position, end, "o"))
        {
            return Success(FnisAnimType.AnimObject, position);
        }

        if (TryConsumeToken(source, ref position, end, "s"))
        {
            return Success(FnisAnimType.Sequenced, position);
        }

        return FnisListParseResult<FnisTypeData>.Failure(FnisListParseErrorKind.InvalidSyntax, position);
    }

    private static FnisListParseResult<FnisTypeData> Success(FnisAnimType type, int nextPos)
    {
        return FnisListParseResult<FnisTypeData>.Success(new FnisTypeData(type, nextPos), nextPos);
    }

    /// <remarks>
    /// NOTE: Case ignored
    /// </remarks>
    private static bool TryConsumeToken(ReadOnlySpan<char> source, ref int position, int end, ReadOnlySpan<char> token)
    {
        if (position + token.Length > end)
        {
            return false;
        }

        int start = position;

        if (!source.Slice(position, token.Length).Equals(token, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        position += token.Length;

        if (position < end && !char.IsWhiteSpace(source[position]) && source[position] != ',')
        {
            position = start;
            return false;
        }

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
