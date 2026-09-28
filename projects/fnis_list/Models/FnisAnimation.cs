using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace fnis_list;

public readonly struct FnisAnimation
{
    private readonly FnisAnimType _type;
    private readonly TextSpan _event;
    private readonly TextSpan _file;
    private readonly List<FnisAnimObjectData> _objects;

    private readonly FnisAnimFlags _flags;
    private readonly float? _blendTime;
    private readonly float? _duration;

    private readonly List<FnisTriggerData> _triggers;
    private readonly List<FnisTriggerData> _triggers2;


    private readonly List<FnisMotionData> _motionData;
    private readonly List<FnisRotationData> _rotationData;

    internal FnisAnimation(
        FnisAnimType type,
        TextSpan @event,
        TextSpan file,
        List<FnisAnimObjectData> objects,
        FnisAnimFlags flags,
        float? blendTime,
        float? duration,
        List<FnisTriggerData> triggers,
        List<FnisTriggerData> triggers2,
          List<FnisMotionData> motionData,
        List<FnisRotationData> rotationData)
    {
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

    public FnisAnimType Type => this._type;

    public FnisAnimFlags Flags => this._flags;

    public bool HasBlendTime => this._blendTime.HasValue;
    public float? BlendTime => this._blendTime;

    public bool HasDuration => this._duration.HasValue;
    public float? Duration => this._duration;

    public ReadOnlySpan<char> AnimEvent(ReadOnlySpan<char> source)
    {
        return this._event.Slice(source);
    }

    public ReadOnlySpan<char> AnimFile(ReadOnlySpan<char> source)
    {
        return this._file.Slice(source);
    }

    public int TriggerCount => this._triggers.Count;
    public int Trigger2Count => this._triggers2.Count;
    public int ObjectCount => this._objects.Count;

    public int MotionDataCount => this._motionData.Count;
    public int RotationDataCount => this._rotationData.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetTrigger(ReadOnlySpan<char> source, int index, out FnisTrigger trigger)
    {
        trigger = default;
        if ((uint)index >= (uint)this.TriggerCount)
        {
            return false;
        }

        FnisTriggerData data = this._triggers[index];
        trigger = new FnisTrigger(data.Event.Slice(source), data.Time);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetTrigger2(ReadOnlySpan<char> source, int index, out FnisTrigger trigger)
    {
        trigger = default;
        if ((uint)index >= (uint)this.Trigger2Count)
        {
            return false;
        }

        FnisTriggerData data = this._triggers2[index];
        trigger = new FnisTrigger(data.Event.Slice(source), data.Time);

        return true;
    }

    public bool TryGetObject(ReadOnlySpan<char> source, int index, out FnisAnimObject value)
    {
        value = default;
        if ((uint)index >= (uint)this.ObjectCount)
        {
            return false;
        }

        FnisAnimObjectData data = this._objects[index];
        value = new FnisAnimObject(data.Name.Slice(source), data.Role);

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetMotionData(int index, out FnisMotionData value)
    {
        if ((uint)index >= (uint)this.MotionDataCount)
        {
            value = default;
            return false;
        }

        value = this._motionData[index];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetRotationData(int index, out FnisRotationData value)
    {
        if ((uint)index >= (uint)this.RotationDataCount)
        {
            value = default;
            return false;
        }

        value = this._rotationData[index];
        return true;
    }

    internal void AddMotionData(FnisMotionData value)
    {
        this._motionData.Add(value);
    }

    internal void AddRotationData(FnisRotationData value)
    {
        this._rotationData.Add(value);
    }
}
