using System;
using System.Numerics;

namespace FnisList;

/// <summary>
/// Represents FNIS motion data from an <c>MD</c> definition.
/// </summary>
public readonly struct FnisMotion {
    private readonly float _time;
    private readonly float _deltaX;
    private readonly float _deltaY;
    private readonly float _deltaZ;

    internal FnisMotion(float time, float deltaX, float deltaY, float deltaZ) {
        this._time = time;
        this._deltaX = deltaX;
        this._deltaY = deltaY;
        this._deltaZ = deltaZ;
    }

    /// <summary>
    /// Gets the motion time.
    /// </summary>
    public float Time => this._time;

    /// <summary>
    /// Gets the X-axis translation delta.
    /// </summary>
    public float DeltaX => this._deltaX;

    /// <summary>
    /// Gets the Y-axis translation delta.
    /// </summary>
    public float DeltaY => this._deltaY;

    /// <summary>
    /// Gets the Z-axis translation delta.
    /// </summary>
    public float DeltaZ => this._deltaZ;
}

/// <summary>
/// Represents FNIS rotation data from an <c>RD</c> definition.
/// </summary>
public readonly struct FnisRotation {
    private readonly float _time;
    private readonly FnisRotationKind _kind;
    private readonly float _x;
    private readonly float _y;
    private readonly float _z;
    private readonly float _w;

    internal FnisRotation(float time, FnisRotationKind kind, float x, float y, float z, float w) {
        this._time = time;
        this._kind = kind;
        this._x = x;
        this._y = y;
        this._z = z;
        this._w = w;
    }

    /// <summary>
    /// Gets the rotation time.
    /// </summary>
    public float Time => this._time;

    /// <summary>
    /// Gets the rotation data kind.
    /// </summary>
    public FnisRotationKind Kind => this._kind;

    /// <summary>
    /// Gets the quaternion value.
    /// </summary>
    ///
    /// <exception cref="InvalidOperationException">
    /// Thrown when this rotation uses the <see cref="FnisRotationKind.DeltaZAngle"/> format.
    /// </exception>
    public Quaternion Quaternion {
        get {
            if (this._kind != FnisRotationKind.Quaternion) {
                throw new InvalidOperationException("The rotation data does not contain a quaternion.");
            }

            return new Quaternion(this._x, this._y, this._z, this._w);
        }
    }

    /// <summary>
    /// Gets the Z-axis rotation angle in degrees.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this rotation uses the <see cref="FnisRotationKind.Quaternion"/> format.
    /// </exception>
    public float ZAngle {
        get {
            if (this._kind != FnisRotationKind.DeltaZAngle) {
                throw new InvalidOperationException("The rotation data does not contain a Z-axis angle.");
            }

            return this._z;
        }
    }

    /// <summary>
    /// Converts the rotation data to a quaternion.
    /// </summary>
    /// <returns>
    /// The stored quaternion, or a quaternion representing the Z-axis angle.
    /// </returns>
    public Quaternion ToQuaternion() {
        if (this._kind == FnisRotationKind.Quaternion) {
            return new Quaternion(this._x, this._y, this._z, this._w);
        }

        float radians = this._z * (MathF.PI / 180.0f);
        float halfRadians = radians * 0.5f;
        return new Quaternion(0.0f, 0.0f, MathF.Sin(halfRadians), MathF.Cos(halfRadians));
    }
}

/// <summary>
/// Specifies the format of FNIS rotation data.
/// </summary>
public enum FnisRotationKind {
    Quaternion,
    DeltaZAngle,
}
