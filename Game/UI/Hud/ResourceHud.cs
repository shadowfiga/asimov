using System.Globalization;
using Graphite.Engine.UI.Theming;
using Graphite.Game.Domain.Run;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI.Hud;

/// <summary>Read-only, event-bound Ore balance. Does not intercept gameplay input.</summary>
internal sealed class ResourceHud : Grid, IDisposable
{
    private readonly Run _run;
    private readonly MenuAssets _assets = new();
    internal Label OreAmount
    {
        get;
    }

    internal ResourceHud(Run run)
    {
        _run = run;
        var theme = GameThemes.DeepDrive;
        var tokens = theme.ResourceHud;
        Width = tokens.Width;
        Padding = new Thickness(theme.Spacing.Md);
        ColumnSpacing = theme.Spacing.Md;
        Background = new RoundedRectangleBrush(new Color(theme.DeepBlack, tokens.SurfaceOpacity),
            theme.BorderRadius.Zero, theme.Border, 1);
        ColumnsProportions.Add(new Proportion(ProportionType.Pixels, tokens.IconSize));
        ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        RowsProportions.Add(Proportion.Auto);
        var image = _assets.Icon("gem", tokens.IconSize, theme.PrimaryText);
        image.OverRenderable = image.FocusedRenderable = image.PressedRenderable = image.Renderable;
        Widgets.Add(image);
        var text = new VerticalStackPanel { Spacing = theme.Spacing.Xs };
        text.Widgets.Add(Text("ORE", tokens.LabelFontSize, theme.SecondaryText));
        OreAmount = Text("0", tokens.ValueFontSize, theme.PrimaryText);
        text.Widgets.Add(OreAmount);
        Grid.SetColumn(text, 1);
        Widgets.Add(text);
        _run.OreChanged += Refresh;
        Refresh();
    }

    private static Label Text(string text, int size, Color color) => new()
    {
        Text = text,
        Font = ThemeAssets.Font(size),
        TextColor = color,
        OverTextColor = color,
        FocusedTextColor = color,
        PressedTextColor = color,
        HorizontalAlignment = HorizontalAlignment.Left
    };

    private void Refresh()
    {
        OreAmount.Text = _run.Ore.ToString("N0", CultureInfo.InvariantCulture);
    }

    public override bool InputFallsThrough(Point localPos) => true;

    public void Dispose()
    {
        _run.OreChanged -= Refresh;
        _assets.Dispose();
    }
}
