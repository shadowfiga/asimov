using Graphite.Engine.Configuration;
using Graphite.Engine.Core;
using Graphite.Engine.UI;
using Graphite.Engine.UI.Theming;
using Graphite.Game.Configuration;
using Graphite.Game.UI;
using Graphite.Game.UI.Materials;
using Graphite.Game.UI.Settings;
using Graphite.Game.UI.Theming;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Ui = Graphite.Engine.UI.UI;

namespace Graphite.Game.Scenes;

public sealed class MainMenuUI : UIScreen
{
    private const float ButtonScale = .8f;
    private readonly MenuAssets _assets = new();
    private readonly UIScope _dialogs = new();
    private readonly List<MenuButton> _rows = [];
    private MenuButton _settingsButton = null!;
    private MenuButton _creditsButton = null!;
    private MenuButton _quitButton = null!;
    private UIMaterialHost _menu = null!;
    private MenuTitle _title = null!;
    private Label? _staging;
    private CreditsDialog? _credits;
    internal SettingsDialog? Settings
    {
        get; private set;
    }
    internal MenuButton SettingsButton => _settingsButton;

    protected override Widget Build()
    {
        try
        {
            return BuildMenu();
        }
        catch
        {
            _assets.Dispose();
            throw;
        }
    }

    private Widget BuildMenu()
    {
        var theme = GameThemes.DeepDrive;
        var spacing = theme.Spacing;
        _title = new MenuTitle();
        var brand = new VerticalStackPanel { Spacing = spacing.Sm };
        brand.Widgets.Add(new Label
        {
            Text = "K-01 INDUSTRIES // FIELD TERMINAL",
            Font = ThemeAssets.Font(15),
            TextColor = theme.Selection,
            HorizontalAlignment = HorizontalAlignment.Left
        });
        brand.Widgets.Add(_title);
        brand.Widgets.Add(new Label
        {
            Text = "RESOURCES TODAY. A BRIGHTER TOMORROW.",
            StyleName = "secondary",
            Font = ThemeAssets.Font(16)
        });
        var buttons = new VerticalStackPanel { Spacing = spacing.Sm };
        AddRow(buttons, "CONTINUE", "play", enabled: false, primary: true, arrow: false);
        AddRow(buttons, "NEW OPERATION", "play", enabled: false);
        AddRow(buttons, "LOAD OPERATION", "folder-open", enabled: false);
        _settingsButton = AddRow(buttons, "SETTINGS", "settings");
        _creditsButton = AddRow(buttons, "CREDITS", "users", enabled: GameSettings.Instance.Menu.Credits.Count > 0);
        _quitButton = AddRow(buttons, "QUIT", "log-out");
        var content = new VerticalStackPanel
        {
            Spacing = spacing.Xl,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Widgets.Add(brand);
        content.Widgets.Add(buttons);
        _menu = new UIMaterialHost(new ScrollViewer
        {
            Content = content,
            ShowHorizontalScrollBar = false,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch
        }, interactions: MenuPresentation.FadeStyle);
        var root = new Panel(styleName: "root") { Background = _assets };
        root.Widgets.Add(_menu);
        if (GameSettings.Instance.Environment == SettingsEnvironment.Staging)
        {
            _staging = new Label
            {
                Text = "STAGING",
                Font = ThemeAssets.Font(14),
                TextColor = theme.DeepBlack,
                Background = new RoundedRectangleBrush(theme.Selection, theme.BorderRadius.Xs),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Padding = new Thickness(spacing.Sm, spacing.Xs)
            };
            root.Widgets.Add(_staging);
        }
        Resize();
        return new UIMaterialHost(root, interactions: MenuPresentation.FadeStyle);
    }

    private MenuButton AddRow(VerticalStackPanel buttons, string text, string icon,
        bool enabled = true, bool primary = false, bool arrow = true)
    {
        var button = new MenuButton(_assets, text, icon, primary, arrow) { Enabled = enabled };
        _rows.Add(button);
        buttons.Widgets.Add(button);
        return button;
    }

    private void Resize()
    {
        var spacing = GameThemes.DeepDrive.Spacing;
        var edge = spacing.Xl * 2;
        var availableWidth = Math.Max(1, Ui.LayoutSize.X - edge * 2);
        var width = Math.Min(520, availableWidth);
        _menu.Width = width + edge;
        _menu.Margin = new Thickness(edge, spacing.Xl, 0, spacing.Xl);
        _title.Fit(width, 48);
        foreach (var button in _rows)
        {
            button.Width = Math.Min((int)(520 * ButtonScale), availableWidth);
            button.Height = (int)(68 * ButtonScale);
            button.Resize(ButtonScale);
        }
        if (_staging is not null)
        {
            _staging.Margin = new Thickness(edge, spacing.Lg);
        }
    }

    protected override void Awake()
    {
        _settingsButton.Click += ShowSettings;
        _creditsButton.Click += ShowCredits;
        _quitButton.Click += Quit;
        Ui.LayoutChanged += Resize;
    }

    protected override void OnDestroy()
    {
        Ui.LayoutChanged -= Resize;
        _settingsButton.Click -= ShowSettings;
        _creditsButton.Click -= ShowCredits;
        _quitButton.Click -= Quit;
        _dialogs.CloseAll();
        _assets.Dispose();
    }

    private void ShowSettings(object sender, MyraEventArgs args)
    {
        if (!IsClosing && Settings?.IsOpen != true)
        {
            Settings = _dialogs.Open<SettingsDialog>();
        }
    }

    private void ShowCredits(object sender, MyraEventArgs args)
    {
        if (!IsClosing && _credits?.IsOpen != true)
        {
            _credits = _dialogs.Open<CreditsDialog>();
        }
    }

    public void BackToMenu()
    {
        if (IsClosing)
        {
            return;
        }
        if (Settings?.IsOpen == true)
        {
            Settings.Back();
        }
        else if (_credits?.IsOpen == true)
        {
            Ui.Close(_credits);
        }
    }

    private static void Quit(object sender, MyraEventArgs args) => Application.Quit();
}
