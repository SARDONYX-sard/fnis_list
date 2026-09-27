using System;
using System.Collections.Generic;
using System.Globalization;

namespace fnis_list;

/// <summary>
/// Parsed FNIS animation flag data.
/// </summary>
public readonly struct FnisFlagData
{
    /// <summary>
    /// Flags span next pos.
    /// </summary>
    public int NextPos { get; }

    /// <summary>
    /// Gets the parsed animation flags.
    /// </summary>
    public FnisAnimFlags Flags { get; }

    /// <summary>
    /// Gets the parsed blend time.
    /// </summary>
    public float? BlendTime { get; }

    /// <summary>
    /// Gets the parsed duration.
    /// </summary>
    public float? Duration { get; }

    /// <summary>
    /// Gets the parsed normal triggers.
    /// </summary>
    public List<FnisTriggerData> Triggers { get; }

    /// <summary>
    /// Gets the parsed secondary triggers.
    /// </summary>
    public List<FnisTriggerData> Triggers2 { get; }

    /// <summary>
    /// Initializes parsed FNIS flag data.
    /// </summary>
    public FnisFlagData(
        int nextPos,
        FnisAnimFlags flags,
        float? blendTime,
        float? duration,
        List<FnisTriggerData> triggers,
        List<FnisTriggerData> triggers2
    )
    {
        this.NextPos = nextPos;
        this.Flags = flags;
        this.BlendTime = blendTime;
        this.Duration = duration;
        this.Triggers = triggers;
        this.Triggers2 = triggers2;
    }
}

/// <summary>
/// Parses FNIS animation modifier flags.
/// </summary>
public static class FnisFlagParser
{
    /// <summary>
    /// Parses optional FNIS animation flags from a line.
    /// </summary>
    public static FnisListParseResult<FnisFlagData> ParseLine(ReadOnlySpan<char> source, TextSpan input)
    {
        int position = input.Pos;
        int end = input.End;

        if ((uint)position > (uint)source.Length || (uint)end > (uint)source.Length)
        {
            return FnisListParseResult<FnisFlagData>.Failure(FnisListParseErrorKind.InvalidSource, position);
        }

        while (position < end && char.IsWhiteSpace(source[position]))
        {
            position++;
        }

        if (position >= end || source[position] != '-')
        {
            return FnisListParseResult<FnisFlagData>.Success(
                new FnisFlagData(
                    position,
                    FnisAnimFlags.None,
                    null,
                    null,
                    new(),
                    new()),
                position);
        }
        if (position < end)
        {
            position++; // skip `-`
        }

        return Parse(source, TextSpan.FromRange(position, end));
    }

    /// <summary>
    /// Parses FNIS animation flags from an input span.
    /// </summary>
    public static FnisListParseResult<FnisFlagData> Parse(ReadOnlySpan<char> source, TextSpan input)
    {
        int position = input.Pos;
        int end = input.End;

        if ((uint)position > (uint)source.Length || (uint)end > (uint)source.Length)
        {
            return FnisListParseResult<FnisFlagData>.Failure(FnisListParseErrorKind.InvalidSource, position);
        }

        FnisAnimFlags flags = FnisAnimFlags.None;
        float? blendTime = null;
        float? duration = null;

        List<FnisTriggerData> triggers = new();
        List<FnisTriggerData> triggers2 = new();

        int nextPos = position;

        while (TryReadFlag(source, ref nextPos, end, out TextSpan flagSpan))
        {
            ReadOnlySpan<char> flag = flagSpan.Slice(source);

            ParseFlag(flag, ref flags);

            if (TryParseTimeFlag(source, flagSpan, 'B', out float parsedBlendTime))
            {
                blendTime = parsedBlendTime;
            }

            if (TryParseTimeFlag(source, flagSpan, 'D', out float parsedDuration))
            {
                duration = parsedDuration;
            }

            if (TryParseTriggerData(source, flagSpan, false, out FnisTriggerData trigger))
            {
                triggers.Add(trigger);
            }
            else if (TryParseTriggerData(source, flagSpan, true, out FnisTriggerData trigger2))
            {
                triggers2.Add(trigger2);
            }
        }

        return FnisListParseResult<FnisFlagData>.Success(
            new FnisFlagData(
                nextPos,
                flags,
                blendTime,
                duration,
                triggers,
                triggers2),
            nextPos);
    }

