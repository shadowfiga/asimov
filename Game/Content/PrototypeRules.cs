using Chisel.Generated;

namespace Graphite.Game.Content;

public sealed record PrototypeRules(
    double QuarterSeconds,
    double ProfitTarget,
    int StartingWorkforce,
    int ActiveContractSlots,
    int HandSize,
    double WorkerEfficiency,
    double BlackSpeedMultiplier,
    double BlackBankruptcyPerSecond,
    double AbandonBankruptcy,
    double ReplacementDelaySeconds)
{
    public static PrototypeRules Current { get; } = Load();

    private static PrototypeRules Load()
    {
        const int index = (int)ChiselGameRulesId.PROTOTYPE;
        if (ChiselGameRules.Count != 1 || ChiselGameRules.Slugs[index] != "PROTOTYPE")
        {
            throw new InvalidDataException("Chisel must export exactly one PROTOTYPE game-rules row.");
        }

        return new PrototypeRules(
            ChiselGameRules.QuarterSeconds[index],
            ChiselGameRules.ProfitTarget[index],
            ChiselGameRules.StartingWorkforce[index],
            ChiselGameRules.ActiveContractSlots[index],
            ChiselGameRules.HandSize[index],
            ChiselGameRules.WorkerEfficiency[index],
            ChiselGameRules.BlackSpeedMultiplier[index],
            ChiselGameRules.BlackBankruptcyPerSecond[index],
            ChiselGameRules.AbandonBankruptcy[index],
            ChiselGameRules.ReplacementDelaySeconds[index]);
    }
}
