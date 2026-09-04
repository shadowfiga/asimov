using Graphite.Engine.Core;
using Graphite.Engine.Scenes;
using Graphite.Engine.UI;
using Graphite.Game.Scenes;
using Graphite.Game.Scripts;
using Myra.Events;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI;

public sealed class MainMenuScreen : UIScreen
{
    private Button _playButton = null!;
    private Button _quitButton = null!;

    protected override Widget Build()
    {
        _playButton = Button.CreateTextButton("START PROTOTYPE");
        _playButton.Width = 350;
        _quitButton = Button.CreateTextButton("QUIT");
        _quitButton.Width = 350;

        var content = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 10
        };
        content.Widgets.Add(new Label { Text = "BLACK! COMPANY" });
        content.Widgets.Add(new Label { Text = "Take the contract. Throw employees at it. Let Legal deal with the consequences." });
        content.Widgets.Add(new Label { Text = "Prototype slot - progress is not saved" });
        content.Widgets.Add(_playButton);
        content.Widgets.Add(_quitButton);
        return content;
    }

    protected override void Awake()
    {
        _playButton.Click += PlayGame;
        _quitButton.Click += Quit;
    }

    protected override void OnDestroy()
    {
        _playButton.Click -= PlayGame;
        _quitButton.Click -= Quit;
    }

    private static void PlayGame(object sender, MyraEventArgs args)
    {
        GameManager.Instance.StartPrototypeSession();
        SceneManager.Load<ContractBoardScene>();
    }

    private static void Quit(object sender, MyraEventArgs args)
    {
        Application.Quit();
    }
}