    private static void ParseFlag(ReadOnlySpan<char> flag, ref FnisAnimFlags flags)
    {
        if (flag.SequenceEqual("a"))
        {
            flags |= FnisAnimFlags.Acyclic;
        }
        else if (flag.SequenceEqual("o"))
        {
            flags |= FnisAnimFlags.AnimObjects;
        }
        else if (flag.SequenceEqual("ac"))
        {
            flags |= FnisAnimFlags.AnimatedCamera;
        }
        else if (flag.SequenceEqual("ac1"))
        {
            flags |= FnisAnimFlags.AnimatedCameraSet;
        }
        else if (flag.SequenceEqual("ac0"))
        {
            flags |= FnisAnimFlags.AnimatedCameraReset;
        }
        else if (flag.SequenceEqual("bsa"))
        {
            flags |= FnisAnimFlags.BSA;
        }
        else if (flag.SequenceEqual("h"))
        {
            flags |= FnisAnimFlags.HeadTracking;
        }
        else if (flag.SequenceEqual("k"))
        {
            flags |= FnisAnimFlags.Known;
        }
        else if (flag.SequenceEqual("md"))
        {
            flags |= FnisAnimFlags.MotionDriven;
        }
        else if (flag.SequenceEqual("st"))
        {
            flags |= FnisAnimFlags.Sticky;
        }
        else if (flag.SequenceEqual("Tn"))
        {
            flags |= FnisAnimFlags.TransitionNext;
        }
    }

    private static bool TryParseTimeFlag(ReadOnlySpan<char> source, TextSpan flagSpan, char prefix, out float value)
    {
        value = 0;

        if (flagSpan.Len <= 1)
        {
            return false;
        }

        char actualPrefix = source[flagSpan.Pos];

        if (actualPrefix != prefix && actualPrefix != char.ToLowerInvariant(prefix))
        {
            return false;
        }

        return float.TryParse(source.Slice(flagSpan.Pos + 1, flagSpan.Len - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseTriggerData(ReadOnlySpan<char> source, TextSpan flagSpan, bool trigger2, out FnisTriggerData trigger)
    {
        trigger = default;

        ReadOnlySpan<char> flag = flagSpan.Slice(source);

        if (flag.Length < 4 || flag[0] != 'T')
        {
            return false;
        }

        bool isTrigger2 = flag.Length >= 3 && flag[1] == '2' && flag[2] == '_';
        if (isTrigger2 != trigger2)
        {
            return false;
        }

        int eventStart = 1;
        int slash = flag.IndexOf('/');

        if (slash <= eventStart || slash >= flag.Length - 1)
        {
            return false;
        }

        if (!float.TryParse(
            flag[(slash + 1)..],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float time))
        {
            return false;
        }

        trigger = new FnisTriggerData(new TextSpan(flagSpan.Pos + eventStart, slash - eventStart), time);

        return true;
    }

    private static bool TryReadFlag(ReadOnlySpan<char> source, ref int position, int end, out TextSpan span)
    {
        span = default;

        if (position >= end || char.IsWhiteSpace(source[position]))
        {
            return false;
        }
        while (position < end && source[position] == ',')
        {
            position++;

            if (position >= end || char.IsWhiteSpace(source[position]))
            {
                return false;
            }
        }

        int start = position;

        while (position < end && source[position] != ',' && !char.IsWhiteSpace(source[position]))
        {
            position++;
        }

        span = new TextSpan(start, position - start);

        return true;
    }
}
