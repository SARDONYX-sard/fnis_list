using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace fnis_list;

public readonly struct FnisAnimation {
    private readonly FnisAnimType _type;
    private readonly TextSpan _event;
    private readonly TextSpan _file;
    private readonly List<FnisAnimObjectSpan> _objects;

    private readonly FnisAnimFlags _flags;
    private readonly float? _blendTime;
    private readonly float? _duration;

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
        List<FnisRotation> rotationData) {
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

    public ReadOnlySpan<char> AnimEvent(ReadOnlySpan<char> source) {
        return this._event.Slice(source);
    }

    public ReadOnlySpan<char> AnimFile(ReadOnlySpan<char> source) {
        return this._file.Slice(source);
    }

    public int TriggerCount => this._triggers.Count;
    public int Trigger2Count => this._triggers2.Count;
    public int ObjectCount => this._objects.Count;

    public int MotionDataCount => this._motionData.Count;
    public int RotationDataCount => this._rotationData.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetTrigger(ReadOnlySpan<char> source, int index, out FnisTrigger trigger) {
        trigger = default;
        if ((uint)index >= (uint)this.TriggerCount) {
            return false;
        }

        FnisTriggerSpan data = this._triggers[index];
        trigger = new FnisTrigger(data.Event.Slice(source), data.Time);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetTrigger2(ReadOnlySpan<char> source, int index, out FnisTrigger trigger) {
        trigger = default;
        if ((uint)index >= (uint)this.Trigger2Count) {
            return false;
        }

        FnisTriggerSpan data = this._triggers2[index];
        trigger = new FnisTrigger(data.Event.Slice(source), data.Time);

        return true;
    }

    public bool TryGetObject(ReadOnlySpan<char> source, int index, out FnisAnimObject value) {
        value = default;
        if ((uint)index >= (uint)this.ObjectCount) {
            return false;
        }

        FnisAnimObjectSpan data = this._objects[index];
        value = new FnisAnimObject(data.Name.Slice(source), data.Role);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetMotionData(int index, out FnisMotion value) {
        if ((uint)index >= (uint)this.MotionDataCount) {
            value = default;
            return false;
        }

        value = this._motionData[index];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetRotationData(int index, out FnisRotation value) {
        if ((uint)index >= (uint)this.RotationDataCount) {
            value = default;
            return false;
        }

        value = this._rotationData[index];
        return true;
    }


    /// <summary>
    /// Returns whether the animation file should be included in the consistency check.
    /// </summary>
    /// <remarks>
    /// Animations marked with <see cref="FnisAnimFlags.BSA"/> or
    /// <see cref="FnisAnimFlags.Known"/> are excluded from the consistency check.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldCheckAnimFile() {
        return (this._flags & (FnisAnimFlags.BSA | FnisAnimFlags.Known)) == 0;
    }
}
