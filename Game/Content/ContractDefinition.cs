using Chisel.Generated;

namespace Graphite.Game.Content;

public sealed record ContractDefinition(
    ChiselContractsId Id,
    string Slug,
    string Name,
    double Payout,
    double WorkRequired,
    double DeadlineSeconds,
    string BaseRisk,
    IReadOnlyList<string> Tags,
    double BlackRiskMultiplier,
    double FailureBankruptcy,
    string SpecialModifier)
{
    public static IReadOnlyList<ContractDefinition> All { get; } = LoadAll();

    private static IReadOnlyList<ContractDefinition> LoadAll()
    {
        var definitions = new List<ContractDefinition>(ChiselContracts.Count);
        for (var index = 0; index < ChiselContracts.Count; index++)
        {
            definitions.Add(new ContractDefinition(
                (ChiselContractsId)index,
                ChiselContracts.Slugs[index],
                ChiselContracts.DisplayName[index],
                ChiselContracts.Payout[index],
                ChiselContracts.WorkRequired[index],
                ChiselContracts.DeadlineSeconds[index],
                ChiselContracts.BaseRisk[index],
                Array.AsReadOnly(ChiselContracts.Tags[index].ToArray()),
                ChiselContracts.BlackRiskMultiplier[index],
                ChiselContracts.FailureBankruptcy[index],
                ChiselContracts.SpecialModifier[index]));
        }

        if (definitions.Count == 0)
        {
            throw new InvalidDataException("Chisel exported no contract definitions.");
        }

        return definitions.AsReadOnly();
    }
}
