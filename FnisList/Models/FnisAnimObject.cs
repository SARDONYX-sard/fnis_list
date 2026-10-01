using System;

namespace FnisList;

public readonly ref struct FnisAnimObject {
    private readonly ReadOnlySpan<char> _name;
    private readonly FnisActorRole _role;

    public FnisAnimObject(ReadOnlySpan<char> name, FnisActorRole role) {
        this._name = name;
        this._role = role;
    }

    public ReadOnlySpan<char> Name => this._name;
    public FnisActorRole Role => this._role;
}

public enum FnisActorRole : byte {
    Active = 1,
    Passive = 2,
}
