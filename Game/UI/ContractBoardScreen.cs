using System.Globalization;
using Graphite.Engine.Scenes;
using Graphite.Engine.UI;
using Graphite.Game.Scenes;
using Graphite.Game.Scripts;
using Myra.Events;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI;

public sealed class ContractBoardScreen : UIScreen
{
    private Label _headlineLabel = null!;
    private Label _riskLabel = null!;
    private Label _workforceLabel = null!;
    private Label _cardPromptLabel = null!;
    private Label _messageLabel = null!;

    private Label[] _slotInfoLabels = null!;
    private Label[] _slotWorkerLabels = null!;
    private Label[] _slotPrimaryLabels = null!;
    private Label[] _slotBlackLabels = null!;
    private HorizontalProgressBar[] _slotProgressBars = null!;
    private Button[] _slotPrimaryButtons = null!;
    private Button[] _slotMinusButtons = null!;
    private Button[] _slotPlusButtons = null!;
    private Button[] _slotBlackButtons = null!;
    private Button[] _slotAbandonButtons = null!;

    private Label[] _cardLabels = null!;
    private Button[] _cardButtons = null!;

    private Button _hrMinusButton = null!;
    private Button _hrPlusButton = null!;
    private Button _legalMinusButton = null!;
    private Button _legalPlusButton = null!;
    private Button _startButton = null!;
    private Button _retryButton = null!;
    private Button _mainMenuButton = null!;

    protected override Widget Build()
    {
        _headlineLabel = new Label();
        _riskLabel = new Label();
        _workforceLabel = new Label();
        _cardPromptLabel = new Label();
        _messageLabel = new Label { Width = 920, Wrap = true };

        CreateSlotControls();
        CreateCardControls();
        CreateSupportControls();

        _startButton = Button.CreateTextButton("START QUARTER");
        _retryButton = Button.CreateTextButton("RESET QUARTER");
        _mainMenuButton = Button.CreateTextButton("MAIN MENU");
        SetButtonWidth(180, _startButton, _retryButton, _mainMenuButton);

        var content = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 930,
            Spacing = 4
        };
        content.Widgets.Add(new Label { Text = "BLACK! COMPANY - CONTRACT DESK" });
        content.Widgets.Add(_headlineLabel);
        content.Widgets.Add(_riskLabel);
        content.Widgets.Add(BuildSupportRow());
        content.Widgets.Add(_workforceLabel);
        content.Widgets.Add(BuildContractBoard());
        content.Widgets.Add(_cardPromptLabel);
        content.Widgets.Add(BuildCardRow());
        content.Widgets.Add(_messageLabel);
        content.Widgets.Add(BuildActionRow());
        content.Widgets.Add(new Label { Text = "Keys: 1-3 contracts | F1-F4 cards | B BLACK! | X abandon | Esc menu" });

