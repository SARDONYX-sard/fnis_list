using System;
using System.Collections.Generic;

namespace FnisList;

/// <summary>
/// Parsed FNIS animation object data.
/// </summary>
public readonly struct FnisAnimObjectSpan {
    /// <summary>
    /// Gets the object name span.
    /// </summary>
    public TextSpan Name { get; }

    /// <summary>
    /// Gets the actor role.
    /// </summary>
    /// <remarks>
    /// NOTE: use only PairedAndKillMove
    /// </remarks>
    public FnisActorRole Role { get; }

    /// <summary>
    /// Initializes parsed animation object data.
    /// </summary>
    public FnisAnimObjectSpan(TextSpan name, FnisActorRole role) {
        this.Name = name;
        this.Role = role;
    }
}

/// <summary>
/// Parses FNIS animation objects.
/// </summary>
public static class FnisAnimObjectParser {
    /// <summary>
    /// Parses animation objects from the remaining part of an animation line.
    /// </summary>
    ///
    /// <returns>
    /// A parse result containing the parsed animation objects.
    /// </returns>
    ///
    /// <remarks>
    /// Normal animation objects are whitespace-separated names.
    /// Paired and kill-move animation objects use <c>Name/1</c> or <c>Name/2</c>.
    /// </remarks>
    public static FnisListParseResult<List<FnisAnimObjectSpan>> Parse(ReadOnlySpan<char> source, TextSpan input, bool isPairAndKill) {
        int position = input.Pos;
        int end = input.End;

        if ((uint)position > (uint)source.Length || (uint)end > (uint)source.Length || position > end) {
            return FnisListParseResult<List<FnisAnimObjectSpan>>.Failure(FnisListParseErrorKind.InvalidSourceRange, position);
        }

        List<FnisAnimObjectSpan> objects = new();

        while (true) {
            while (position < end && char.IsWhiteSpace(source[position])) {
                position++;
            }

            if (position >= end) {
                break;
            }
            TextSpan tokenSpan = TakeUntilSpace(source, ref position, end);

            FnisListParseResult<FnisAnimObjectSpan> result = TryParse(source, tokenSpan, isPairAndKill);
            if (result.IsFailure) {
                return FnisListParseResult<List<FnisAnimObjectSpan>>.Failure(result.Error, result.Pos);
            }

            objects.Add(result.Value);
        }

        return FnisListParseResult<List<FnisAnimObjectSpan>>.Success(objects, position);
    }

    private static FnisListParseResult<FnisAnimObjectSpan> TryParse(ReadOnlySpan<char> source, TextSpan tokenSpan, bool pairAndKill) {
        return pairAndKill ? ParsePairAndKill(source, tokenSpan) : ParseNormal(source, tokenSpan);
    }

    private static FnisListParseResult<FnisAnimObjectSpan> ParseNormal(ReadOnlySpan<char> source, TextSpan tokenSpan) {
        if (tokenSpan.Len == 0) {
            return FnisListParseResult<FnisAnimObjectSpan>.Failure(FnisListParseErrorKind.InvalidAnimationObject, tokenSpan.Pos);
        }

        ReadOnlySpan<char> token = tokenSpan.Slice(source);

        if (token.IndexOf('/') >= 0) {
            return FnisListParseResult<FnisAnimObjectSpan>.Failure(FnisListParseErrorKind.NumberedAnimationObjectRequiresPairAndKill, tokenSpan.Pos);
        }

        FnisAnimObjectSpan value = new(tokenSpan, FnisActorRole.Active);

        return FnisListParseResult<FnisAnimObjectSpan>.Success(value, tokenSpan.End);
    }

    private static FnisListParseResult<FnisAnimObjectSpan> ParsePairAndKill(ReadOnlySpan<char> source, TextSpan tokenSpan) {
        ReadOnlySpan<char> token = tokenSpan.Slice(source);

        int slash = token.IndexOf('/');
        if (slash < 0) {
            return FnisListParseResult<FnisAnimObjectSpan>.Failure(FnisListParseErrorKind.PairAndKillRoleRequiresSlash, tokenSpan.End);
        }

        if (slash <= 0 || slash != token.LastIndexOf('/')) {
            return FnisListParseResult<FnisAnimObjectSpan>.Failure(FnisListParseErrorKind.InvalidAnimationObject, tokenSpan.Pos);
        }

        ReadOnlySpan<char> name = token[..slash];
        ReadOnlySpan<char> role = token[(slash + 1)..];

        FnisActorRole actorRole;

        if (role.SequenceEqual("1")) {
            actorRole = FnisActorRole.Active;
        }
        else if (role.SequenceEqual("2")) {
            actorRole = FnisActorRole.Passive;
        }
        else {
            return FnisListParseResult<FnisAnimObjectSpan>.Failure(FnisListParseErrorKind.InvalidPairedAndKillRoleNumber, tokenSpan.Pos);
        }

        FnisAnimObjectSpan value = new(new TextSpan(tokenSpan.Pos, name.Length), actorRole);

        return FnisListParseResult<FnisAnimObjectSpan>.Success(value, tokenSpan.End);
    }

    private static TextSpan TakeUntilSpace(ReadOnlySpan<char> source, ref int position, int end) {
        int start = position;

        while (position < end && !char.IsWhiteSpace(source[position])) {
            position++;
        }

        return new TextSpan(start, position - start);
    }
}
