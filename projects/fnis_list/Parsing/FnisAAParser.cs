using System;
using System.Collections.Generic;
using System.Globalization;

namespace fnis_list;

/// <summary>
/// Parses FNIS alternate-animation blocks.
/// </summary>
public static class FnisAAParser
{
    /// <summary>
    /// Parses an alternate-animation block beginning with <c>AAprefix</c>.
    /// </summary>
    public static FnisListParseResult<FnisAlternateAnimation> Parse(ReadOnlySpan<char> source, ref int position)
    {
        TextSpan prefixLine = GetLine(source, position);

        if (!TryParsePrefix(source, prefixLine, out TextSpan prefix))
        {
            return FnisListParseResult<FnisAlternateAnimation>.Failure(FnisListParseErrorKind.InvalidSyntax, prefixLine.Pos);
        }

        position = FnisLineParser.NextLine(source, prefixLine.End);

        List<FnisAASet> sets = new();
        List<FnisAnimTrigger> triggers = new();

        while (position < source.Length)
        {
            TextSpan line = GetLine(source, position);
            ReadOnlySpan<char> text = line.Slice(source);

            if (IsPrefixLine(text))
            {
                break;
            }

            if (IsSetLine(text))
            {
                if (!TryParseSet(source, line, out FnisAASet set))
                {
                    return FnisListParseResult<FnisAlternateAnimation>.Failure(FnisListParseErrorKind.InvalidSyntax, line.Pos);
                }

                sets.Add(set);
                position = FnisLineParser.NextLine(source, line.End);
                continue;
            }

            if (IsTriggerLine(text))
            {
                if (!TryParseTrigger(source, line, out FnisAnimTrigger trigger))
                {
                    return FnisListParseResult<FnisAlternateAnimation>.Failure(FnisListParseErrorKind.InvalidSyntax, line.Pos);
                }

                triggers.Add(trigger);
                position = FnisLineParser.NextLine(source, line.End);
                continue;
            }

            break;
        }

        return FnisListParseResult<FnisAlternateAnimation>.Success(new FnisAlternateAnimation(prefix, sets, triggers), position);
    }

    private static bool TryParsePrefix(
        ReadOnlySpan<char> source,
        TextSpan line,
        out TextSpan prefix)
    {
        prefix = default;

        int position = line.Pos;
        int end = line.End;

        if (!TryReadToken(source, ref position, end, out TextSpan command) ||
            !command.Slice(source).SequenceEqual("AAprefix"))
        {
            return false;
        }

        return TryReadToken(source, ref position, end, out prefix);
    }

    private static bool TryParseSet(
        ReadOnlySpan<char> source,
        TextSpan line,
        out FnisAASet set)
    {
        set = default;

        int position = line.Pos;
        int end = line.End;

        if (!TryReadToken(source, ref position, end, out TextSpan command) ||
            !command.Slice(source).SequenceEqual("AAset"))
        {
            return false;
        }

        if (!TryReadToken(source, ref position, end, out TextSpan group) ||
            !TryReadToken(source, ref position, end, out TextSpan slotsSpan))
        {
            return false;
        }

        if (!ulong.TryParse(
            slotsSpan.Slice(source),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out ulong slots))
        {
            return false;
        }

        set = new FnisAASet(group, slots);
        return true;
    }

    private static bool TryParseTrigger(
        ReadOnlySpan<char> source,
        TextSpan line,
        out FnisAnimTrigger trigger)
    {
        trigger = default;

        int position = line.Pos;
        int end = line.End;

        if (!TryReadToken(source, ref position, end, out TextSpan command) ||
            !command.Slice(source).SequenceEqual("T"))
        {
            return false;
        }

        if (!TryReadToken(source, ref position, end, out TextSpan animName))
        {
            return false;
        }

        List<FnisTriggerData> triggerData = new();

        while (TryReadToken(source, ref position, end, out TextSpan token))
        {
            ReadOnlySpan<char> value = token.Slice(source);
            int slash = value.IndexOf('/');

            if (slash <= 0 || slash >= value.Length - 1)
            {
                return false;
            }

            if (!float.TryParse(
                value[(slash + 1)..],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float time))
            {
                return false;
            }

            triggerData.Add(
                new FnisTriggerData(
                    new TextSpan(token.Pos, slash),
                    time));
        }

        if (triggerData.Count == 0)
        {
            return false;
        }

        trigger = new FnisAnimTrigger(animName, triggerData);
        return true;
    }

    private static bool IsPrefixLine(ReadOnlySpan<char> line)
    {
        return StartsWithCommand(line, "AAprefix");
    }

    private static bool IsSetLine(ReadOnlySpan<char> line)
    {
        return StartsWithCommand(line, "AAset");
    }

    private static bool IsTriggerLine(ReadOnlySpan<char> line)
    {
        return StartsWithCommand(line, "T");
    }

    private static bool StartsWithCommand(
        ReadOnlySpan<char> line,
        ReadOnlySpan<char> command)
    {
        int position = 0;

        while (position < line.Length && char.IsWhiteSpace(line[position]))
        {
            position++;
        }

        return line[position..].StartsWith(command, StringComparison.Ordinal);
    }

    private static bool TryReadToken(
        ReadOnlySpan<char> source,
        ref int position,
        int end,
        out TextSpan token)
    {
        while (position < end && char.IsWhiteSpace(source[position]))
        {
            position++;
        }

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

    private static TextSpan GetLine(
        ReadOnlySpan<char> source,
        int position)
    {
        int end = FnisLineParser.FindLineEnd(source, position);
        return TextSpan.FromRange(position, end);
    }
}
