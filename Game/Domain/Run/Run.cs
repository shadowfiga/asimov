using Graphite.Engine.Persistence;

namespace Graphite.Game.Domain.Run;

[SaveContract("deep-drive.run", Version = 1)]
public sealed class Run
{
    [SaveMember("xp")]
    public int XP;

    [SaveMember("biomass")]
    public int Biomass;

    [SaveMember("kills")]
    public int Kills;

    [SaveMember("durationMs")]
    public int DurationMs;

    [SaveMember("modifiers")]
    public int[] Modifiers = [];
}
