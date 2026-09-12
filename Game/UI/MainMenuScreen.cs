using Graphite.Game.Configuration;
using System.Diagnostics;
using Graphite.Engine.Core;
using Graphite.Engine.Scenes;
using Graphite.Engine.UI;
using Graphite.Game.Scenes;
using Microsoft.Xna.Framework;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI;

public sealed class MainMenuScreen : UIScreen
{
    private Button _playButton = null!;
    private Button _settingsButton = null!;
    private Button _quitButton = null!;
    private Button _creditsButton = null!;
    private Button _discordButton = null!;
    private Button _backButton = null!;
    private VerticalStackPanel _menu = null!;
    private HorizontalStackPanel _footer = null!;
    private VerticalStackPanel _credits = null!;
    private MenuSettings _menuContent = null!;
    private bool _starting;

    protected override Widget Build()
    {
        _menuContent = GameSettings.Instance.Menu;
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
        _menu = content;

        _creditsButton = Button.CreateTextButton("CREDITS");
        _creditsButton.Width = 130;
        _creditsButton.Height = 40;
        _creditsButton.Enabled = _menuContent.Credits.Count > 0;
        _discordButton = Button.CreateTextButton("DISCORD");
        _discordButton.Width = 130;
        _discordButton.Height = 40;
        _discordButton.Enabled = _menuContent.DiscordUrl != null;
        _footer = new HorizontalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(24),
            Spacing = 12
        };
        _footer.Widgets.Add(_creditsButton);
        _footer.Widgets.Add(_discordButton);

        _credits = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 18,
            Visible = false
        };
        _credits.Widgets.Add(new Label { Text = "CREDITS", HorizontalAlignment = HorizontalAlignment.Center });
        foreach (var entry in _menuContent.Credits)
        {
            _credits.Widgets.Add(new Label { Text = entry, HorizontalAlignment = HorizontalAlignment.Center });
        }
        _backButton = Button.CreateTextButton("BACK");
        _backButton.Height = 48;
        _credits.Widgets.Add(_backButton);

        var root = new Panel();
        root.Widgets.Add(_menu);
        root.Widgets.Add(_footer);
        root.Widgets.Add(_credits);
        return root;
    }

    protected override void Awake()
    {
        _playButton.Click += PlayClicked;
        _quitButton.Click += Quit;
        _creditsButton.Click += ShowCredits;
        _discordButton.Click += OpenDiscord;
        _backButton.Click += BackClicked;
    }

    protected override void OnDestroy()
    {
        _playButton.Click -= PlayClicked;
        _quitButton.Click -= Quit;
        _creditsButton.Click -= ShowCredits;
        _discordButton.Click -= OpenDiscord;
        _backButton.Click -= BackClicked;
    }

    public void PlayGame()
    {
        if (_starting || _credits.Visible)
        {
            return;
        }
        _starting = true;
        SceneManager.Load<AftergreenScene>();
    }

    private void PlayClicked(object sender, MyraEventArgs args) => PlayGame();

    private void ShowCredits(object sender, MyraEventArgs args)
    {
        _menu.Visible = _footer.Visible = false;
        _credits.Visible = true;
    }

    public void BackToMenu()
    {
        _credits.Visible = false;
        _menu.Visible = _footer.Visible = true;
    }

    private void BackClicked(object sender, MyraEventArgs args) => BackToMenu();

    private void OpenDiscord(object sender, MyraEventArgs args)
    {
        if (_menuContent.DiscordUrl == null)
        {
            return;
        }
        try
        {
            using var process = Process.Start(new ProcessStartInfo(_menuContent.DiscordUrl.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Console.Error.WriteLine($"Could not open Discord: {exception.Message}");
        }
    }

    private static void Quit(object sender, MyraEventArgs args) => Application.Quit();
}
