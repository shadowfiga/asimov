using Microsoft.Xna.Framework;

namespace Graphite.Engine.Objects;

/// <summary>Local TRS composed through the parent. World values are always current, including between updates.</summary>
public sealed class Transform2D
{
    private Vector2 _position;
    private float _rotation;
    private Vector2 _scale = Vector2.One;
    public GameObject Owner
    {
        get;
    }
    internal Transform2D(GameObject owner) => Owner = owner;

    public Vector2 LocalPosition
    {
        get => _position;
        set
        {
            Validate(value);
            _position = value;
        }
    }
    public float LocalRotation
    {
        get => _rotation;
        set
        {
            if (!float.IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            _rotation = value;
        }
    }
    public Vector2 LocalScale
    {
        get => _scale;
        set
        {
            Validate(value);
            if (value.X <= 0 || value.Y <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Transform scale must be positive. Use sprite effects for mirroring.");
            }
            _scale = value;
        }
    }
    public Matrix LocalMatrix => Matrix.CreateScale(_scale.X, _scale.Y, 1)
        * Matrix.CreateRotationZ(_rotation) * Matrix.CreateTranslation(_position.X, _position.Y, 0);
    public Matrix WorldMatrix => LocalMatrix * (Owner.Parent?.Transform.WorldMatrix ?? Matrix.Identity);
    public Vector2 WorldPosition
    {
        get => TransformPoint(Vector2.Zero);
        set
        {
            Validate(value);
            LocalPosition = Owner.Parent?.Transform.InverseTransformPoint(value) ?? value;
        }
    }
    public Vector2 Forward => Vector2.Normalize(Vector2.TransformNormal(Vector2.UnitX, WorldMatrix));
    public float WorldRotation
    {
        get => MathF.Atan2(Forward.Y, Forward.X);
        set
        {
            if (!float.IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            var direction = new Vector2(MathF.Cos(value), MathF.Sin(value));
            if (Owner.Parent is not null)
            {
                direction = Vector2.TransformNormal(direction, Matrix.Invert(Owner.Parent.Transform.WorldMatrix));
            }
            LocalRotation = MathF.Atan2(direction.Y, direction.X);
        }
    }

    public Vector2 TransformPoint(Vector2 point) => Vector2.Transform(point, WorldMatrix);
    public Vector2 InverseTransformPoint(Vector2 point) => Vector2.Transform(point, Matrix.Invert(WorldMatrix));
    public void FaceWorldPoint(Vector2 target)
    {
        Validate(target);
        var direction = target - WorldPosition;
        if (direction.LengthSquared() > .0001f)
        {
            WorldRotation = MathF.Atan2(direction.Y, direction.X);
        }
    }

    internal static (Vector2 Position, float Rotation, Vector2 Scale) Decompose(Matrix matrix)
    {
        var x = new Vector2(matrix.M11, matrix.M12);
        var y = new Vector2(matrix.M21, matrix.M22);
        var scale = new Vector2(x.Length(), y.Length());
        Validate(scale);
        if (scale.X <= 0 || scale.Y <= 0 || matrix.Determinant() <= 0
            || Math.Abs(Vector2.Dot(x / scale.X, y / scale.Y)) > .0001f)
        {
            throw new InvalidOperationException("Keeping this world transform would require shear or reflection in the local transform.");
        }
        return (new Vector2(matrix.M41, matrix.M42), MathF.Atan2(x.Y, x.X), scale);
    }

    public static void Validate(Vector2 value)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Coordinates must be finite.");
        }
    }
}
