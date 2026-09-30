namespace fnis_list;

/// <summary>
/// Represents the typed value of an <c>AnimVar</c> definition.
///
/// <para>
/// The variable may also need to be registered in the following structures:
/// </para>
/// <para>- hkbBehaviorGraphStringData.variableNames</para>
/// <para>- hkbVariableValueSet.wordVariableValues</para>
/// <para>- hkbBehaviorGraphData.variableInfos</para>
/// </summary>
public readonly struct FnisAnimVarValue {
    private readonly FnisAnimVarValueKind _kind;
    private readonly bool _bool;
    private readonly int _int32;
    private readonly float _real;

    private FnisAnimVarValue(FnisAnimVarValueKind kind, bool @bool, int int32, float real) {
        this._kind = kind;
        this._bool = @bool;
        this._int32 = int32;
        this._real = real;
    }

    public FnisAnimVarValueKind Kind => this._kind;

    public bool Bool => this._bool;

    public int Int32 => this._int32;

    public float Real => this._real;

    internal static FnisAnimVarValue FromBool(bool value) {
        return new FnisAnimVarValue(FnisAnimVarValueKind.Bool, value, default, default);
    }

    internal static FnisAnimVarValue FromInt32(int value) {
        return new FnisAnimVarValue(FnisAnimVarValueKind.Int32, default, value, default);
    }

    internal static FnisAnimVarValue FromReal(float value) {
        return new FnisAnimVarValue(FnisAnimVarValueKind.Real, default, default, value);
    }
}

/// <summary>
/// Specifies the type of value stored by an <c>AnimVar</c>.
/// </summary>
public enum FnisAnimVarValueKind {
    Bool,
    Int32,
    Real,
}

/// <summary>
/// Represents an FNIS <c>AnimVar</c> definition.
/// </summary>
public readonly struct FnisAnimVarSpan {
    private readonly TextSpan _name;
    private readonly FnisAnimVarValue _value;

    internal FnisAnimVarSpan(TextSpan name, FnisAnimVarValue value) {
        this._name = name;
        this._value = value;
    }

    /// <summary>
    /// Gets the variable name.
    /// </summary>
    public TextSpan Name => this._name;

    /// <summary>
    /// Gets the typed variable value.
    /// </summary>
    public FnisAnimVarValue Value => this._value;
}
