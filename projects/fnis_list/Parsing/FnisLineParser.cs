using System;
using System.Collections.Generic;
using System.Globalization;

namespace fnis_list;

public static class FnisLineParser
{
    public static FnisListParseResult<FnisAnimation> Parse(ReadOnlySpan<char> source, TextSpan line)
    {
        FnisListParseResult<FnisTypeData> typeResult = FnisTypeParser.Parse(source, line);
        if (typeResult.IsFailure)
        {
            return FnisListParseResult<FnisAnimation>.Failure(typeResult.Error, typeResult.Pos);
        }
        FnisTypeData type = typeResult.Value;

        FnisListParseResult<FnisFlagData> flagResult = FnisFlagParser.ParseLine(source, TextSpan.FromRange(type.NextPos, line.End));
        if (flagResult.IsFailure)
        {
            return FnisListParseResult<FnisAnimation>.Failure(flagResult.Error, flagResult.Pos);
        }
        FnisFlagData flags = flagResult.Value;

        FnisListParseResult<FnisTokenData> eventResult = FnisTokenParser.Parse(source, TextSpan.FromRange(flags.NextPos, line.End));
        if (eventResult.IsFailure)
        {
            return FnisListParseResult<FnisAnimation>.Failure(eventResult.Error, eventResult.Pos);
        }
        FnisTokenData eventData = eventResult.Value;

        FnisListParseResult<FnisTokenData> fileResult = FnisTokenParser.Parse(source, TextSpan.FromRange(eventData.NextPos, line.End));
        if (fileResult.IsFailure)
        {
            return FnisListParseResult<FnisAnimation>.Failure(fileResult.Error, fileResult.Pos);
        }
        FnisTokenData fileData = fileResult.Value;

        bool isPairAndKill = type.Type is FnisAnimType.Paired or FnisAnimType.KillMove;
        FnisListParseResult<List<FnisAnimObjectData>> objectResult =
            FnisAnimObjectParser.Parse(source, TextSpan.FromRange(fileData.NextPos, line.End), isPairAndKill);
        if (objectResult.IsFailure)
        {
            return FnisListParseResult<FnisAnimation>.Failure(objectResult.Error, objectResult.Pos);
        }
        List<FnisAnimObjectData> objects = objectResult.Value;

        FnisAnimation animation = new FnisAnimation(
            type.Type,
            eventData.Span,
            fileData.Span,
            objects,
            flags.Flags,
            flags.BlendTime,
            flags.Duration,
            flags.Triggers,
            flags.Triggers2);

        return FnisListParseResult<FnisAnimation>.Success(animation, fileData.NextPos);
    }

    public static int FindLineEnd(ReadOnlySpan<char> source, int position)
    {
        int index = source[position..].IndexOfAny('\r', '\n');

        if (index < 0)
        {
            return source.Length;
        }

        return position + index;
    }

    public static int NextLine(ReadOnlySpan<char> source, int lineEnd)
    {
        if (lineEnd >= source.Length)
        {
            return source.Length;
        }

        if (source[lineEnd] == '\r')
        {
            int position = lineEnd + 1;
            if (position < source.Length && source[position] == '\n')
            {
                position++;
            }

            return position;
        }

        if (source[lineEnd] == '\n')
        {
            return lineEnd + 1;
        }

        return lineEnd;
    }

    public static void SkipWhitespaceAndComments(ReadOnlySpan<char> source, ref int position, int end)
    {
        while (position < end)
        {
            while (position < end && char.IsWhiteSpace(source[position]))
            {
                position++;
            }

            if (position >= end)
            {
                return;
            }

            if (source[position] != '\'')
            {
                return;
            }

            int lineEnd = FindLineEnd(source, position);

            position = NextLine(
                source,
                lineEnd);
        }
    }

    public static bool IsVersionLine(ReadOnlySpan<char> line)
    {
        int position = 0;

        SkipWhiteSpace(line, ref position);

        if (position >= line.Length)
        {
            return false;
        }

        return line[position..].StartsWith("Version", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryParseVersion(ReadOnlySpan<char> line, out int major, out int minor)
    {
        major = 0;
        minor = 0;

        int position = 0;

        SkipWhiteSpace(line, ref position);

        if (!ConsumeLiteral(line, ref position, "Version"))
        {
            return false;
        }

        SkipWhiteSpace(line, ref position);
        if (position < line.Length && (line[position] == 'v' || line[position] == 'V'))
        {
            position++;

            SkipWhiteSpace(line, ref position);
        }

        if (!TryReadInteger(line, ref position, out major))
        {
            return false;
        }

        SkipWhiteSpace(line, ref position);
        if (position < line.Length && line[position] == '.')
        {
            position++;

            if (!TryReadInteger(line, ref position, out minor))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsContinuationLine(ReadOnlySpan<char> line)
    {
        int position = 0;

        SkipWhiteSpace(line, ref position);

        if (position >= line.Length)
        {
            return false;
        }

        return TryConsumeToken(line, ref position, "+");
    }

    private static bool TryConsumeToken(ReadOnlySpan<char> source, ref int position, ReadOnlySpan<char> token)
    {
        if (position + token.Length > source.Length)
        {
            return false;
        }

        int start = position;

        if (!source[position..].StartsWith(token, StringComparison.Ordinal))
        {
            return false;
        }

        position += token.Length;

        if (position < source.Length && !char.IsWhiteSpace(source[position]) &&
            source[position] != ',')
        {
            position = start;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Ignore case
    /// </summary>
    private static bool ConsumeLiteral(ReadOnlySpan<char> source, ref int position, ReadOnlySpan<char> value)
    {
        if (position + value.Length > source.Length)
        {
            return false;
        }

        if (!source[position..].StartsWith(value, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        position += value.Length;
        return true;
    }

    private static bool TryReadInteger(ReadOnlySpan<char> source, ref int position, out int value)
    {
        int start = position;

        while (position < source.Length && char.IsDigit(source[position]))
        {
            position++;
        }

        if (start == position)
        {
            value = 0;
            return false;
        }

        return int.TryParse(source[start..position], NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    private static void SkipWhiteSpace(ReadOnlySpan<char> source, ref int position)
    {
        while (position < source.Length && char.IsWhiteSpace(source[position]))
        {
            position++;
        }
    }
}
