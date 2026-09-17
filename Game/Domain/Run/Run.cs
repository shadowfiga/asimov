using Chisel.Generated;
using Graphite.Engine.Persistence;
using Graphite.Game.Data;

namespace Graphite.Game.Domain.Run;

[SaveContract("deep-drive.run", Version = 1)]
public sealed class Run
{
    [SaveMember("ore")]
    public int Ore;

    [SaveMember("xp")]
    public int XP;

    [SaveMember("kills")]
    public int Kills;

    [SaveMember("duration")]
    public TimeSpan Duration;

    [SaveMember("modifiers")]
    public int[] Modifiers = [];

    public event Action? OreChanged;

    public void AddTime(TimeSpan elapsed)
    {
        Duration += elapsed;
    }

    public void AddOre(int amount)
    {
        Ore += amount;
        OreChanged?.Invoke();
    }

    public bool TrySpendOre(int amount)
    {
        if (Ore < amount)
        {
            return false;
        }
        AddOre(-amount);
        return true;
    }

    /// <summary>Called once per enemy death; rewards come directly from its Chisel definition.</summary>
    public void RecordKill(ChiselEnemiesId enemyId)
    {
        var experience = ChiselEnemies.Experience[enemyId.ToInt()];
        Kills++;
        XP += experience;
    }
}
