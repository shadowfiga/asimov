namespace Graphite.Engine.Objects;

/// <summary>A reusable construction recipe. Live state belongs to the objects built on each spawn.</summary>
public abstract class Prefab<T> where T : class
{
    public string Name
    {
        get;
    }

    protected Prefab(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>The engine supplies a positioned root and its owning World.</summary>
    protected internal abstract T Build(GameObject root);
}
