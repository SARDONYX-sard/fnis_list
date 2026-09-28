using System;

namespace fnis_list;

public readonly struct FnisListParseResult<T>
{
    private readonly bool _success;
    private readonly FnisListParseErrorKind _error;
    private readonly bool _endOfInput;

    private readonly T _value;
    private readonly int _pos;

    public bool IsSuccess => this._success;
    public bool IsFailure => !this._success;
    /// <summary>
    /// The end of the input was reached or the input is empty.
    ///
    /// Whether this is considered an error depends on the caller.
    /// This value is treated as a non-failure by <see cref="FnisListParseResult{T}.IsFailure"/>.
    /// </summary>
    public bool IsEndOfInput => this._endOfInput;

    public int Pos => this._pos;

    public T Value
    {
        get
        {
            if (!this._success)
            {
                throw new InvalidOperationException("The parse result does not contain a value.");
            }

            return this._value;
        }
    }

    public FnisListParseErrorKind Error
    {
        get
        {
            if (!this.IsFailure)
            {
                throw new InvalidOperationException("The parse result does not contain an error.");
            }

            return this._error;
        }
    }

    private FnisListParseResult(T value, int pos)
    {
        this._success = true;
        this._value = value;
        this._error = default;
        this._pos = pos;
    }

    private FnisListParseResult(FnisListParseErrorKind error, int pos)
    {
        this._success = false;
        this._value = default!;
        this._error = error;
        this._pos = pos;
    }

    /// <summary>
    /// For EndOfInput
    /// </summary>
    private FnisListParseResult(int pos)
    {
        this._success = false;
        this._endOfInput = true;
        this._value = default!;
        this._error = default;
        this._pos = pos;
    }

    public static FnisListParseResult<T> Success(T value, int pos) => new(value, pos);

    public static FnisListParseResult<T> Failure(FnisListParseErrorKind error, int pos) => new(error, pos);

    // internal static FnisListParseResult<T> EndOfInput(int pos) => new FnisListParseResult<T>(endOfInput: true, pos);
    public static FnisListParseResult<T> EndOfInput(int pos)
    {
        return new FnisListParseResult<T>(pos);
    }

    /// <summary>
    /// Represents the result of parsing a FNIS list.
    ///
    /// <code>
    /// Example error:
    ///   ┌─ path:2:1
    ///   │
    /// 2 │ + Second second.hkx
    ///   │ ^ invalid sequence
    /// </code>
    /// </summary>
    public string ReadableError(ReadOnlySpan<char> source, string path = "")
    {
        if (!this.IsFailure)
        {
            throw new InvalidOperationException("The parse result does not contain an error.");
        }

        int pos = Math.Clamp(this._pos, 0, source.Length);

        int lineStart = pos;
        while (lineStart > 0 && source[lineStart - 1] != '\r' && source[lineStart - 1] != '\n')
        {
            lineStart--;
        }

        int lineEnd = pos;
        while (lineEnd < source.Length && source[lineEnd] != '\r' && source[lineEnd] != '\n')
        {
            lineEnd++;
        }

        ReadOnlySpan<char> line = source.Slice(lineStart, lineEnd - lineStart);
        int column = pos - lineStart;

        int lineNumber = 1;
        for (int i = 0; i < lineStart; i++)
        {
            if (source[i] == '\n') { lineNumber++; }
        }

        return
            $"  ┌─ {path}:{lineNumber}:{column + 1}\n" +
            $"  │\n" +
            $"{lineNumber,2} │ {line}\n" +
            $"  │ {new string(' ', column)}^ {ErrorMessage(this._error)}";
    }


    /// <summary>
    /// Return EnumErrorKind to readable message.
    /// </summary>
    public static string ErrorMessage(FnisListParseErrorKind error)
    {
        return error switch
        {
            FnisListParseErrorKind.InvalidSource
                => "invalid source",

            FnisListParseErrorKind.UnexpectedEnd
                => "unexpected end of input",

            FnisListParseErrorKind.InvalidSyntax
                => "invalid syntax",

            FnisListParseErrorKind.MissingDuration
                => "missing duration",

            FnisListParseErrorKind.InvalidSequence
                => "invalid sequence",

            FnisListParseErrorKind.InvalidNumber
                => "invalid number",

            FnisListParseErrorKind.UnexpectedAnimationObject
                => "Animation object data (remaining input) was provided without the -o flag.",

            FnisListParseErrorKind.InvalidAnimationObject
                => "invalid animation object",

            FnisListParseErrorKind.NumberedAnimationObjectRequiresPairAndKill
                => "numbered animation objects (`Name/1` or `Name/2`) are only valid for PairedAndKill animations",

            FnisListParseErrorKind.PairAndKillRoleRequiresSlash
                => "PairedAndKill animation objects require '/' followed by a role number",

            FnisListParseErrorKind.InvalidPairedAndKillRoleNumber
                => "invalid PairedAndKill role number; only 1 and 2 are valid",

            FnisListParseErrorKind.ChairRequiresThreeContinuations
                => "Chair requires at least 3 continuation animations",

            FnisListParseErrorKind.ChairAllowsOnlyNoFlagsOrAnimationObjects
                => "Chair allows no flags or the -o flag only",

            FnisListParseErrorKind.FurnitureRequiresAcyclic
                => "Furniture requires the -a flag",

            FnisListParseErrorKind.FurnitureRequiresThreeAnimations
                => "Furniture requires at least 3 animations",

            FnisListParseErrorKind.FurnitureRequiresAcyclicLastAnimation
                => "the last Furniture animation requires the -a flag",

            FnisListParseErrorKind.FurnitureSecondToLastMustBeCyclic
                => "the second-to-last Furniture animation must be cyclic",

            _ => "unknown parse error",
        };
    }
}

