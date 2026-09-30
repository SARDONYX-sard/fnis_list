using System;

namespace fnis_list;

public readonly ref struct FnisTrigger {
    private readonly ReadOnlySpan<char> _event;
    private readonly float _time;

    public FnisTrigger(ReadOnlySpan<char> @event, float time) {
        this._event = @event;
        this._time = time;
    }

    public ReadOnlySpan<char> Event => this._event;
    public float Time => this._time;
}

public readonly struct FnisTriggerSpan {
    public readonly TextSpan Event;
    public readonly float Time;

    public FnisTriggerSpan(TextSpan @event, float time) {
        this.Event = @event;
        this.Time = time;
    }
}
