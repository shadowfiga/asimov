using Graphite.Engine.Core;
using Graphite.Engine.Scenes;
using Graphite.Engine.UI;
using Graphite.Engine.UI.Controls;
using Graphite.Game.Scenes;

namespace Graphite.Game.UI;

public sealed class MainMenuScreen : UIScreen
{
    [UIElement] private Button _playButton = null!;
    [UIElement] private Button _quitButton = null!;

    protected override void Awake()
    {
        _playButton.Clicked += PlayGame;
        _quitButton.Clicked += Quit;
    }

    protected override void OnDestroy()
    {
        _playButton.Clicked -= PlayGame;
        _quitButton.Clicked -= Quit;
    }

    private static void PlayGame()
    {
        SceneManager.Load<CounterScene>();
    }

    private static void Quit()
    {
        Application.Quit();
    }
}
