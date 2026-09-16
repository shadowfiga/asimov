using Graphite.Engine.UI.Theming;
using Graphite.Engine.UI.Audio;
using Graphite.Game.Audio;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI.Settings;

internal sealed class SettingsNavigationButton : Button
{
    private readonly Label _label;
    internal bool Selected
    {
        get; private set;
    }

    internal SettingsNavigationButton(string title)
    {
        var theme = GameThemes.DeepDrive;
        Height = 52;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Padding = new Thickness(theme.Spacing.Md, theme.Spacing.Sm);
        // Match the selected outline instead of inheriting the thinner generic button states.
        OverBackground = new RoundedRectangleBrush(Color.Lerp(theme.ControlSurface, theme.Selection, .16f),
            theme.BorderRadius.Zero, theme.SelectionHighlight, 2);
        FocusedBackground = new RoundedRectangleBrush(Color.Lerp(theme.ControlSurface, theme.Selection, .1f),
            theme.BorderRadius.Zero, theme.SelectionHighlight, 2);
        _label = DialogLayout.Title(title, 22);
        _label.VerticalAlignment = VerticalAlignment.Center;
        Content = _label;
        MouseEntered += (_, _) => RefreshText();
        MouseLeft += (_, _) => RefreshText();
        KeyboardFocusChanged += (_, _) => RefreshText();
        PressedChanged += (_, _) => RefreshText();
        Select(false);
        _ = new UIAudioFeedback(this, GameAudio.Hover, GameAudio.Click);
    }

    internal void Select(bool selected)
    {
        Selected = selected;
        var theme = GameThemes.DeepDrive;
        Background = new RoundedRectangleBrush(
            selected ? Color.Lerp(theme.ControlSurface, theme.Selection, .12f) : theme.DeepBlack,
            theme.BorderRadius.Xs, selected ? theme.SelectionHighlight : theme.Border, selected ? 2 : 1);
        RefreshText();
    }

    private void RefreshText()
    {
        var theme = GameThemes.DeepDrive;
        _label.TextColor = IsPressed ? theme.DeepBlack
            : Selected || IsMouseInside || IsKeyboardFocused ? theme.SelectionHighlight : theme.SecondaryText;
    }
}
