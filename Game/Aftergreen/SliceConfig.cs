namespace Graphite.Game.Aftergreen;

public sealed class SliceConfig
{
    public required float TargetMass { get; init; }
    public required float PlayerSpeed { get; init; }
    public required float VacuumRange { get; init; }
    public required float PullSpeed { get; init; }
    public required float HopperCapacity { get; init; }
    public required float UpgradedCapacity { get; init; }
    public required float ExcavationSeconds { get; init; }
    private IReadOnlyList<int> _upgradeCosts = [];
    public required IReadOnlyList<int> UpgradeCosts
    {
        get => _upgradeCosts;
        init => _upgradeCosts = value is null ? [] : Array.AsReadOnly(value.ToArray());
    }
    public required int BotCost { get; init; }
    public required float BotSpeed { get; init; }
    public required float BotCapacity { get; init; }
    public required int LightCount { get; init; }
    public required int MediumCount { get; init; }
    public required int HeavyCount { get; init; }
    public required float LightMass { get; init; }
    public required float MediumMass { get; init; }
    public required float HeavyMass { get; init; }
    public required float AnchorMass { get; init; }
    public required float StructureMass { get; init; }

    internal void Validate()
    {
        float[] positiveValues = [TargetMass, PlayerSpeed, VacuumRange, PullSpeed, HopperCapacity,
            UpgradedCapacity, ExcavationSeconds, BotSpeed, BotCapacity, LightMass, MediumMass,
            HeavyMass, AnchorMass, StructureMass];
        if (positiveValues.Any(value => !float.IsFinite(value) || value <= 0)
            || UpgradeCosts.Count != 3 || UpgradeCosts.Any(x => x < 0)
            || HopperCapacity < 30 || UpgradedCapacity < HopperCapacity || BotCapacity < 6
            || BotCost < 0 || LightCount < 0 || MediumCount < 0 || HeavyCount < 0
            || StructureMass <= AnchorMass * 2 || HeavyMass > HopperCapacity || AnchorMass > HopperCapacity
            || LightMass > HopperCapacity || MediumMass > HopperCapacity)
        {
            throw new InvalidDataException("Invalid slice tuning values.");
        }
    }
}
