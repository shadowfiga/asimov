using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI.Settings;

internal static class SettingsControls
{
    internal static Grid Row(string title, Widget control)
    {
        var row = new Grid { Height = 54, ColumnSpacing = GameThemes.DeepDrive.Spacing.Md };
        row.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        row.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 280));
        row.RowsProportions.Add(new Proportion(ProportionType.Fill));
        row.Widgets.Add(new Label
        {
            Text = title,
            Font = ThemeAssets.Font(18),
            TextColor = GameThemes.DeepDrive.SecondaryText,
            VerticalAlignment = VerticalAlignment.Center
        });
        control.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(control, 1);
        row.Widgets.Add(control);
        return row;
    }

    internal static Grid Dropdown(MenuAssets assets, string title, IEnumerable<string> options, out ComboView dropdown)
    {
        dropdown = new ComboView("settings-dropdown")
        {
            Height = 42,
            DropdownMaximumHeight = 280,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        foreach (var option in options)
        {
            dropdown.Widgets.Add(Option(option));
        }
        var selector = new Grid { Height = 42 };
        selector.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        selector.RowsProportions.Add(new Proportion(ProportionType.Fill));
        selector.Widgets.Add(dropdown);
        selector.Widgets.Add(new DropdownChevron(assets.Icon("chevron-right", 18, GameThemes.DeepDrive.SecondaryText))
        {
            TransformOrigin = new Vector2(.5f),
            Rotation = 90,
            HorizontalAlignment = HorizontalAlignment.Right,
            Left = -GameThemes.DeepDrive.Spacing.Md
        });
        return Row(title, selector);
    }

    internal static Label Option(string text) => new()
    {
        Text = text,
        Font = ThemeAssets.Font(18),
        TextColor = GameThemes.DeepDrive.PrimaryText,
        DisabledTextColor = GameThemes.DeepDrive.SecondaryText,
        OverTextColor = GameThemes.DeepDrive.SelectionHighlight,
        FocusedTextColor = GameThemes.DeepDrive.SelectionHighlight,
        PressedTextColor = GameThemes.DeepDrive.SelectionHighlight,
        VerticalAlignment = VerticalAlignment.Center
    };

    private sealed class DropdownChevron : Image
    {
        internal DropdownChevron(Image source) => CopyFrom(source);
        public override bool InputFallsThrough(Point localPos) => true;
    }
}

internal sealed class PercentageControl : Grid
{
    private readonly HorizontalProgressBar _meter;
    private readonly Label _percentage;
    internal HorizontalSlider Slider
    {
        get;
    }

    internal PercentageControl(float value, Action<float> changed)
    {
        Height = 24;
        ColumnSpacing = GameThemes.DeepDrive.Spacing.Sm;
        ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 48));
        RowsProportions.Add(new Proportion(ProportionType.Fill));
        var track = new Grid();
        track.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        track.RowsProportions.Add(new Proportion(ProportionType.Fill));
        _meter = new HorizontalProgressBar
        {
            Minimum = 0,
            Maximum = 1,
            Value = value,
            Height = 20,
            VerticalAlignment = VerticalAlignment.Center
        };
        Slider = new HorizontalSlider
        {
            Minimum = 0,
            Maximum = 1,
            Value = value,
            WheelStep = .05f,
            Height = 24,
            VerticalAlignment = VerticalAlignment.Center
        };
        track.Widgets.Add(_meter);
        track.Widgets.Add(Slider);
        Widgets.Add(track);
        _percentage = new Label
        {
            Text = $"{value:P0}",
            Font = ThemeAssets.Font(18),
            TextColor = GameThemes.DeepDrive.PrimaryText,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(_percentage, 1);
        Widgets.Add(_percentage);
        Slider.ValueChanged += (_, _) =>
        {
            _meter.Value = Slider.Value;
            _percentage.Text = $"{Slider.Value:P0}";
            changed(Slider.Value);
        };
    }
}
