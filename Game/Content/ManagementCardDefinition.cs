using Chisel.Generated;

namespace Graphite.Game.Content;

public enum ManagementCardEffect
{
    SpeedMultiplier,
    TemporaryWorkers,
    RemainingProgress,
    QualityControl,
    ShipIt
}

public sealed record ManagementCardDefinition(
    ChiselManagementCardsId Id,
    string Slug,
    string Name,
    string Description,
    ManagementCardEffect Effect,
    double Magnitude,
    double DurationSeconds,
    double BankruptcyDelta,
    IReadOnlyList<string> CompatibleTags)
{
    public static IReadOnlyList<ManagementCardDefinition> All { get; } = LoadAll();

    private static IReadOnlyList<ManagementCardDefinition> LoadAll()
    {
        var definitions = new List<ManagementCardDefinition>(ChiselManagementCards.Count);
        for (var index = 0; index < ChiselManagementCards.Count; index++)
        {
            definitions.Add(new ManagementCardDefinition(
                (ChiselManagementCardsId)index,
                ChiselManagementCards.Slugs[index],
                ChiselManagementCards.DisplayName[index],
                ChiselManagementCards.Description[index],
                ParseEffect(ChiselManagementCards.Effect[index]),
                ChiselManagementCards.Magnitude[index],
                ChiselManagementCards.DurationSeconds[index],
                ChiselManagementCards.BankruptcyDelta[index],
                Array.AsReadOnly(ChiselManagementCards.CompatibleTags[index].ToArray())));
        }

        if (definitions.Count == 0)
        {
            throw new InvalidDataException("Chisel exported no Management Card definitions.");
        }

        return definitions.AsReadOnly();
    }

    private static ManagementCardEffect ParseEffect(string effect) => effect switch
    {
        "SPEED_MULTIPLIER" => ManagementCardEffect.SpeedMultiplier,
        "TEMP_WORKERS" => ManagementCardEffect.TemporaryWorkers,
        "REMAINING_PROGRESS" => ManagementCardEffect.RemainingProgress,
        "QUALITY_CONTROL" => ManagementCardEffect.QualityControl,
        "SHIP_IT" => ManagementCardEffect.ShipIt,
        _ => throw new InvalidDataException($"Chisel exported unsupported Management Card effect '{effect}'.")
    };
}
