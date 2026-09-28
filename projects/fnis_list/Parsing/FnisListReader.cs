using System;
using System.Collections.Generic;

namespace fnis_list;

/// <summary>
/// Parses an entire FNIS list file.
/// </summary>
public ref struct FnisListReader
{
    private readonly ReadOnlySpan<char> _source;
    private int _position;
    private int _versionMajor;
    private int _versionMinor;
    private bool _canContinueSequence;

    private bool _chairActive;
    private int _chairContinuationCount;

    private bool _furnitureActive;
    private int _furnitureAnimationCount;
    private bool _furniturePreviousAcyclic;
    private bool _furnitureLastAcyclic;

    public FnisListReader(ReadOnlySpan<char> source)
    {
        this._source = source;
        this._position = 0;
        this._versionMajor = 0;
        this._versionMinor = 0;
        this._canContinueSequence = false;

        this._chairActive = false;
        this._chairContinuationCount = 0;

        this._furnitureActive = false;
        this._furnitureAnimationCount = 0;
        this._furniturePreviousAcyclic = false;
        this._furnitureLastAcyclic = false;
    }

    public int VersionMajor => this._versionMajor;
    public int VersionMinor => this._versionMinor;

    public FnisListParseResult<FnisPattern> Parse()
    {
        List<FnisAnimation> animations = new();
        List<FnisAnimVarData> animVars = new();
        List<FnisAlternateAnimation> alternateAnimations = new();

        while (true)
        {
            FnisLineParser.SkipWhitespaceAndComments(this._source, ref this._position, this._source.Length);

            if (this._position >= this._source.Length)
            {
                FnisListParseErrorKind? error = this.ValidatePendingSequence();
                if (error is not null)
                {
                    return FnisListParseResult<FnisPattern>.Failure(error.Value, this._position);
                }

                break;
            }

            TextSpan line = this.GetCurrentLine();
            ReadOnlySpan<char> lineText = line.Slice(this._source);

            // /////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Format: Version v<D>.<D>
            if (FnisLineParser.IsVersionLine(lineText))
            {
                if (!FnisLineParser.TryParseVersion(lineText, out int major, out int minor))
                {
                    return FnisListParseResult<FnisPattern>.Failure(FnisListParseErrorKind.InvalidSyntax, line.Pos);
                }

                this._versionMajor = major;
                this._versionMinor = minor;
                this._position = FnisLineParser.NextLine(this._source, line.End);

                continue;
            }

            // /////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Format: MD <time:f32> <x> <y> <z>
            if (FnisRotationParser.IsMotionLine(lineText))
            {
                if (animations.Count == 0)
                {
                    return FnisListParseResult<FnisPattern>.Failure(FnisListParseErrorKind.InvalidSyntax, line.Pos);
                }

                FnisListParseResult<FnisMotionData> result = FnisRotationParser.ParseMotion(this._source, line);
                if (result.IsFailure)
                {
                    return FnisListParseResult<FnisPattern>.Failure(result.Error, result.Pos);
                }

                animations[^1].AddMotionData(result.Value);// push to prev animation
                this._position = FnisLineParser.NextLine(this._source, line.End);

                continue;
            }

            // /////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Format: RD <time:f32> <x> <y> <z>
            if (FnisRotationParser.IsRotationLine(lineText))
            {
                if (animations.Count == 0)
                {
                    return FnisListParseResult<FnisPattern>.Failure(FnisListParseErrorKind.InvalidSyntax, line.Pos);
                }

                FnisListParseResult<FnisRotationData> result = FnisRotationParser.ParseRotation(this._source, line);
                if (result.IsFailure)
                {
                    return FnisListParseResult<FnisPattern>.Failure(result.Error, result.Pos);
                }
                animations[^1].AddRotationData(result.Value); // push to prev animation

                this._position = FnisLineParser.NextLine(this._source, line.End);
                continue;
            }

            // /////////////////////////////////////////////////////////////////////////////////////////////////////////

            // Peek FNIS type
            FnisListParseResult<FnisTypeData> typeResult = FnisTypeParser.Parse(this._source, line);
            if (typeResult.IsFailure)
            {
                return FnisListParseResult<FnisPattern>.Failure(typeResult.Error, typeResult.Pos);
            }

            switch (typeResult.Value.Type)
            {
                case FnisAnimType.AnimVar:
                    {
                        FnisListParseErrorKind? error = this.ValidatePendingSequence();
                        if (error is not null)
                        {
                            return FnisListParseResult<FnisPattern>.Failure(error.Value, this._position);
                        }

                        FnisListParseResult<FnisAnimVarData> result = FnisAnimVarParser.Parse(this._source, line);
                        if (result.IsFailure)
                        {
                            return FnisListParseResult<FnisPattern>.Failure(result.Error, result.Pos);
                        }

                        animVars.Add(result.Value);
                        this._position = FnisLineParser.NextLine(this._source, line.End);
                        break;
                    }

                case FnisAnimType.Alternate:
                    {
                        FnisListParseErrorKind? error = this.ValidatePendingSequence();
                        if (error is not null)
                        {
                            return FnisListParseResult<FnisPattern>.Failure(error.Value, this._position);
                        }

                        FnisListParseResult<FnisAlternateAnimation> result = FnisAAParser.Parse(this._source, ref this._position);

                        if (result.IsFailure)
                        {
                            return FnisListParseResult<FnisPattern>.Failure(
                                result.Error,
                                result.Pos);
                        }

                        alternateAnimations.Add(result.Value);
                        break;
                    }

                default:
                    {
                        FnisListParseResult<FnisAnimation> result = FnisLineParser.Parse(this._source, line);

                        if (result.IsFailure)
                        {
                            return FnisListParseResult<FnisPattern>.Failure(result.Error, result.Pos);
                        }

                        FnisAnimation animation = result.Value;

                        if (animation.Type != FnisAnimType.SequencedContinued)
                        {
                            FnisListParseErrorKind? error = this.ValidatePendingSequence();

                            if (error is not null)
                            {
                                return FnisListParseResult<FnisPattern>.Failure(error.Value, this._position);
                            }
                        }

                        {
                            FnisListParseErrorKind? error = this.ValidateAnimation(animation);
                            if (error is not null)
                            {
                                return FnisListParseResult<FnisPattern>.Failure(error.Value, line.Pos);
                            }
                        }

                        animations.Add(animation);

                        this._position = FnisLineParser.NextLine(this._source, line.End);
                        break;
                    }
            }
        }

        return FnisListParseResult<FnisPattern>.Success(new FnisPattern(animations, animVars, alternateAnimations), this._position);
    }

    private FnisListParseErrorKind? ValidateAnimation(FnisAnimation animation)
    {
        switch (animation.Type)
        {
            case FnisAnimType.Chair:
                return this.StartChair(animation);

            case FnisAnimType.Furniture:
            case FnisAnimType.FurnitureOptimized:
                return this.StartFurniture(animation);

            case FnisAnimType.Sequenced:
            case FnisAnimType.SequencedOptimized:
                this._canContinueSequence = true;
                return null;

            case FnisAnimType.SequencedContinued:
                if (!this._canContinueSequence)
                {
                    return FnisListParseErrorKind.InvalidSequence;
                }

                if (this._chairActive)
                {
                    this._chairContinuationCount++;
                }

                if (this._furnitureActive)
                {
                    this._furnitureAnimationCount++;
                    this._furniturePreviousAcyclic = this._furnitureLastAcyclic;
                    this._furnitureLastAcyclic = HasAcyclicFlag(animation);
                }

                this._canContinueSequence = true;
                return null;

            default:
                this._canContinueSequence = false;
                return null;
        }
    }

    private FnisListParseErrorKind? StartChair(FnisAnimation animation)
    {
        FnisAnimFlags flags = animation.Flags;

        if (flags != FnisAnimFlags.None && flags != FnisAnimFlags.AnimObjects)
        {
            return FnisListParseErrorKind.ChairAllowsOnlyNoFlagsOrAnimationObjects;
        }

        this._chairActive = true;
        this._chairContinuationCount = 0;
        this._canContinueSequence = true;

        return null;
    }

    private FnisListParseErrorKind? StartFurniture(FnisAnimation animation)
    {
        if (!HasAcyclicFlag(animation))
        {
            return FnisListParseErrorKind.FurnitureRequiresAcyclic;
        }

        this._furnitureActive = true;
        this._furnitureAnimationCount = 1;
        this._furniturePreviousAcyclic = false;
        this._furnitureLastAcyclic = true;
        this._canContinueSequence = true;

        return null;
    }

    private FnisListParseErrorKind? ValidatePendingSequence()
    {
        if (this._chairActive)
        {
            if (this._chairContinuationCount < 3)
            {
                return FnisListParseErrorKind.ChairRequiresThreeContinuations;
            }

            this._chairActive = false;
            this._chairContinuationCount = 0;
        }

        if (this._furnitureActive)
        {
            if (this._furnitureAnimationCount < 3)
            {
                return FnisListParseErrorKind.FurnitureRequiresThreeAnimations;
            }

            if (!this._furnitureLastAcyclic)
            {
                return FnisListParseErrorKind.FurnitureRequiresAcyclicLastAnimation;
            }

            // Intended: second-to-last must be cyclic
            if (this._furniturePreviousAcyclic == this._furnitureLastAcyclic)
            {
                return FnisListParseErrorKind.FurnitureSecondToLastMustBeCyclic;
            }

            this._furnitureActive = false;
            this._furnitureAnimationCount = 0;
            this._furniturePreviousAcyclic = false;
            this._furnitureLastAcyclic = false;
        }

        return null;
    }

    private static bool HasAcyclicFlag(FnisAnimation animation)
    {
        return animation.Flags.HasFlag(FnisAnimFlags.Acyclic);
    }

    private TextSpan GetCurrentLine()
    {
        int end = FnisLineParser.FindLineEnd(this._source, this._position);
        return TextSpan.FromRange(this._position, end);
    }
}
