using Graphite.Engine.Scenes;
using Graphite.Game.UI;

namespace Graphite.Game.Scenes;

public sealed class CounterScene : Scene
{
    protected internal override void OnLoad()
    {
        UI.Open<CounterScreen>();
    }
}
