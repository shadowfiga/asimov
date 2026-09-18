using Chisel.Generated;
using Graphite.Engine.Objects;
using Graphite.Game.Domain.Combat;

namespace Graphite.Game.Domain.Enemies;

public sealed class EnemyPrefab(int enemyId)
    : Prefab<EnemyController>(ChiselEnemies.Slugs[enemyId])
{
    protected internal override EnemyController Build(GameObject root)
    {
        root.AddComponent(new HealthComponent(ChiselEnemies.MaxHealth[enemyId]));
        var enemy = root.AddComponent(new EnemyController(enemyId));
        root.AddComponent(new EnemyRenderer(enemy.BodyRadius) { Layer = 20 });
        return enemy;
    }
}
