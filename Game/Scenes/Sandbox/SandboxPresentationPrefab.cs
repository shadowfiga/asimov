using Graphite.Engine.Objects;
using Graphite.Game.Domain.Player;
using Graphite.Game.Graphics;

namespace Graphite.Game.Scenes;

internal sealed class SandboxPresentationPrefab(PlayerController player) : Prefab<ReticleRenderer>("Sandbox presentation")
{
    protected internal override ReticleRenderer Build(GameObject root)
    {
        root.CreateChild("Grid").AddComponent(new PrototypeGridRenderer { Layer = -100 });
        return root.CreateChild("Reticle").AddComponent(new ReticleRenderer(player) { Layer = 1000 });
    }
}
