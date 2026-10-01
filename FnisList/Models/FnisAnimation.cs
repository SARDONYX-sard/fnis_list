using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace FnisList;

public readonly struct FnisAnimation {
    private readonly FnisAnimType _type;
    private readonly TextSpan _event;
    private readonly TextSpan _file;

    private readonly FnisAnimFlags _flags;
    private readonly float? _blendTime;
    private readonly float? _duration;

    private readonly List<FnisAnimObjectSpan> _objects;
    private readonly List<FnisTriggerSpan> _triggers;
    private readonly List<FnisTriggerSpan> _triggers2;
    private readonly List<FnisMotion> _motionData;
    private readonly List<FnisRotation> _rotationData;

    internal FnisAnimation(
        FnisAnimType type,
        TextSpan @event,
        TextSpan file,
        List<FnisAnimObjectSpan> objects,
        FnisAnimFlags flags,
        float? blendTime,
        float? duration,
        List<FnisTriggerSpan> triggers,
        List<FnisTriggerSpan> triggers2,
        List<FnisMotion> motionData,
        List<FnisRotation> rotationData
    ) {
        this._type = type;
        this._event = @event;
        this._file = file;
        this._objects = objects;
        this._flags = flags;
        this._blendTime = blendTime;
        this._duration = duration;
        this._triggers = triggers;
        this._triggers2 = triggers2;
        this._motionData = motionData;
        this._rotationData = rotationData;
    }

    internal void PushMotionData(FnisMotion value) {
        this._motionData.Add(value);
    }

    internal void PushRotationData(FnisRotation value) {
        this._rotationData.Add(value);
    }

    public FnisAnimType Type => this._type;
    public FnisAnimFlags Flags => this._flags;

    public bool HasBlendTime => this._blendTime.HasValue;
    public float? BlendTime => this._blendTime;
    public bool HasDuration => this._duration.HasValue;
    public float? Duration => this._duration;

    public ReadOnlySpan<char> AnimEvent(ReadOnlySpan<char> source) => this._event.Slice(source);
    public ReadOnlySpan<char> AnimFile(ReadOnlySpan<char> source) => this._file.Slice(source);
    public IReadOnlyList<FnisAnimObjectSpan> Objects => this._objects;
    public IReadOnlyList<FnisTriggerSpan> Triggers => this._triggers;
    public IReadOnlyList<FnisTriggerSpan> Triggers2 => this._triggers2;
    public IReadOnlyList<FnisMotion> MotionData => this._motionData;
    public IReadOnlyList<FnisRotation> RotationData => this._rotationData;

    /// <summary>
    /// Returns whether the animation path should be registered for each actor,
    /// such as <c>defaultmale.hkx</c>.
    /// </summary>
    ///
    /// <remarks>
    /// Internally, this checks whether <c>FnisAnimFlags.BSA</c> or <c>FnisAnimFlags.Known</c> is't set.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldCheckAnimFile() => (this._flags & (FnisAnimFlags.BSA | FnisAnimFlags.Known)) == 0;
}
