using Graphite.Engine.Objects;

namespace Graphite.Gameplay.Tests;

/// <summary>Minimal root fixture for low-level object/component tests.</summary>
internal sealed class ObjectPrefab(string name) : Prefab<GameObject>(name)
{
    protected override GameObject Build(GameObject root) => root;
}