        Refresh();
        return content;
    }

    protected override void Awake()
    {
        _slotPrimaryButtons[0].Click += ActivateFirstSlot;
        _slotPrimaryButtons[1].Click += ActivateSecondSlot;
        _slotPrimaryButtons[2].Click += ActivateThirdSlot;
        _slotMinusButtons[0].Click += RemoveWorkerFromFirstSlot;
        _slotMinusButtons[1].Click += RemoveWorkerFromSecondSlot;
        _slotMinusButtons[2].Click += RemoveWorkerFromThirdSlot;
        _slotPlusButtons[0].Click += AddWorkerToFirstSlot;
        _slotPlusButtons[1].Click += AddWorkerToSecondSlot;
        _slotPlusButtons[2].Click += AddWorkerToThirdSlot;
        _slotBlackButtons[0].Click += ToggleFirstSlotBlack;
        _slotBlackButtons[1].Click += ToggleSecondSlotBlack;
        _slotBlackButtons[2].Click += ToggleThirdSlotBlack;
        _slotAbandonButtons[0].Click += AbandonFirstSlot;
        _slotAbandonButtons[1].Click += AbandonSecondSlot;
        _slotAbandonButtons[2].Click += AbandonThirdSlot;
        _cardButtons[0].Click += SelectFirstCard;
        _cardButtons[1].Click += SelectSecondCard;
        _cardButtons[2].Click += SelectThirdCard;
        _cardButtons[3].Click += SelectFourthCard;
        _hrMinusButton.Click += RemoveHrWorker;
        _hrPlusButton.Click += AddHrWorker;
        _legalMinusButton.Click += RemoveLegalWorker;
        _legalPlusButton.Click += AddLegalWorker;
        _startButton.Click += StartQuarter;
        _retryButton.Click += RetryQuarter;
        _mainMenuButton.Click += OpenMainMenu;
    }

    protected override void OnDestroy()
    {
        _slotPrimaryButtons[0].Click -= ActivateFirstSlot;
        _slotPrimaryButtons[1].Click -= ActivateSecondSlot;
        _slotPrimaryButtons[2].Click -= ActivateThirdSlot;
        _slotMinusButtons[0].Click -= RemoveWorkerFromFirstSlot;
        _slotMinusButtons[1].Click -= RemoveWorkerFromSecondSlot;
        _slotMinusButtons[2].Click -= RemoveWorkerFromThirdSlot;
        _slotPlusButtons[0].Click -= AddWorkerToFirstSlot;
        _slotPlusButtons[1].Click -= AddWorkerToSecondSlot;
        _slotPlusButtons[2].Click -= AddWorkerToThirdSlot;
        _slotBlackButtons[0].Click -= ToggleFirstSlotBlack;
        _slotBlackButtons[1].Click -= ToggleSecondSlotBlack;
        _slotBlackButtons[2].Click -= ToggleThirdSlotBlack;
        _slotAbandonButtons[0].Click -= AbandonFirstSlot;
        _slotAbandonButtons[1].Click -= AbandonSecondSlot;
        _slotAbandonButtons[2].Click -= AbandonThirdSlot;
        _cardButtons[0].Click -= SelectFirstCard;
        _cardButtons[1].Click -= SelectSecondCard;
        _cardButtons[2].Click -= SelectThirdCard;
        _cardButtons[3].Click -= SelectFourthCard;
        _hrMinusButton.Click -= RemoveHrWorker;
        _hrPlusButton.Click -= AddHrWorker;
        _legalMinusButton.Click -= RemoveLegalWorker;
        _legalPlusButton.Click -= AddLegalWorker;
        _startButton.Click -= StartQuarter;
        _retryButton.Click -= RetryQuarter;
        _mainMenuButton.Click -= OpenMainMenu;
    }

    public void Refresh()
    {
        var session = CurrentSession;
        _headlineLabel.Text =
            $"TIME {FormatTime(session.TimeRemaining)}    PROFIT {FormatMoney(session.Profit)} / {FormatMoney(session.ProfitTarget)}    BANKRUPTCY {session.Bankruptcy:0.0}%";
        _riskLabel.Text =
            $"Quarter: {StatusText(session.Status)} | Target: {(session.TargetReached ? "REACHED - excess Profit counts" : "NOT REACHED")} | Operational Risk: {session.OperationalRisk}";
        _workforceLabel.Text =
            $"WORKFORCE {session.TotalWorkforce} | Unassigned {session.UnassignedWorkers} | Contracts {session.AssignedContractWorkers} | HR {session.HrWorkers} | Legal {session.LegalWorkers}";
        _cardPromptLabel.Text = session.SelectedCardIndex is int selectedCard
            ? $"SELECTED CARD: {session.Hand[selectedCard].Name} - choose an active contract"
            : "MANAGEMENT HAND - select a card, then choose an active contract";
        _messageLabel.Text = session.LastMessage;

        for (var index = 0; index < _slotInfoLabels.Length; index++)
        {
            RefreshSlot(index, session.ContractSlots[index], session);
        }

        RefreshCards(session);
        RefreshSupportButtons(session);
        _startButton.Visible = session.Status == QuarterStatus.Preparing;
        _retryButton.Visible = IsTerminal(session.Status);
    }

    public void ActivateSlot(int slotIndex)
    {
        CurrentSession.TryActivateSlot(slotIndex);
        Refresh();
    }

    public void SelectCard(int handIndex)
    {
        CurrentSession.TrySelectCard(handIndex);
        Refresh();
    }

    public void ToggleBlackOnSelectedSlot()
    {
        if (CurrentSession.SelectedSlotIndex is int slotIndex)
        {
            CurrentSession.TryToggleBlack(slotIndex);
            Refresh();
        }
    }

    public void AbandonSelectedSlot()
    {
        if (CurrentSession.SelectedSlotIndex is int slotIndex)
        {
            CurrentSession.TryAbandonContract(slotIndex);
            Refresh();
        }
    }

    public void StartOrRetry()
    {
        if (CurrentSession.Status == QuarterStatus.Preparing)
        {
            CurrentSession.StartQuarter();
        }
        else if (IsTerminal(CurrentSession.Status))
        {
            CurrentSession.PrepareQuarter();
        }

        Refresh();
    }

    private void CreateSlotControls()
    {
        _slotInfoLabels = [CreateWrappingLabel(286), CreateWrappingLabel(286), CreateWrappingLabel(286)];
        _slotWorkerLabels = [new Label(), new Label(), new Label()];
        _slotPrimaryLabels = [new Label(), new Label(), new Label()];
        _slotBlackLabels = [new Label(), new Label(), new Label()];
        _slotProgressBars = [CreateProgressBar(), CreateProgressBar(), CreateProgressBar()];
        _slotPrimaryButtons =
        [
            CreateLabeledButton(_slotPrimaryLabels[0], 275),
            CreateLabeledButton(_slotPrimaryLabels[1], 275),
            CreateLabeledButton(_slotPrimaryLabels[2], 275)
        ];
        _slotMinusButtons = [Button.CreateTextButton("-"), Button.CreateTextButton("-"), Button.CreateTextButton("-")];
        _slotPlusButtons = [Button.CreateTextButton("+"), Button.CreateTextButton("+"), Button.CreateTextButton("+")];
        _slotBlackButtons =
        [
            CreateLabeledButton(_slotBlackLabels[0], 128),
            CreateLabeledButton(_slotBlackLabels[1], 128),
            CreateLabeledButton(_slotBlackLabels[2], 128)
        ];
        _slotAbandonButtons =
        [
            Button.CreateTextButton("ABANDON"),
            Button.CreateTextButton("ABANDON"),
            Button.CreateTextButton("ABANDON")
        ];
        SetButtonWidth(42, _slotMinusButtons.Concat(_slotPlusButtons).ToArray());
        SetButtonWidth(128, _slotAbandonButtons);
    }

    private void CreateCardControls()
    {
        _cardLabels = [new Label(), new Label(), new Label(), new Label()];
        _cardButtons = _cardLabels.Select(label => CreateLabeledButton(label, 220, 62)).ToArray();
        foreach (var label in _cardLabels)
        {
            label.Wrap = true;
            label.Width = 210;
        }
    }

    private void CreateSupportControls()
    {
        _hrMinusButton = Button.CreateTextButton("-");
        _hrPlusButton = Button.CreateTextButton("+");
        _legalMinusButton = Button.CreateTextButton("-");
        _legalPlusButton = Button.CreateTextButton("+");
        SetButtonWidth(42, _hrMinusButton, _hrPlusButton, _legalMinusButton, _legalPlusButton);
    }

    private HorizontalStackPanel BuildSupportRow()
    {
        var row = CreateRow();
        row.Widgets.Add(new Label { Text = "HR" });
        row.Widgets.Add(_hrMinusButton);
        row.Widgets.Add(_hrPlusButton);
        row.Widgets.Add(new Label { Text = "     LEGAL" });
        row.Widgets.Add(_legalMinusButton);
        row.Widgets.Add(_legalPlusButton);
        return row;
    }

    private HorizontalStackPanel BuildContractBoard()
    {
        var row = CreateRow();
        for (var index = 0; index < _slotInfoLabels.Length; index++)
        {
            row.Widgets.Add(BuildContractSlot(index));
        }

        return row;
    }

    private VerticalStackPanel BuildContractSlot(int index)
    {
        var workerRow = CreateRow();
        workerRow.Widgets.Add(_slotWorkerLabels[index]);
        workerRow.Widgets.Add(_slotMinusButtons[index]);
        workerRow.Widgets.Add(_slotPlusButtons[index]);

        var riskRow = CreateRow();
        riskRow.Widgets.Add(_slotBlackButtons[index]);
        riskRow.Widgets.Add(_slotAbandonButtons[index]);

        var panel = new VerticalStackPanel { Width = 296, Spacing = 3 };
        panel.Widgets.Add(new Label { Text = $"CONTRACT SLOT {index + 1}" });
        panel.Widgets.Add(_slotInfoLabels[index]);
        panel.Widgets.Add(_slotProgressBars[index]);
        panel.Widgets.Add(workerRow);
        panel.Widgets.Add(_slotPrimaryButtons[index]);
        panel.Widgets.Add(riskRow);
        return panel;
    }

    private HorizontalStackPanel BuildCardRow()
    {
        var row = CreateRow();
        foreach (var button in _cardButtons)
        {
            row.Widgets.Add(button);
        }

        return row;
    }

    private HorizontalStackPanel BuildActionRow()
    {
        var row = CreateRow();
        row.Widgets.Add(_startButton);
        row.Widgets.Add(_retryButton);
        row.Widgets.Add(_mainMenuButton);
        return row;
    }

    private void RefreshSlot(int index, ContractSlot slot, Session session)
    {
        var isSelected = session.SelectedSlotIndex == index;
        if (slot.State == ContractSlotState.Replacing)
        {
            _slotInfoLabels[index].Text = $"REPLACING...\nNew opportunity in {slot.ReplacementTimeRemaining:0.0}s";
            _slotProgressBars[index].Value = 0f;
            _slotWorkerLabels[index].Text = "Workers: 0";
            _slotPrimaryLabels[index].Text = "WAITING";
            SetSlotButtonStates(index, false, false, false, false);
            return;
        }

        var definition = slot.Definition;
        var tags = string.Join(" / ", definition.Tags);
        if (slot.State == ContractSlotState.Offer)
        {
            _slotInfoLabels[index].Text =
                $"OFFER: {definition.Name}\n{FormatMoney(definition.Payout)} | {definition.DeadlineSeconds:0}s | {definition.BaseRisk}\n{tags}\n{definition.SpecialModifier}";
            _slotProgressBars[index].Value = 0f;
            _slotWorkerLabels[index].Text = "Workers: 0";
            _slotPrimaryLabels[index].Text = $"ACCEPT [{index + 1}]";
            _slotBlackLabels[index].Text = "BLACK!";
            SetSlotButtonStates(index, session.IsRunning, false, false, false);
            return;
        }

        var selectionMark = isSelected ? "SELECTED - " : string.Empty;
        _slotInfoLabels[index].Text =
            $"{selectionMark}{definition.Name}\n{FormatMoney(definition.Payout)} | {slot.DeadlineRemaining:0.0}s | {definition.BaseRisk}\nWork {slot.Progress:0.0}/{definition.WorkRequired:0.0}\nEffects: {slot.ActiveEffects}";
        _slotProgressBars[index].Value = (float)(slot.ProgressRatio * 100d);
        _slotWorkerLabels[index].Text = slot.TemporaryWorkers > 0
            ? $"Workers: {slot.AssignedWorkers} + {slot.TemporaryWorkers} temp"
            : $"Workers: {slot.AssignedWorkers}";
        _slotPrimaryLabels[index].Text = session.SelectedCardIndex is int
            ? $"PLAY CARD HERE [{index + 1}]"
            : $"SELECT [{index + 1}]";
        _slotBlackLabels[index].Text = slot.IsBlack ? "NORMAL [B]" : "BLACK! [B]";
        SetSlotButtonStates(
            index,
            session.IsRunning,
            session.IsRunning && slot.AssignedWorkers > 0,
            session.IsRunning && session.UnassignedWorkers > 0,
            session.IsRunning);
    }

    private void SetSlotButtonStates(int index, bool primary, bool minus, bool plus, bool contractActions)
    {
        _slotPrimaryButtons[index].Enabled = primary;
        _slotMinusButtons[index].Enabled = minus;
        _slotPlusButtons[index].Enabled = plus;
        _slotBlackButtons[index].Enabled = contractActions;
        _slotAbandonButtons[index].Enabled = contractActions;
    }

    private void RefreshCards(Session session)
    {
        for (var index = 0; index < _cardButtons.Length; index++)
        {
            var hasCard = index < session.Hand.Count;
            _cardButtons[index].Visible = hasCard;
            _cardButtons[index].Enabled = hasCard && session.IsRunning;
            if (hasCard)
            {
                var card = session.Hand[index];
                var selected = session.SelectedCardIndex == index ? "[SELECTED] " : string.Empty;
                _cardLabels[index].Text = $"{selected}{card.Name} [F{index + 1}]\n{card.Description}";
            }
        }
    }

    private void RefreshSupportButtons(Session session)
    {
        _hrMinusButton.Enabled = session.IsRunning && session.HrWorkers > 0;
        _hrPlusButton.Enabled = session.IsRunning && session.UnassignedWorkers > 0;
        _legalMinusButton.Enabled = session.IsRunning && session.LegalWorkers > 0;
        _legalPlusButton.Enabled = session.IsRunning && session.UnassignedWorkers > 0;
    }

    private static Label CreateWrappingLabel(int width)
        => new()
        {
            Width = width,
            Height = 92,
            Wrap = true
        };

    private static HorizontalProgressBar CreateProgressBar()
        => new()
        {
            Minimum = 0f,
            Maximum = 100f,
            Value = 0f,
            Width = 275,
            Height = 10
        };

    private static Button CreateLabeledButton(Label label, int width, int? height = null)
    {
        var button = new Button
        {
            Content = label,
            Width = width
        };
        if (height is int value)
        {
            button.Height = value;
        }

        return button;
    }

    private static HorizontalStackPanel CreateRow()
        => new()
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 5
        };

    private static void SetButtonWidth(int width, params Button[] buttons)
    {
        foreach (var button in buttons)
        {
            button.Width = width;
        }
    }

    private static string FormatMoney(double amount)
        => "$" + amount.ToString("N0", CultureInfo.InvariantCulture);

    private static string FormatTime(double seconds)
    {
        var wholeSeconds = Math.Max(0, (int)Math.Ceiling(seconds));
        return $"{wholeSeconds / 60:00}:{wholeSeconds % 60:00}";
    }

    private static string StatusText(QuarterStatus status) => status switch
    {
        QuarterStatus.Preparing => "PREPARING",
        QuarterStatus.Running => "RUNNING",
        QuarterStatus.Succeeded => "SUCCESS",
        QuarterStatus.TargetMissed => "TARGET MISSED",
        QuarterStatus.Bankrupt => "BANKRUPT",
        _ => throw new InvalidOperationException($"Unknown quarter status: {status}.")
    };

    private static bool IsTerminal(QuarterStatus status)
        => status is QuarterStatus.Succeeded or QuarterStatus.TargetMissed or QuarterStatus.Bankrupt;

    private static Session CurrentSession => GameManager.Instance.CurrentSession;

    private void ActivateFirstSlot(object sender, MyraEventArgs args) => ActivateSlot(0);
    private void ActivateSecondSlot(object sender, MyraEventArgs args) => ActivateSlot(1);
    private void ActivateThirdSlot(object sender, MyraEventArgs args) => ActivateSlot(2);
    private void RemoveWorkerFromFirstSlot(object sender, MyraEventArgs args) => ChangeSlotWorkers(0, -1);
    private void RemoveWorkerFromSecondSlot(object sender, MyraEventArgs args) => ChangeSlotWorkers(1, -1);
    private void RemoveWorkerFromThirdSlot(object sender, MyraEventArgs args) => ChangeSlotWorkers(2, -1);
    private void AddWorkerToFirstSlot(object sender, MyraEventArgs args) => ChangeSlotWorkers(0, 1);
    private void AddWorkerToSecondSlot(object sender, MyraEventArgs args) => ChangeSlotWorkers(1, 1);
    private void AddWorkerToThirdSlot(object sender, MyraEventArgs args) => ChangeSlotWorkers(2, 1);
    private void ToggleFirstSlotBlack(object sender, MyraEventArgs args) => ToggleBlack(0);
    private void ToggleSecondSlotBlack(object sender, MyraEventArgs args) => ToggleBlack(1);
    private void ToggleThirdSlotBlack(object sender, MyraEventArgs args) => ToggleBlack(2);
    private void AbandonFirstSlot(object sender, MyraEventArgs args) => AbandonSlot(0);
    private void AbandonSecondSlot(object sender, MyraEventArgs args) => AbandonSlot(1);
    private void AbandonThirdSlot(object sender, MyraEventArgs args) => AbandonSlot(2);
    private void SelectFirstCard(object sender, MyraEventArgs args) => SelectCard(0);
    private void SelectSecondCard(object sender, MyraEventArgs args) => SelectCard(1);
    private void SelectThirdCard(object sender, MyraEventArgs args) => SelectCard(2);
    private void SelectFourthCard(object sender, MyraEventArgs args) => SelectCard(3);

    private void ChangeSlotWorkers(int slotIndex, int change)
    {
        CurrentSession.TryChangeContractWorkers(slotIndex, change);
        Refresh();
    }

    private void ToggleBlack(int slotIndex)
    {
        CurrentSession.TryToggleBlack(slotIndex);
        Refresh();
    }

    private void AbandonSlot(int slotIndex)
    {
        CurrentSession.TryAbandonContract(slotIndex);
        Refresh();
    }

    private void RemoveHrWorker(object sender, MyraEventArgs args)
    {
        CurrentSession.TryChangeHrWorkers(-1);
        Refresh();
    }

    private void AddHrWorker(object sender, MyraEventArgs args)
    {
        CurrentSession.TryChangeHrWorkers(1);
        Refresh();
    }

    private void RemoveLegalWorker(object sender, MyraEventArgs args)
    {
        CurrentSession.TryChangeLegalWorkers(-1);
        Refresh();
    }

    private void AddLegalWorker(object sender, MyraEventArgs args)
    {
        CurrentSession.TryChangeLegalWorkers(1);
        Refresh();
    }

    private void StartQuarter(object sender, MyraEventArgs args) => StartOrRetry();
    private void RetryQuarter(object sender, MyraEventArgs args) => StartOrRetry();

    private static void OpenMainMenu(object sender, MyraEventArgs args)
    {
        SceneManager.Load<MainMenuScene>();
    }
}
