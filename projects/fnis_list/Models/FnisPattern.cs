using System.Collections.Generic;

namespace fnis_list;

/// <summary>
/// Represents the parsed contents of an FNIS list file.
/// </summary>
public sealed class FnisPattern {
    private readonly List<FnisAnimation> _animations;
    private readonly List<FnisAnimVarSpan> _animVars;
    private readonly List<FnisAlternateAnimation> _alternateAnimations;

    internal FnisPattern(List<FnisAnimation> animations, List<FnisAnimVarSpan> animVars, List<FnisAlternateAnimation> alternateAnimations) {
        this._animations = animations;
        this._animVars = animVars;
        this._alternateAnimations = alternateAnimations;
    }

    /// <summary>
    /// Gets the normal animations.
    /// </summary>
    public IReadOnlyList<FnisAnimation> Animations => this._animations;

    /// <summary>
    /// Gets the animation variables.
    /// </summary>
    public IReadOnlyList<FnisAnimVarSpan> AnimVars => this._animVars;

    /// <summary>
    /// Gets the alternate-animation blocks.
    /// </summary>
    public IReadOnlyList<FnisAlternateAnimation> AlternateAnimations => this._alternateAnimations;
}
