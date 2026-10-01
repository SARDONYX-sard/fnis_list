using System;

namespace FnisList;

public readonly struct FnisListParseResult<T> {
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

    public T Value {
        get {
            if (!this._success) {
                throw new InvalidOperationException("The parse result does not contain a value.");
            }

            return this._value;
        }
    }

    public FnisListParseErrorKind Error {
        get {
            if (!this.IsFailure) {
                throw new InvalidOperationException("The parse result does not contain an error.");
            }

            return this._error;
        }
    }

    private FnisListParseResult(T value, int pos) {
        this._success = true;
        this._value = value;
        this._error = default;
        this._pos = pos;
    }

    private FnisListParseResult(FnisListParseErrorKind error, int pos) {
        this._success = false;
        this._value = default!;
        this._error = error;
        this._pos = pos;
    }

    /// <summary>
    /// For EndOfInput
    /// </summary>
    private FnisListParseResult(int pos) {
        this._success = false;
        this._endOfInput = true;
        this._value = default!;
        this._error = default;
        this._pos = pos;
    }

    public static FnisListParseResult<T> Success(T value, int pos) => new(value, pos);

    public static FnisListParseResult<T> Failure(FnisListParseErrorKind error, int pos) => new(error, pos);

    // internal static FnisListParseResult<T> EndOfInput(int pos) => new FnisListParseResult<T>(endOfInput: true, pos);
    public static FnisListParseResult<T> EndOfInput(int pos) {
        return new FnisListParseResult<T>(pos);
    }

    /// <summary>
    /// Represents the result of parsing a FNIS list.
    ///
    /// <code>
    /// Example error:
    ///
    /// path:2:1
    ///    │
    ///  2 │ + Second second.hkx
    ///    │ ^ SequencedContinued ('+') must follow s/so/fu/fuo/ch.
    /// </code>
    /// </summary>
    ///
    /// <exception cref="InvalidOperationException">
    /// If not IsFailure
    /// </exception>
    public string ReadableError(ReadOnlySpan<char> source, string path = "") {
        if (!this.IsFailure) {
            throw new InvalidOperationException("The parse result does not contain an error.");
        }

        int pos = Math.Clamp(this._pos, 0, source.Length);

        int lineStart = pos;
        while (lineStart > 0 && source[lineStart - 1] != '\r' && source[lineStart - 1] != '\n') {
            lineStart--;
        }

        int lineEnd = pos;
        while (lineEnd < source.Length && source[lineEnd] != '\r' && source[lineEnd] != '\n') {
            lineEnd++;
        }

        ReadOnlySpan<char> line = source.Slice(lineStart, lineEnd - lineStart);
        int column = pos - lineStart;

        int lineNumber = 1;
        for (int i = 0; i < lineStart; i++) {
            if (source[i] == '\n') { lineNumber++; }
        }

        return
            $"{path}:{lineNumber}:{column + 1}\n" +
            $"   │\n" +
            $"{lineNumber,2} │ {line}\n" +
            $"   │ {new string(' ', column)}^ {ErrorMessage(this._error)}";
    }

    /// <summary>
    /// Return EnumErrorKind to readable message.
    /// </summary>
    public static string ErrorMessage(FnisListParseErrorKind error) {
        return error switch {
            // ---------------------------------------------------------------------------------------------------------
            // Common
            // ---------------------------------------------------------------------------------------------------------

            FnisListParseErrorKind.InvalidSourceRange
                => "invalid source range; position and end must be within the source bounds",

            FnisListParseErrorKind.UnexpectedEnd
                => "unexpected end of input",

            // ---------------------------------------------------------------------------------------------------------
            // FNIS Version
            // ---------------------------------------------------------------------------------------------------------

            FnisListParseErrorKind.InvalidVersion
                => "invalid version; expected: Version V<major>.<minor>",

            // ---------------------------------------------------------------------------------------------------------
            // FNIS Animation
            // ---------------------------------------------------------------------------------------------------------

            FnisListParseErrorKind.InvalidAnimationType
                => "invalid animation type; expected one of: AnimVar, AAPrefix, fuo, ofa, ch, fu, km, pa, so, +, b, o, s",

            FnisListParseErrorKind.InvalidSequence
                => "SequencedContinued ('+') must follow s/so/fu/fuo/ch",

            FnisListParseErrorKind.UnexpectedAnimationObject
                => "Animation object data (remaining input) was provided without the -o flag",

            FnisListParseErrorKind.InvalidAnimationObject
                => "invalid animation object",

            // ---------------------------------------------------------------------------------------------------------
            // FNIS Motion / Rotation
            // ---------------------------------------------------------------------------------------------------------

            FnisListParseErrorKind.MissingRelatedAnimation
                => "MD/RD definitions require a related animation definition such as b, s, so, fu, fuo, or ch",

            FnisListParseErrorKind.InvalidMotion
                => "invalid MD definition; expected: MD <time> <deltaX> <deltaY> <deltaZ>",

            FnisListParseErrorKind.InvalidRotation
                => "invalid RD definition; expected: RD <time> <x> <y> <z> <w> or RD <time> <angle>",

            // ---------------------------------------------------------------------------------------------------------
            // FNIS AnimVar
            // ---------------------------------------------------------------------------------------------------------

            FnisListParseErrorKind.InvalidAnimVarDefinition
                => "invalid AnimVar definition; expected: AnimVar <Name> <BOOL|INT32|REAL> <value>",

            FnisListParseErrorKind.InvalidAnimVarType
                => "invalid AnimVar type; expected one of: BOOL, INT32, REAL",

            FnisListParseErrorKind.InvalidAnimVarValue
                => "invalid AnimVar value; expected a value valid for the declared type",

            // ---------------------------------------------------------------------------------------------------------
            // FNIS AA
            // ---------------------------------------------------------------------------------------------------------

            FnisListParseErrorKind.InvalidAAPrefix
                => "invalid AAprefix definition; expected: AAprefix <prefix>",

            FnisListParseErrorKind.InvalidAASet
                => "invalid AAset definition; expected: AAset <group> <slots>",

            FnisListParseErrorKind.InvalidAATrigger
                => "invalid alternate-animation trigger; expected: T <animation> <trigger>/<time> [...]",

            // ---------------------------------------------------------------------------------------------------------
            // FNIS Paired / KillMove
            // ---------------------------------------------------------------------------------------------------------

            FnisListParseErrorKind.NumberedAnimationObjectRequiresPairAndKill
                => "numbered animation objects (`Name/1` or `Name/2`) are only valid for PairedAndKill animations",

            FnisListParseErrorKind.PairAndKillRoleRequiresSlash
                => "PairedAndKill animation objects require '/' followed by a role number",

            FnisListParseErrorKind.InvalidPairedAndKillRoleNumber
                => "invalid PairedAndKill role number; only 1 and 2 are valid",

            // ---------------------------------------------------------------------------------------------------------
            // FNIS Chair
            // ---------------------------------------------------------------------------------------------------------

            FnisListParseErrorKind.ChairRequiresThreeContinuations
                => "Chair requires at least 3 continuation animations",

            FnisListParseErrorKind.ChairAllowsOnlyNoFlagsOrAnimationObjects
                => "Chair allows no flags or the -o flag only",

            // ---------------------------------------------------------------------------------------------------------
            // FNIS Furniture
            // ---------------------------------------------------------------------------------------------------------

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
public enum FnisListParseErrorKind : byte {
    // -----------------------------------------------------------------------------------------------------------------
    // Common
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The input source span is out of range.
    /// </summary>
    InvalidSourceRange,

    /// <summary>
    /// The input ended before the required syntax was complete.
    /// </summary>
    UnexpectedEnd,

    // -----------------------------------------------------------------------------------------------------------------
    // FNIS Version
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The version format is invalid.
    /// </summary>
    InvalidVersion,

    // -----------------------------------------------------------------------------------------------------------------
    // FNIS Animation
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// The animation type is invalid.
    /// </summary>
    InvalidAnimationType,

    /// <summary>
    /// SequencedContinued ('+') must follow s/so/fu/fuo/ch.
    /// </summary>
    InvalidSequence,

    /// <summary>
    /// Animation object data was provided without the <c>-o</c> flag.
    /// </summary>
    UnexpectedAnimationObject,

    /// <summary>
    /// The animation object syntax is invalid.
    /// </summary>
    InvalidAnimationObject,

    // -----------------------------------------------------------------------------------------------------------------
    // FNIS Motion / Rotation
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// An MD or RD definition has no related animation definition.
    /// </summary>
    MissingRelatedAnimation,

    /// <summary>
    /// An MD motion definition has an invalid format.
    /// </summary>
    InvalidMotion,

    /// <summary>
    /// An RD rotation definition has an invalid format.
    /// </summary>
    InvalidRotation,

    // -----------------------------------------------------------------------------------------------------------------
    // FNIS AnimVar
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// An AnimVar definition has an invalid syntax.
    /// </summary>
    InvalidAnimVarDefinition,

    /// <summary>
    /// An AnimVar type is invalid.
    /// </summary>
    InvalidAnimVarType,

    /// <summary>
    /// An AnimVar value is invalid for its declared type.
    /// </summary>
    InvalidAnimVarValue,

    // -----------------------------------------------------------------------------------------------------------------
    // FNIS AA
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// An alternate-animation prefix definition is invalid.
    /// </summary>
    InvalidAAPrefix,

    /// <summary>
    /// An alternate-animation set definition is invalid.
    /// </summary>
    InvalidAASet,

    /// <summary>
    /// An alternate-animation trigger definition is invalid.
    /// </summary>
    InvalidAATrigger,

    // -----------------------------------------------------------------------------------------------------------------
    // FNIS Paired / KillMove
    // -----------------------------------------------------------------------------------------------------------------

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

    // -----------------------------------------------------------------------------------------------------------------
    // FNIS Chair
    // -----------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// A Chair requires at least three continuation animations.
    /// </summary>
    ChairRequiresThreeContinuations,

    /// <summary>
    /// A Chair allows no flags or the <c>-o</c> flag only.
    /// </summary>
    ChairAllowsOnlyNoFlagsOrAnimationObjects,

    // -----------------------------------------------------------------------------------------------------------------
    // FNIS Furniture
    // -----------------------------------------------------------------------------------------------------------------

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
