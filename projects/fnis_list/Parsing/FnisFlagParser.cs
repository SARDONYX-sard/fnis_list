using System;
using System.Collections.Generic;
using System.Globalization;

namespace fnis_list;

/// <summary>
/// Parsed FNIS animation flag data.
/// </summary>
public readonly struct FnisFlagSpan {
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
    public List<FnisTriggerSpan> Triggers { get; }

    /// <summary>
    /// Gets the parsed secondary triggers.
    /// </summary>
    public List<FnisTriggerSpan> Triggers2 { get; }

    /// <summary>
    /// Initializes parsed FNIS flag data.
    /// </summary>
    public FnisFlagSpan(
        int nextPos,
        FnisAnimFlags flags,
        float? blendTime,
        float? duration,
        List<FnisTriggerSpan> triggers,
        List<FnisTriggerSpan> triggers2
    ) {
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
public static class FnisFlagParser {
    /// <summary>
    /// Parses optional FNIS animation flags from a line.
    /// If the line does not start with <c>-</c>, no flags are present and a successful result
    /// containing <see cref="FnisAnimFlags.None"/> is returned.
    /// For example, <c>-a,bsa</c> contains flags, while <c>Attack attack.hkx</c> does not.
    /// </summary>
    public static FnisListParseResult<FnisFlagSpan> Parse(ReadOnlySpan<char> source, TextSpan input) {
        int position = input.Pos;
        int end = input.End;

        if ((uint)position > (uint)source.Length || (uint)end > (uint)source.Length) {
            return FnisListParseResult<FnisFlagSpan>.Failure(FnisListParseErrorKind.InvalidSourceRange, position);
        }

        while (position < end && char.IsWhiteSpace(source[position])) {
            position++;
        }

        if (position >= end || source[position] != '-') {
            return FnisListParseResult<FnisFlagSpan>.Success(new FnisFlagSpan(position, FnisAnimFlags.None, null, null, new(), new()), position);
        }
        if (position < end) {
            position++; // skip `-`
        }

        return ParseRaw(source, TextSpan.FromRange(position, end));
    }

    /// <summary>
    /// Parses FNIS animation flags from an input span without the leading <c>-</c>.
    /// For example, <c>a,bsa</c>.
    /// </summary>
    public static FnisListParseResult<FnisFlagSpan> ParseRaw(ReadOnlySpan<char> source, TextSpan input) {
        int position = input.Pos;
        int end = input.End;

        if ((uint)position > (uint)source.Length || (uint)end > (uint)source.Length) {
            return FnisListParseResult<FnisFlagSpan>.Failure(FnisListParseErrorKind.InvalidSourceRange, position);
        }

        FnisAnimFlags flags = FnisAnimFlags.None;
        float? blendTime = null;
        float? duration = null;

        List<FnisTriggerSpan> triggers = new();
        List<FnisTriggerSpan> triggers2 = new();

        int nextPos = position;
        bool hasNext;

        do {
            if (!TryReadFlag(source, ref nextPos, end, out TextSpan flagSpan, out hasNext)) {
                break;
            }

            ReadOnlySpan<char> flag = flagSpan.Slice(source);

            ParseSimpleFlag(flag, ref flags);

            if (TryParseTimeFlag(source, flagSpan, 'B', out float parsedBlendTime)) {
                blendTime = parsedBlendTime;
            }

            if (TryParseTimeFlag(source, flagSpan, 'D', out float parsedDuration)) {
                duration = parsedDuration;
            }

            if (TryParseTriggerData(source, flagSpan, false, out FnisTriggerSpan trigger)) {
                triggers.Add(trigger);
            }
            else if (TryParseTriggerData(source, flagSpan, true, out FnisTriggerSpan trigger2)) {
                triggers2.Add(trigger2);
            }
        } while (hasNext);

        return FnisListParseResult<FnisFlagSpan>.Success(new FnisFlagSpan(nextPos, flags, blendTime, duration, triggers, triggers2), nextPos);
    }

    private static void ParseSimpleFlag(ReadOnlySpan<char> flag, ref FnisAnimFlags flags) {
        if (flag.SequenceEqual("a")) {
            flags |= FnisAnimFlags.Acyclic;
        }
        else if (flag.SequenceEqual("o")) {
            flags |= FnisAnimFlags.AnimObjects;
        }
        else if (flag.SequenceEqual("ac")) {
            flags |= FnisAnimFlags.AnimatedCamera;
        }
        else if (flag.SequenceEqual("ac1")) {
            flags |= FnisAnimFlags.AnimatedCameraSet;
        }
        else if (flag.SequenceEqual("ac0")) {
            flags |= FnisAnimFlags.AnimatedCameraReset;
        }
        else if (flag.SequenceEqual("bsa")) {
            flags |= FnisAnimFlags.BSA;
        }
        else if (flag.SequenceEqual("h")) {
            flags |= FnisAnimFlags.HeadTracking;
        }
        else if (flag.SequenceEqual("k")) {
            flags |= FnisAnimFlags.Known;
        }
        else if (flag.SequenceEqual("md")) {
            flags |= FnisAnimFlags.MotionDriven;
        }
        else if (flag.SequenceEqual("st")) {
            flags |= FnisAnimFlags.Sticky;
        }
        else if (flag.SequenceEqual("Tn")) {
            flags |= FnisAnimFlags.TransitionNext;
        }
    }

    private static bool TryParseTimeFlag(ReadOnlySpan<char> source, TextSpan flagSpan, char prefix, out float value) {
        value = 0;

        if (flagSpan.Len <= 1) {
            return false;
        }

        char actualPrefix = source[flagSpan.Pos];

        if (actualPrefix != prefix && actualPrefix != char.ToLowerInvariant(prefix)) {
            return false;
        }

        return float.TryParse(source.Slice(flagSpan.Pos + 1, flagSpan.Len - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseTriggerData(ReadOnlySpan<char> source, TextSpan flagSpan, bool trigger2, out FnisTriggerSpan trigger) {
        trigger = default;

        ReadOnlySpan<char> flag = flagSpan.Slice(source);

        if (flag.Length < 4 || flag[0] != 'T') {
            return false;
        }

        bool isTrigger2 = flag.Length >= 3 && flag[1] == '2' && flag[2] == '_';
        if (isTrigger2 != trigger2) {
            return false;
        }

        int eventStart = 1;
        int slash = flag.IndexOf('/');

        if (slash <= eventStart || slash >= flag.Length - 1) {
            return false;
        }

        if (!float.TryParse(flag[(slash + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out float time)) {
            return false;
        }

        trigger = new FnisTriggerSpan(new TextSpan(flagSpan.Pos + eventStart, slash - eventStart), time);

        return true;
    }

    private static bool TryReadFlag(ReadOnlySpan<char> source, ref int position, int end, out TextSpan span, out bool hasNext) {
        span = default;
        hasNext = false;

        // space0
        int start = position;
        while (start < end && char.IsWhiteSpace(source[start])) {
            start++;
        }

        if (start >= end || source[start] == ',') {
            return false;
        }

        // take_while(1.., |c| c != ',' && !c.is_whitespace())
        int current = start;
        while (current < end && source[current] != ',' && !char.IsWhiteSpace(source[current])) {
            current++;
        }

        span = new TextSpan(start, current - start);

        // opt(','), with the trailing space left unconsumed when `,` is absent.
        int afterFlag = current;
        while (afterFlag < end && char.IsWhiteSpace(source[afterFlag])) {
            afterFlag++;
        }

        if (afterFlag < end && source[afterFlag] == ',') {
            position = afterFlag + 1; // NOTE: A comma means another flag follows, so advance past it.
            hasNext = true;
        }
        else {
            position = current; // To keep position at the whitespace immediately after the flag.
        }

        return true;
    }
}
