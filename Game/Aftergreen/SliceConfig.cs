using System.Text.Json;

namespace Graphite.Game.Aftergreen;

public sealed class SliceConfig
{
    public float TargetMass { get; set; } = 1000;
    public float PlayerSpeed { get; set; } = 150;
    public float VacuumRange { get; set; } = 145;
    public float PullSpeed { get; set; } = 95;
    public float HopperCapacity { get; set; } = 100;
    public float UpgradedCapacity { get; set; } = 160;
    public float ExcavationSeconds { get; set; } = 3.2f;
    public int[] UpgradeCosts { get; set; } = [65, 110, 150];
    public int BotCost { get; set; } = 125;
    public float BotSpeed { get; set; } = 100;
    public float BotCapacity { get; set; } = 24;
    public int LightCount { get; set; } = 220;
    public int MediumCount { get; set; } = 80;
    public int HeavyCount { get; set; } = 18;
    public float LightMass { get; set; } = 1;
    public float MediumMass { get; set; } = 3;
    public float HeavyMass { get; set; } = 15;
    public float AnchorMass { get; set; } = 20;
    public float StructureMass { get; set; } = 95;

    public static SliceConfig Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Content", "slice.json");
        var config = JsonSerializer.Deserialize<SliceConfig>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Slice configuration is empty.");
        if (config.UpgradeCosts.Length != 3 || config.UpgradeCosts.Any(x => x < 0)
            || config.TargetMass <= 0 || config.PlayerSpeed <= 0 || config.VacuumRange <= 0
            || config.PullSpeed <= 0 || config.HopperCapacity < 30 || config.UpgradedCapacity < 30
            || config.ExcavationSeconds <= 0 || config.BotCapacity < 6 || config.BotSpeed <= 0
            || config.BotCost < 0 || config.LightCount < 0 || config.MediumCount < 0 || config.HeavyCount < 0
            || config.LightMass <= 0 || config.MediumMass <= 0 || config.HeavyMass <= 0 || config.AnchorMass <= 0
            || config.StructureMass <= config.AnchorMass * 2 || config.HeavyMass > config.HopperCapacity
            || config.AnchorMass > config.HopperCapacity)
        {
            throw new InvalidDataException("Invalid AFTERGREEN slice tuning values.");
        }
        return config;
    }
}
