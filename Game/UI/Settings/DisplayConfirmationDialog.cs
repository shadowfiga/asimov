using Graphite.Engine.Graphics;
using Graphite.Game.UI.Theming;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI.Settings;

internal sealed class DisplayConfirmationDialog : IDisposable
{
    private readonly Label _countdown;
    internal Dialog Overlay
    {
        get;
    }
    internal MenuButton Keep
    {
        get;
    }
    internal MenuButton Revert
    {
        get;
    }

    internal DisplayConfirmationDialog(MenuAssets assets)
    {
        var content = DialogLayout.Content("KEEP DISPLAY SETTINGS?", 520);
        _countdown = DialogLayout.Title("", 18);
        _countdown.TextColor = GameThemes.DeepDrive.SecondaryText;
        content.Widgets.Add(_countdown);
        var buttons = new HorizontalStackPanel { Spacing = GameThemes.DeepDrive.Spacing.Sm };
        Keep = new MenuButton(assets, "KEEP", textVariant: MenuButtonTextVariant.Compact)
        {
            Width = 200,
            Height = 44
        };
        Revert = new MenuButton(assets, "CANCEL", textVariant: MenuButtonTextVariant.Compact, tone: MenuButtonTone.Danger)
        {
            Width = 200,
            Height = 44
        };
        Keep.Click += (_, _) => DisplaySettings.KeepChanges();
        Revert.Click += (_, _) => DisplaySettings.Revert();
        buttons.Widgets.Add(Keep);
        buttons.Widgets.Add(Revert);
        content.Widgets.Add(buttons);
        Overlay = new Dialog(content) { Visible = false };
    }

    internal void Attach()
    {
        DisplaySettings.Changed += Synchronize;
        Synchronize();
    }

    private void Synchronize()
    {
        Overlay.Visible = DisplaySettings.NeedsConfirmation;
        _countdown.Text = $"REVERTING IN {DisplaySettings.SecondsRemaining}s";
    }

    public void Dispose() => DisplaySettings.Changed -= Synchronize;
}
