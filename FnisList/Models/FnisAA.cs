// Maintainer note:
// Do not add mutating methods for sets or triggers.
// This preserves the safety invariant required by CollectionsMarshal.AsSpan.
using System.Collections.Generic;

namespace FnisList;

/// <summary>
/// Represents an alternative animation set declared by an <c>AAset</c> line.
/// </summary>
public readonly struct FnisAASetSpan {
    private readonly TextSpan _group;
    private readonly ulong _slots;

    internal FnisAASetSpan(TextSpan group, ulong slots) {
        this._group = group;
        this._slots = slots;
    }

    /// <summary>
    /// Gets the animation group name.
    /// </summary>
    public TextSpan Group => this._group;

    /// <summary>
    /// Gets the number of slots available in the animation group.
    /// </summary>
    public ulong Slots => this._slots;
}

/// <summary>
/// Represents an alternative animation and its trigger definitions declared by a <c>T</c> line.
/// </summary>
public readonly struct FnisAnimTriggerSpan {
    private readonly TextSpan _animName;
    private readonly List<FnisTriggerSpan> _triggers;

    internal FnisAnimTriggerSpan(TextSpan animName, List<FnisTriggerSpan> triggers) {
        this._animName = animName;
        this._triggers = triggers;
    }

    /// <summary>
    /// Gets the name of the alternative animation.
    /// </summary>
    public TextSpan AnimName => this._animName;

    /// <summary>
    /// Gets the trigger definitions associated with the animation.
    /// </summary>
    public List<FnisTriggerSpan> Triggers => this._triggers;
}

/// <summary>
/// Represents an FNIS alternate-animation block beginning with an <c>AAprefix</c> line.
/// </summary>
public sealed class FnisAlternateAnimation {
    private readonly TextSpan _prefix;
    private readonly List<FnisAASetSpan> _sets;
    private readonly List<FnisAnimTriggerSpan> _triggers;

    internal FnisAlternateAnimation(TextSpan prefix, List<FnisAASetSpan> sets, List<FnisAnimTriggerSpan> triggers) {
        this._prefix = prefix;
        this._sets = sets;
        this._triggers = triggers;
    }

    /// <summary>
    /// Gets the alternate-animation prefix.
    /// </summary>
    public TextSpan Prefix => this._prefix;

    /// <summary>
    /// Gets the animation sets declared by this alternate-animation block.
    /// </summary>
    public IReadOnlyList<FnisAASetSpan> Sets => this._sets;

    /// <summary>
    /// Gets the animations and their triggers declared by this alternate-animation block.
    /// </summary>
    public IReadOnlyList<FnisAnimTriggerSpan> Triggers => this._triggers;
}
