using Chisel.Generated;
using Graphite.Engine.Objects;
using Graphite.Game.Data;
using Graphite.Game.Domain.Combat;

namespace Graphite.Game.Domain.Enemies;

public sealed class EnemyPrefab(ChiselEnemiesId enemyId)
    : Prefab<EnemyController>(enemyId.ToString())
{
    protected internal override EnemyController Build(GameObject root)
    {
        var index = enemyId.ToInt();
        root.AddComponent(new HealthComponent(ChiselEnemies.MaxHealth[index]));
        var enemy = root.AddComponent(new EnemyController(enemyId));
        root.AddComponent(new EnemyRenderer(enemy.BodyRadius) { Layer = 20 });
        return enemy;
    }
}
