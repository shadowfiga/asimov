using Graphite.Engine.Core;
using Graphite.Engine.Scenes;
using Graphite.Engine.UI;
using Graphite.Game.Scenes;
using Microsoft.Xna.Framework;
using Myra.Events;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI;

public sealed class MainMenuScreen : UIScreen
{
    private Button _playButton = null!;
    private Button _settingsButton = null!;
    private Button _quitButton = null!;
    private bool _starting;

    protected override Widget Build()
    {
        _playButton = Button.CreateTextButton("NEW GAME / CONTINUE");
        _playButton.Width = 350;
        _playButton.Height = 48;
        _settingsButton = Button.CreateTextButton("Settings");
        _settingsButton.Width = 350;
        _settingsButton.Height = 48;
        _quitButton = Button.CreateTextButton("QUIT");
        _quitButton.Width = 350;
        _quitButton.Height = 48;

        var content = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 18
        };
        content.Widgets.Add(new Label
        {
            Text = "AFTERGREEN",
            TextColor = new Color(172, 218, 135),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        content.Widgets.Add(_playButton);
        content.Widgets.Add(_settingsButton);
        content.Widgets.Add(_quitButton);
        return content;
    }

    protected override void Awake()
    {
        _playButton.Click += PlayClicked;
        _quitButton.Click += Quit;
    }

    protected override void OnDestroy()
    {
        _playButton.Click -= PlayClicked;
        _quitButton.Click -= Quit;
    }

    public void PlayGame()
    {
        if (_starting)
        {
            return;
        }
        _starting = true;
        SceneManager.Load<AftergreenScene>();
    }

    private void PlayClicked(object sender, MyraEventArgs args) => PlayGame();

    private static void Quit(object sender, MyraEventArgs args) => Application.Quit();
}
