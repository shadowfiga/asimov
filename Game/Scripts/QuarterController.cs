using Chisel.Generated;
using Graphite.Engine.Core;
using Graphite.Engine.Scenes;
using Graphite.Game.Scenes;
using Graphite.Game.UI;

namespace Graphite.Game.Scripts;

public sealed class QuarterController : Behaviour
{
    private readonly ChiselInput _input = new();

    public ContractBoardScreen Screen { get; set; } = null!;

    protected internal override void Update(float dt)
    {
        _input.Update();

        if (_input.IsActionJustPressed(ChiselInputBindingsId.CANCEL))
        {
            SceneManager.Load<MainMenuScene>();
            return;
        }

        GameManager.Instance.CurrentSession.Update(dt);
        HandleInput();

        if (Screen.IsOpen)
        {
            Screen.Refresh();
        }
    }

    private void HandleInput()
    {
        if (_input.IsActionJustPressed(ChiselInputBindingsId.CONFIRM))
        {
            Screen.StartOrRetry();
        }

        if (_input.IsActionJustPressed(ChiselInputBindingsId.CONTRACT_1))
        {
            Screen.ActivateSlot(0);
        }

        if (_input.IsActionJustPressed(ChiselInputBindingsId.CONTRACT_2))
        {
            Screen.ActivateSlot(1);
        }

        if (_input.IsActionJustPressed(ChiselInputBindingsId.CONTRACT_3))
        {
            Screen.ActivateSlot(2);
        }

        if (_input.IsActionJustPressed(ChiselInputBindingsId.CARD_1))
        {
            Screen.SelectCard(0);
        }

        if (_input.IsActionJustPressed(ChiselInputBindingsId.CARD_2))
        {
            Screen.SelectCard(1);
        }

        if (_input.IsActionJustPressed(ChiselInputBindingsId.CARD_3))
        {
            Screen.SelectCard(2);
        }

        if (_input.IsActionJustPressed(ChiselInputBindingsId.CARD_4))
        {
            Screen.SelectCard(3);
        }

        if (_input.IsActionJustPressed(ChiselInputBindingsId.TOGGLE_BLACK))
        {
            Screen.ToggleBlackOnSelectedSlot();
        }

        if (_input.IsActionJustPressed(ChiselInputBindingsId.ABANDON))
        {
            Screen.AbandonSelectedSlot();
        }
    }
}
