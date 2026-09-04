using Graphite.Engine.Scenes;
using Graphite.Game.Scripts;
using Graphite.Game.UI;

namespace Graphite.Game.Scenes;

public sealed class ContractBoardScene : Scene
{
    protected internal override void OnLoad()
    {
        if (!GameManager.Instance.HasCurrentSession)
        {
            GameManager.Instance.StartPrototypeSession();
        }

        var screen = UI.Open<ContractBoardScreen>();
        var controller = Create("Quarter Controller").Add<QuarterController>();
        controller.Screen = screen;
    }
}