/// <summary>
/// Describes the result of parsing.
/// </summary>
public enum FnisListParseErrorKind : byte
{
    /// <summary>
    /// The input source or span is invalid.
    /// </summary>
    InvalidSource,

    /// <summary>
    /// The input ended before the required syntax was complete.
    /// </summary>
    UnexpectedEnd,

    /// <summary>
    /// The input does not match the expected syntax.
    /// </summary>
    InvalidSyntax,

    /// <summary>
    /// A required duration is missing.
    /// </summary>
    MissingDuration,

    /// <summary>
    /// A sequence continuation is invalid.
    /// </summary>
    InvalidSequence,

    /// <summary>
    /// A numeric value is invalid.
    /// </summary>
    InvalidNumber,

    /// <summary>
    /// Animation object data was provided without the <c>-o</c> flag.
    /// </summary>
    UnexpectedAnimationObject,

    /// <summary>
    /// Invalid AnimObject Syntax.
    /// </summary>
    InvalidAnimationObject,

    /// <summary>
    /// A numbered animation object requires a PairedAndKill animation.
    /// </summary>
    NumberedAnimationObjectRequiresPairAndKill,

    /// <summary>
    /// A PairedAndKill animation object requires a '/' before its role number.
    /// </summary>
    PairAndKillRoleRequiresSlash,

    /// <summary>
    /// The role number of a PairedAndKill animation object is invalid.
    /// Only 1 and 2 are valid.
    /// </summary>
    InvalidPairedAndKillRoleNumber,

    /// <summary>
    /// A Chair requires at least three continuation animations.
    /// </summary>
    ChairRequiresThreeContinuations,

    /// <summary>
    /// A Chair allows no flags or the <c>-o</c> flag only.
    /// </summary>
    ChairAllowsOnlyNoFlagsOrAnimationObjects,

    /// <summary>
    /// A Furniture animation requires the <c>-a</c> flag.
    /// </summary>
    FurnitureRequiresAcyclic,

    /// <summary>
    /// A Furniture sequence requires at least three animations.
    /// </summary>
    FurnitureRequiresThreeAnimations,

    /// <summary>
    /// The last animation in a Furniture sequence requires the <c>-a</c> flag.
    /// </summary>
    FurnitureRequiresAcyclicLastAnimation,

    /// <summary>
    /// The second-to-last Furniture animation must be cyclic.
    /// </summary>
    FurnitureSecondToLastMustBeCyclic,
}
