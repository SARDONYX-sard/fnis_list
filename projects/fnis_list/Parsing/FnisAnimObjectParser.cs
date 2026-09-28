using System;
using System.Collections.Generic;

namespace fnis_list;

/// <summary>
/// Parsed FNIS animation object data.
/// </summary>
public readonly struct FnisAnimObjectData
{
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
    public FnisAnimObjectData(TextSpan name, FnisActorRole role)
    {
        this.Name = name;
        this.Role = role;
    }
}

/// <summary>
/// Parses FNIS animation objects.
/// </summary>
public static class FnisAnimObjectParser
{
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
    public static FnisListParseResult<List<FnisAnimObjectData>> Parse(ReadOnlySpan<char> source, TextSpan input, bool isPairAndKill)
    {
        int position = input.Pos;
        int end = input.End;

        if ((uint)position > (uint)source.Length || (uint)end > (uint)source.Length || position > end)
        {
            return FnisListParseResult<List<FnisAnimObjectData>>.Failure(FnisListParseErrorKind.InvalidSource, position);
        }

        List<FnisAnimObjectData> objects = new();

        while (true)
        {
            while (position < end && char.IsWhiteSpace(source[position]))
            {
                position++;
            }

            if (position >= end)
            {
                break;
            }
            TextSpan tokenSpan = TakeUntilSpace(source, ref position, end);

            FnisListParseResult<FnisAnimObjectData> result = TryParse(source, tokenSpan, isPairAndKill);
            if (result.IsFailure)
            {
                return FnisListParseResult<List<FnisAnimObjectData>>.Failure(result.Error, result.Pos);
            }

            objects.Add(result.Value);
        }

        return FnisListParseResult<List<FnisAnimObjectData>>.Success(objects, position);
    }

    private static FnisListParseResult<FnisAnimObjectData> TryParse(ReadOnlySpan<char> source, TextSpan tokenSpan, bool pairAndKill)
    {
        return pairAndKill ? ParsePairAndKill(source, tokenSpan) : ParseNormal(source, tokenSpan);
    }

    private static FnisListParseResult<FnisAnimObjectData> ParseNormal(ReadOnlySpan<char> source, TextSpan tokenSpan)
    {
        if (tokenSpan.Len == 0)
        {
            return FnisListParseResult<FnisAnimObjectData>.Failure(FnisListParseErrorKind.InvalidAnimationObject, tokenSpan.Pos);
        }

        ReadOnlySpan<char> token = tokenSpan.Slice(source);

        if (token.IndexOf('/') >= 0)
        {
            return FnisListParseResult<FnisAnimObjectData>.Failure(FnisListParseErrorKind.NumberedAnimationObjectRequiresPairAndKill, tokenSpan.Pos);
        }

        FnisAnimObjectData value = new(tokenSpan, FnisActorRole.Active);

        return FnisListParseResult<FnisAnimObjectData>.Success(value, tokenSpan.End);
    }

    private static FnisListParseResult<FnisAnimObjectData> ParsePairAndKill(ReadOnlySpan<char> source, TextSpan tokenSpan)
    {
        ReadOnlySpan<char> token = tokenSpan.Slice(source);

        int slash = token.IndexOf('/');
        if (slash < 0)
        {
            return FnisListParseResult<FnisAnimObjectData>.Failure(FnisListParseErrorKind.PairAndKillRoleRequiresSlash, tokenSpan.End);
        }

        if (slash <= 0 || slash != token.LastIndexOf('/'))
        {
            return FnisListParseResult<FnisAnimObjectData>.Failure(FnisListParseErrorKind.InvalidAnimationObject, tokenSpan.Pos);
        }

        ReadOnlySpan<char> name = token[..slash];
        ReadOnlySpan<char> role = token[(slash + 1)..];

        FnisActorRole actorRole;

        if (role.SequenceEqual("1"))
        {
            actorRole = FnisActorRole.Active;
        }
        else if (role.SequenceEqual("2"))
        {
            actorRole = FnisActorRole.Passive;
        }
        else
        {
            return FnisListParseResult<FnisAnimObjectData>.Failure(FnisListParseErrorKind.InvalidPairedAndKillRoleNumber, tokenSpan.Pos);
        }

        FnisAnimObjectData value = new(new TextSpan(tokenSpan.Pos, name.Length), actorRole);

        return FnisListParseResult<FnisAnimObjectData>.Success(value, tokenSpan.End);
    }

    private static TextSpan TakeUntilSpace(ReadOnlySpan<char> source, ref int position, int end)
    {
        int start = position;

        while (position < end && !char.IsWhiteSpace(source[position]))
        {
            position++;
        }

        return new TextSpan(start, position - start);
    }
}
