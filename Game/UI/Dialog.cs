using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Graphite.Game.UI;

/// <summary>A modal fullscreen scrim that centers one child.</summary>
internal sealed class Dialog : Panel
{
    internal Widget Content
    {
        get;
    }

    internal Dialog(Widget content)
        : base(new Stylesheet { PanelStyle = new WidgetStyle() }, null)
    {
        if (content.Parent is not null || content.Desktop is not null)
        {
            throw new InvalidOperationException("Add a dialog around a child before placing that child in the UI.");
        }

        Content = content;
        var scrim = new SolidBrush(GameThemes.DeepDrive.DialogScrim);
        Background = DisabledBackground = OverBackground = FocusedBackground = PressedBackground = scrim;
        Border = DisabledBorder = OverBorder = FocusedBorder = PressedBorder = null;
        BorderThickness = new Thickness(0);
        // The shared layout backend fits the child's declared size inside this safe area.
        Padding = new Thickness(GameThemes.DeepDrive.Spacing.Xl);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        AcceptsKeyboardFocus = true;

        content.HorizontalAlignment = HorizontalAlignment.Center;
        content.VerticalAlignment = VerticalAlignment.Center;
        Widgets.Add(content);
    }

    public override bool InputFallsThrough(Point localPos) => false;

    public override void OnVisibleChanged()
    {
        base.OnVisibleChanged();
        if (Visible && Desktop is not null)
        {
            SetKeyboardFocus();
        }
    }
}
