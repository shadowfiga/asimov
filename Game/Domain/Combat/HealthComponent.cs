using Graphite.Engine.Objects;

namespace Graphite.Game.Domain.Combat;

/// <summary>Shared hit points only; each owner decides what death means.</summary>
public sealed class HealthComponent : Component
{
    public int Maximum
    {
        get;
    }
    public int Current
    {
        get; private set;
    }
    public float Ratio => (float)Current / Maximum;
    public bool IsDead => Current == 0;

    public HealthComponent(int maximum)
    {
        if (maximum <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximum));
        }
        Maximum = maximum;
        Current = maximum;
    }

    /// <summary>Returns true only when this hit changes the component from alive to dead.</summary>
    public bool ApplyDamage(int amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount));
        }
        if (IsDead)
        {
            return false;
        }
        Current = Math.Max(0, Current - amount);
        return IsDead;
    }
}
