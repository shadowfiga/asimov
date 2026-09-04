using System.Globalization;
using Graphite.Game.Content;

namespace Graphite.Game.Scripts;

public sealed class Session
{
    private readonly PrototypeRules _rules = PrototypeRules.Current;
    private readonly List<ContractSlot> _contractSlots = [];
    private readonly List<ManagementCardDefinition> _drawPile = [];
    private readonly List<ManagementCardDefinition> _discardPile = [];
    private readonly List<ManagementCardDefinition> _hand = [];

    private int _hrWorkers;
    private int _legalWorkers;

    public Session(string slotName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotName);
        SlotName = slotName;
        PrepareQuarter();
    }

    public string SlotName { get; }
    public QuarterStatus Status { get; private set; }
    public double TimeRemaining { get; private set; }
    public double Profit { get; private set; }
    public double Bankruptcy { get; private set; }
    public string LastMessage { get; private set; } = string.Empty;
    public int? SelectedSlotIndex { get; private set; }
    public int? SelectedCardIndex { get; private set; }
    public IReadOnlyList<ContractSlot> ContractSlots => _contractSlots;
    public IReadOnlyList<ManagementCardDefinition> Hand => _hand;

    public double ProfitTarget => _rules.ProfitTarget;
    public int TotalWorkforce => _rules.StartingWorkforce;
    public int HrWorkers => _hrWorkers;
    public int LegalWorkers => _legalWorkers;
    public int AssignedContractWorkers => _contractSlots.Sum(slot => slot.AssignedWorkers);
    public int UnassignedWorkers => TotalWorkforce - AssignedContractWorkers - HrWorkers - LegalWorkers;
    public bool TargetReached => Profit >= ProfitTarget;
    public bool IsRunning => Status == QuarterStatus.Running;

    public string OperationalRisk
    {
        get
        {
            var score = _contractSlots
                .Where(slot => slot.State == ContractSlotState.Active)
                .Sum(slot => BaseRiskScore(slot.Definition.BaseRisk) + (slot.IsBlack ? 3d * slot.Definition.BlackRiskMultiplier : 0d));
            score = Math.Max(0d, score - HrWorkers * 0.6d - LegalWorkers * 0.35d);

            if (score >= 10d)
            {
                return "CRITICAL";
            }

            if (score >= 6d)
            {
                return "SEVERE";
            }

            if (score >= 2.5d)
            {
                return "ELEVATED";
            }

            return "LOW";
        }
    }

    public void PrepareQuarter()
    {
        Status = QuarterStatus.Preparing;
        TimeRemaining = _rules.QuarterSeconds;
        Profit = 0d;
        Bankruptcy = 0d;
        _hrWorkers = 0;
        _legalWorkers = 0;
        SelectedSlotIndex = null;
        SelectedCardIndex = null;
        LastMessage = "Review the board, then start the fiscal quarter.";

        _contractSlots.Clear();
        for (var index = 0; index < _rules.ActiveContractSlots; index++)
        {
            _contractSlots.Add(new ContractSlot(index));
        }

        foreach (var slot in _contractSlots)
        {
            FillOffer(slot);
        }

        ResetDeck();
    }

    public void StartQuarter()
    {
        if (Status != QuarterStatus.Preparing)
        {
            return;
        }

        Status = QuarterStatus.Running;
        LastMessage = "Quarter started. Accept a contract and assign workers.";
    }

    public void Update(float deltaTime)
    {
        if (!IsRunning || deltaTime <= 0f)
        {
            return;
        }

        var step = Math.Min(deltaTime, TimeRemaining);
        var blackBankruptcy = _contractSlots
            .Where(slot => slot.State == ContractSlotState.Active && slot.IsBlack)
            .Sum(slot => _rules.BlackBankruptcyPerSecond * slot.Definition.BlackRiskMultiplier);
        var supportRecovery = HrWorkers * 0.035d + LegalWorkers * 0.02d;

        foreach (var slot in _contractSlots.Where(slot => slot.State == ContractSlotState.Replacing))
        {
            if (slot.UpdateReplacement(step))
            {
                FillOffer(slot);
            }
        }

        foreach (var slot in _contractSlots.Where(slot => slot.State == ContractSlotState.Active).ToArray())
        {
            slot.Update(step, _rules.WorkerEfficiency, _rules.BlackSpeedMultiplier);

            if (slot.Progress >= slot.Definition.WorkRequired)
            {
                CompleteContract(slot);
            }
            else if (slot.DeadlineRemaining <= 0d)
            {
                FailContract(slot);
            }
        }

        Bankruptcy = Math.Clamp(Bankruptcy + (blackBankruptcy - supportRecovery) * step, 0d, 100d);
        TimeRemaining = Math.Max(0d, TimeRemaining - step);

        if (Bankruptcy >= 100d)
        {
            Status = QuarterStatus.Bankrupt;
            LastMessage = "BANKRUPT. Responsibility successfully relocated.";
        }
        else if (TimeRemaining <= 0d)
        {
            Status = TargetReached ? QuarterStatus.Succeeded : QuarterStatus.TargetMissed;
            LastMessage = TargetReached
                ? "QUARTER COMPLETE. Shareholders delighted."
                : "TARGET MISSED. Failure remains a future progression opportunity.";
        }
    }

    public bool TryActivateSlot(int slotIndex)
    {
        if (!TryGetSlot(slotIndex, out var slot) || !IsRunning)
        {
            return false;
        }

        if (slot.State == ContractSlotState.Offer)
        {
            slot.Accept();
            SelectedSlotIndex = slotIndex;
            LastMessage = $"Accepted {slot.Definition.Name}. Assign workers before the deadline.";
            return true;
        }

        if (slot.State != ContractSlotState.Active)
        {
            return false;
        }

        SelectedSlotIndex = slotIndex;
        if (SelectedCardIndex is int)
        {
            return TryPlaySelectedCard(slotIndex);
        }

        LastMessage = $"Selected {slot.Definition.Name}.";
        return true;
    }

    public bool TryChangeContractWorkers(int slotIndex, int change)
    {
        if (!TryGetSlot(slotIndex, out var slot) || slot.State != ContractSlotState.Active || !IsRunning || change == 0)
        {
            return false;
        }

        if (change > 0 && UnassignedWorkers < change)
        {
            LastMessage = "No unassigned workers are available.";
            return false;
        }

        if (change < 0 && slot.AssignedWorkers < -change)
        {
            return false;
        }

        slot.ChangeWorkers(change);
        LastMessage = $"{slot.Definition.Name}: {slot.AssignedWorkers} assigned workers.";
        return true;
    }

    public bool TryChangeHrWorkers(int change)
    {
        if (!CanChangeSupportWorkers(change, HrWorkers))
        {
            return false;
        }

        _hrWorkers += change;
        LastMessage = $"HR staffing changed to {HrWorkers}.";
        return true;
    }

    public bool TryChangeLegalWorkers(int change)
    {
        if (!CanChangeSupportWorkers(change, LegalWorkers))
        {
            return false;
        }

        _legalWorkers += change;
        LastMessage = $"Legal staffing changed to {LegalWorkers}.";
        return true;
    }

    public bool TryToggleBlack(int slotIndex)
    {
        if (!TryGetSlot(slotIndex, out var slot) || slot.State != ContractSlotState.Active || !IsRunning)
        {
            return false;
        }

        slot.ToggleBlack();
        SelectedSlotIndex = slotIndex;
        LastMessage = slot.IsBlack
            ? $"{slot.Definition.Name} is now BLACK!"
            : $"{slot.Definition.Name} returned to NORMAL operations.";
        return true;
    }

    public bool TryAbandonContract(int slotIndex)
    {
        if (!TryGetSlot(slotIndex, out var slot) || slot.State != ContractSlotState.Active || !IsRunning)
        {
            return false;
        }

        var name = slot.Definition.Name;
        ChangeBankruptcy(_rules.AbandonBankruptcy);
        BeginReplacement(slot);
        LastMessage = $"Abandoned {name}. Bankruptcy +{_rules.AbandonBankruptcy:0.#}%.";
        return true;
    }

    public bool TrySelectCard(int handIndex)
    {
        if (!IsRunning || handIndex < 0 || handIndex >= _hand.Count)
        {
            return false;
        }

        if (SelectedCardIndex == handIndex)
        {
            SelectedCardIndex = null;
            LastMessage = "Management Card selection cleared.";
            return true;
        }

        SelectedCardIndex = handIndex;
        LastMessage = $"Selected {_hand[handIndex].Name}. Choose an active contract.";
        return true;
    }

    public bool TryPlaySelectedCard(int slotIndex)
    {
        if (SelectedCardIndex is not int handIndex || !TryGetSlot(slotIndex, out var slot) ||
            slot.State != ContractSlotState.Active || !IsRunning)
        {
            return false;
        }

        var card = _hand[handIndex];
        if (!IsCompatible(card, slot.Definition))
        {
            LastMessage = $"{card.Name} is not compatible with {slot.Definition.Name}.";
            return false;
        }

        if (!ApplyCard(card, slot))
        {
            return false;
        }

        ChangeBankruptcy(card.BankruptcyDelta);
        _hand.RemoveAt(handIndex);
        _discardPile.Add(card);
        DrawCard();
        SelectedCardIndex = null;
        SelectedSlotIndex = slotIndex;

        if (slot.Progress >= slot.Definition.WorkRequired)
        {
            CompleteContract(slot);
        }
        else
        {
            LastMessage = $"{card.Name} attached to {slot.Definition.Name}.";
        }

        if (Bankruptcy >= 100d)
        {
            Status = QuarterStatus.Bankrupt;
            LastMessage = "BANKRUPT. The card was technically very effective.";
        }

        return true;
    }

    private bool ApplyCard(ManagementCardDefinition card, ContractSlot slot)
    {
        switch (card.Effect)
        {
            case ManagementCardEffect.SpeedMultiplier:
                slot.AddSpeedEffect(card.Name, card.Magnitude, card.DurationSeconds);
                return true;
            case ManagementCardEffect.TemporaryWorkers:
                slot.AddTemporaryWorkers(card.Name, (int)card.Magnitude, card.DurationSeconds);
                return true;
            case ManagementCardEffect.RemainingProgress:
                slot.AdvanceRemainingWork(card.Magnitude);
                return true;
            case ManagementCardEffect.QualityControl:
                slot.ReduceCurrentProgress(card.Magnitude);
                return true;
            case ManagementCardEffect.ShipIt:
                if (slot.ProgressRatio < card.Magnitude)
                {
                    LastMessage = $"{card.Name} requires at least {card.Magnitude * 100d:0}% progress.";
                    return false;
                }

                slot.CompleteImmediately();
                return true;
            default:
                throw new InvalidOperationException($"Unsupported Management Card effect: {card.Effect}.");
        }
    }

    private void CompleteContract(ContractSlot slot)
    {
        var definition = slot.Definition;
        Profit += definition.Payout;
        BeginReplacement(slot);
        LastMessage = TargetReached
            ? $"{definition.Name} completed for {FormatMoney(definition.Payout)}. Target reached - keep earning excess Profit."
            : $"{definition.Name} completed for {FormatMoney(definition.Payout)}.";
    }

    private void FailContract(ContractSlot slot)
    {
        var definition = slot.Definition;
        ChangeBankruptcy(definition.FailureBankruptcy);
        BeginReplacement(slot);
        LastMessage = $"{definition.Name} failed. Bankruptcy +{definition.FailureBankruptcy:0.#}%.";
    }

    private void BeginReplacement(ContractSlot slot)
    {
        if (SelectedSlotIndex == slot.Index)
        {
            SelectedSlotIndex = null;
        }

        slot.BeginReplacement(_rules.ReplacementDelaySeconds);
    }

    private bool CanChangeSupportWorkers(int change, int current)
    {
        if (!IsRunning || change == 0)
        {
            return false;
        }

        if (change > 0 && UnassignedWorkers < change)
        {
            LastMessage = "No unassigned workers are available.";
            return false;
        }

        return change >= 0 || current >= -change;
    }

    private void FillOffer(ContractSlot slot)
    {
        var offeredIds = _contractSlots
            .Where(existing => existing != slot && existing.HasDefinition)
            .Select(existing => existing.Definition.Id)
            .ToHashSet();
        var candidates = ContractDefinition.All.Where(definition => !offeredIds.Contains(definition.Id)).ToArray();
        if (candidates.Length == 0)
        {
            candidates = ContractDefinition.All.ToArray();
        }

        slot.SetOffer(candidates[Random.Shared.Next(candidates.Length)]);
    }

    private void ResetDeck()
    {
        _drawPile.Clear();
        _discardPile.Clear();
        _hand.Clear();
        _drawPile.AddRange(ManagementCardDefinition.All);
        Shuffle(_drawPile);

        while (_hand.Count < _rules.HandSize)
        {
            DrawCard();
        }
    }

    private void DrawCard()
    {
        if (_drawPile.Count == 0)
        {
            _drawPile.AddRange(_discardPile);
            _discardPile.Clear();
            Shuffle(_drawPile);
        }

        if (_drawPile.Count == 0)
        {
            return;
        }

        var lastIndex = _drawPile.Count - 1;
        _hand.Add(_drawPile[lastIndex]);
        _drawPile.RemoveAt(lastIndex);
    }

    private void ChangeBankruptcy(double amount)
    {
        Bankruptcy = Math.Clamp(Bankruptcy + amount, 0d, 100d);
    }

    private bool TryGetSlot(int slotIndex, out ContractSlot slot)
    {
        if (slotIndex < 0 || slotIndex >= _contractSlots.Count)
        {
            slot = null!;
            return false;
        }

        slot = _contractSlots[slotIndex];
        return true;
    }

    private static bool IsCompatible(ManagementCardDefinition card, ContractDefinition contract)
        => card.CompatibleTags.Count == 0 || card.CompatibleTags.Intersect(contract.Tags).Any();

    private static string FormatMoney(double amount)
        => "$" + amount.ToString("N0", CultureInfo.InvariantCulture);

    private static double BaseRiskScore(string risk) => risk switch
    {
        "LOW" => 0.5d,
        "ELEVATED" => 1.5d,
        "HIGH" => 2.5d,
        "CRITICAL" => 4d,
        _ => throw new InvalidDataException($"Chisel exported unsupported contract risk '{risk}'.")
    };

    private static void Shuffle<T>(IList<T> items)
    {
        for (var index = items.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (items[index], items[swapIndex]) = (items[swapIndex], items[index]);
        }
    }
}
