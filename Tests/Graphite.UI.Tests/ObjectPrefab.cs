using Graphite.Engine.Objects;

namespace Graphite.UI.Tests;

/// <summary>Minimal root fixture for low-level rendering and lifecycle tests.</summary>
internal sealed class ObjectPrefab(string name) : Prefab<GameObject>(name)
{
    protected internal override GameObject Build(GameObject root) => root;
}
