using Chisel.Generated;
using Graphite.Engine.Objects;
using Graphite.Game.Data;
using Graphite.Game.Domain.Combat;
using Graphite.Game.Domain.Player;

namespace Graphite.Game.Domain.Enemies;

public sealed class EnemyPrefab(ChiselEnemiesId enemyId, PlayerController target)
    : Prefab<EnemyController>(enemyId.ToString())
{
    protected internal override EnemyController Build(GameObject root)
    {
        var index = enemyId.ToInt();
        var health = root.AddComponent(new HealthComponent(ChiselEnemies.MaxHealth[index]));
        var enemy = root.AddComponent(new EnemyController(enemyId, target, health));
        root.AddComponent(new EnemyRenderer(enemy.BodyRadius) { Layer = 20 });
        return enemy;
    }
}
