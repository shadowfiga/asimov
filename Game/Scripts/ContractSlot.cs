using Graphite.Game.Content;

namespace Graphite.Game.Scripts;

public enum ContractSlotState
{
    Offer,
    Active,
    Replacing
}

public sealed class ContractSlot
{
    private readonly List<TimedContractEffect> _effects = [];
    private ContractDefinition? _definition;

    internal ContractSlot(int index)
    {
        Index = index;
    }

    public int Index { get; }
    public ContractSlotState State { get; private set; }
    public int AssignedWorkers { get; private set; }
    public double Progress { get; private set; }
    public double DeadlineRemaining { get; private set; }
    public bool IsBlack { get; private set; }
    public double ReplacementTimeRemaining { get; private set; }
    public bool HasDefinition => _definition is not null;

    public ContractDefinition Definition => _definition
        ?? throw new InvalidOperationException($"Contract slot {Index + 1} has no current definition.");

    public double ProgressRatio => State == ContractSlotState.Active
        ? Math.Clamp(Progress / Definition.WorkRequired, 0d, 1d)
        : 0d;

    public int TemporaryWorkers => _effects.Sum(effect => effect.TemporaryWorkers);
    public double EffectiveWorkers => AssignedWorkers + TemporaryWorkers;

    public string ActiveEffects => _effects.Count == 0
        ? "None"
        : string.Join(" | ", _effects.Select(effect => $"{effect.Name} {effect.Remaining:0.0}s"));

    internal void SetOffer(ContractDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        _definition = definition;
        State = ContractSlotState.Offer;
        AssignedWorkers = 0;
        Progress = 0d;
        DeadlineRemaining = definition.DeadlineSeconds;
        IsBlack = false;
        ReplacementTimeRemaining = 0d;
        _effects.Clear();
    }

    internal void Accept()
    {
        if (State != ContractSlotState.Offer)
        {
            throw new InvalidOperationException($"Contract slot {Index + 1} is not an available offer.");
        }

        State = ContractSlotState.Active;
        DeadlineRemaining = Definition.DeadlineSeconds;
    }

    internal void ChangeWorkers(int change)
    {
        var next = AssignedWorkers + change;
        if (State != ContractSlotState.Active || next < 0)
        {
            throw new InvalidOperationException($"Cannot change workers on contract slot {Index + 1}.");
        }

        AssignedWorkers = next;
    }

    internal void ToggleBlack()
    {
        if (State != ContractSlotState.Active)
        {
            throw new InvalidOperationException($"Contract slot {Index + 1} is not active.");
        }

        IsBlack = !IsBlack;
    }

    internal void AddSpeedEffect(string name, double multiplier, double duration)
    {
        _effects.Add(new TimedContractEffect(name, multiplier, 0, duration));
    }

    internal void AddTemporaryWorkers(string name, int workers, double duration)
    {
        _effects.Add(new TimedContractEffect(name, 1d, workers, duration));
    }

    internal void AdvanceRemainingWork(double fraction)
    {
        var remaining = Math.Max(0d, Definition.WorkRequired - Progress);
        Progress = Math.Min(Definition.WorkRequired, Progress + remaining * fraction);
    }

    internal void ReduceCurrentProgress(double fraction)
    {
        Progress = Math.Max(0d, Progress * (1d - fraction));
    }

    internal void CompleteImmediately()
    {
        Progress = Definition.WorkRequired;
    }

    internal void Update(double deltaTime, double workerEfficiency, double blackSpeedMultiplier)
    {
        if (State != ContractSlotState.Active)
        {
            return;
        }

        var speedMultiplier = _effects.Aggregate(1d, (total, effect) => total * effect.SpeedMultiplier);
        var blackMultiplier = IsBlack ? blackSpeedMultiplier : 1d;
        Progress = Math.Min(
            Definition.WorkRequired,
            Progress + EffectiveWorkers * workerEfficiency * speedMultiplier * blackMultiplier * deltaTime);
        DeadlineRemaining = Math.Max(0d, DeadlineRemaining - deltaTime);

        for (var index = _effects.Count - 1; index >= 0; index--)
        {
            var effect = _effects[index];
            effect.Remaining = Math.Max(0d, effect.Remaining - deltaTime);
            if (effect.Remaining <= 0d)
            {
                _effects.RemoveAt(index);
            }
        }
    }

    internal void BeginReplacement(double delay)
    {
        State = ContractSlotState.Replacing;
        AssignedWorkers = 0;
        IsBlack = false;
        ReplacementTimeRemaining = delay;
        _definition = null;
        _effects.Clear();
    }

    internal bool UpdateReplacement(double deltaTime)
    {
        if (State != ContractSlotState.Replacing)
        {
            return false;
        }

        ReplacementTimeRemaining = Math.Max(0d, ReplacementTimeRemaining - deltaTime);
        return ReplacementTimeRemaining <= 0d;
    }

    private sealed class TimedContractEffect(
        string name,
        double speedMultiplier,
        int temporaryWorkers,
        double remaining)
    {
        public string Name { get; } = name;
        public double SpeedMultiplier { get; } = speedMultiplier;
        public int TemporaryWorkers { get; } = temporaryWorkers;
        public double Remaining { get; set; } = remaining;
    }
}
