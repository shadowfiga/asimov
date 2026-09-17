using FontStashSharp;
using Graphite.Engine.Persistence;
using Graphite.Engine.UI;
using Graphite.Game.Scenes;
using Graphite.Game.Configuration;
using Graphite.Game.UI;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Ui = Graphite.Engine.UI.UI;

namespace Graphite.UI.Tests;

internal static class DeclarativeLayoutChecks
{
    public static void Run()
    {
        using var assets = new MenuAssets();
        var theme = GameThemes.DeepDrive;
        var title = new MenuTitle();
        Program.Check(title.Text == "GIRLS & MINING"
            && new StagingSettings().Game.Name == title.Text && new ProductionSettings().Game.Name == title.Text,
            "Menu and both environment window titles use the shared game display name");
        Program.Check(title.Font.MeasureString(title.Text).X <= theme.Layout.MenuWidth,
            "The renamed title fits the themed menu width");
        Program.Check(new StagingSettings().Game.Id == "deep-drive" && new ProductionSettings().Game.Id == "deep-drive",
            "The display-name change preserves existing settings and save locations");
        var button = new MenuButton(assets, "SETTINGS", "settings", size: MenuButtonSize.Menu);
        var frame = new Panel { Width = 900, Height = 500 };
        frame.Widgets.Add(button);
        using var material = new UIMaterialHost(frame);
        var dialog = new Graphite.Game.UI.Dialog(material);
        var root = new Panel(styleName: "root");
        root.Widgets.Add(dialog);
        foreach (var bounds in new[] { new Point(1400, 900), new Point(400, 300), new Point(1000, 700) })
        {
            root.Measure(bounds);
            root.Arrange(new Rectangle(Point.Zero, bounds));
            var expected = new Point(Math.Min(900, bounds.X - theme.Spacing.Xl * 2),
                Math.Min(500, bounds.Y - theme.Spacing.Xl * 2));
            Program.Check(frame.Bounds.Size == expected && material.Width == 900 && material.Height == 500,
                "A material-wrapped dialog fits a changing parent without mutating its preferred dimensions");
            var center = frame.ToGlobal(new Vector2(frame.Bounds.Width / 2f, frame.Bounds.Height / 2f));
            Program.Check(Math.Abs(center.X - bounds.X / 2f) <= 1 && Math.Abs(center.Y - bounds.Y / 2f) <= 1,
                "Default dialog layout centers content without a resize callback");
            Program.Check(button.Bounds.Width == Math.Min(theme.MenuButton.Menu.Width, expected.X)
                && button.Height == theme.MenuButton.Menu.Height,
                "Buttons shrink to parent constraints and regain their themed width when space returns");
        }

        var grid = new AdaptiveGrid();
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 200));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        grid.RowsProportions.Add(new Proportion(ProportionType.Fill));
        grid.ColumnRules.Add(new UIColumnRule(0, 200, 120, 600));
        var sidebar = new Panel(styleName: "root");
        grid.Widgets.Add(sidebar);
        foreach (var width in new[] { 900, 400, 900 })
        {
            grid.Measure(new Point(width, 200));
            grid.Arrange(new Rectangle(0, 0, width, 200));
            Program.Check(sidebar.Bounds.Width == (width < 600 ? 120 : 200),
                "Grid breakpoints use available parent width, independent of the viewport or screen class");
        }
        grid.ColumnRules[0] = new UIColumnRule(0, 220, 140, 600);
        grid.Measure(new Point(900, 200));
        grid.Arrange(new Rectangle(0, 0, 900, 200));
        Program.Check(sidebar.Bounds.Width == 220, "Changing a declarative column rule invalidates layout automatically");

        var originalScale = Preferences.Get(RuntimePreferences.UiScale);
        var observer = Ui.Open<LayoutObserverScreen>();
        try
        {
            Program.Check(observer.LayoutNotifications == 1, "Specialized layout overrides initialize automatically on open");
            using var credits = Ui.Open<CreditsDialog>();
            foreach (var scale in new[] { .75f, 1.75f, 1f })
            {
                Preferences.Set(RuntimePreferences.UiScale, scale);
                Ui.Update(0);
                var size = credits.Frame.Bounds.Size;
                Program.Check(size.X == Math.Min(theme.Layout.CreditsSize.X, Ui.LayoutSize.X - theme.Spacing.Xl * 2)
                    && size.Y == Math.Min(theme.Layout.CreditsSize.Y, Ui.LayoutSize.Y - theme.Spacing.Xl * 2),
                    "Credits uses the same default layout as Settings with no resize handler");
                Program.Near(((DynamicSpriteFont)observer.Title.Font).FontSystem.FontResolutionFactor,
                    Math.Max(1, MathF.Ceiling(Ui.Scale)), "Distributed text participates in global font-density updates");
            }
            Program.Check(observer.LayoutNotifications == 4, "The engine invokes optional overrides once per layout change");
            observer.Dispose();
            Preferences.Set(RuntimePreferences.UiScale, 1.25f);
            Ui.Update(0);
            Program.Check(observer.LayoutNotifications == 4, "Closed screens receive no layout callbacks");
        }
        finally
        {
            observer.Dispose();
            Preferences.Set(RuntimePreferences.UiScale, originalScale);
            Ui.Update(0);
        }
    }
}

internal sealed class LayoutObserverScreen : UIScreen
{
    internal DistributedLabel Title { get; private set; } = null!;
    internal int LayoutNotifications
    {
        get; private set;
    }

    protected override Widget Build()
    {
        Title = new DistributedLabel { Text = "TEST", Font = ThemeAssets.Font(24), Width = 300 };
        return Title;
    }

    protected override void OnLayoutChanged()
    {
        LayoutNotifications++;
    }
}
