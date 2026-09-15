using Graphite.Game.UI.Settings;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Graphite.UI.Tests;

internal static class ProgressSurfaceChecks
{
    public static void Run(GraphicsDevice device, string output)
    {
        var theme = GameThemes.DeepDrive;
        var styles = Stylesheet.Current;
        foreach (var style in new WidgetStyle[] { styles.HorizontalProgressBarStyle, styles.VerticalProgressBarStyle,
            styles.HorizontalSliderStyle, styles.HorizontalSliderStyle.KnobStyle })
        {
            Program.Check(new[] { style.Border, style.OverBorder, style.FocusedBorder, style.PressedBorder, style.DisabledBorder }
                .All(border => border is null) && style.BorderThickness == new Thickness(0) && style.Padding == new Thickness(0),
                "Progress tracks, input layers, and handles have no border or inset in any state");
            Program.Check(new[] { style.Background, style.OverBackground, style.FocusedBackground,
                style.PressedBackground, style.DisabledBackground }.All(brush => brush is null or SolidBrush),
                "No progress or slider state can reintroduce an outlined surface");
        }

        var control = new PercentageControl(0, _ => { })
        {
            Width = 280,
            Left = 10,
            Top = 10,
            TransformOrigin = Vector2.Zero
        };
        var meter = control.GetChildren(true).OfType<HorizontalProgressBar>().Single();
        using var target = new RenderTarget2D(device, 840, 120);
        using var context = new RenderContext { Opacity = 1 };
        var pixels = new Color[target.Width * target.Height];
        foreach (var scale in new[] { .6f, 1f, 2.8f })
        {
            control.Scale = new Vector2(scale);
            foreach (var value in new[] { 0f, .5f, 1f })
            {
                control.Slider.Value = value;
                control.Measure(target.Bounds.Size);
                control.Arrange(target.Bounds);
                device.SetRenderTarget(target);
                device.Clear(theme.RaisedSurface);
                context.Begin();
                context.Scissor = target.Bounds;
                control.Render(context);
                context.End();
                device.SetRenderTarget(null);
                target.GetData(pixels);

                var start = meter.ToGlobal(Vector2.Zero);
                var end = meter.ToGlobal(new Vector2(meter.Bounds.Width, meter.Bounds.Height));
                foreach (var fraction in new[] { .25f, .75f })
                {
                    var x = (int)(start.X + (end.X - start.X) * fraction);
                    var expected = fraction < value ? theme.Selection : theme.DeepBlack;
                    foreach (var y in new[] { (int)MathF.Ceiling(start.Y), (int)((start.Y + end.Y) / 2), (int)MathF.Floor(end.Y) - 1 })
                    {
                        Program.Check(pixels[y * target.Width + x] == expected,
                            "Progress is solid palette orange over a dark track, including top/bottom edges at every scale");
                    }
                    Program.Check(pixels[((int)MathF.Ceiling(start.Y) - 2) * target.Width + x] == theme.RaisedSurface
                        && pixels[((int)MathF.Ceiling(end.Y) + 1) * target.Width + x] == theme.RaisedSurface,
                        "The transparent slider layer draws no second outline outside the track");
                }
                if (scale == 2.8f && value == .5f)
                {
                    using var file = File.Create(Path.Combine(output, "progress-borderless.png"));
                    target.SaveAsPng(file, target.Width, target.Height);
                }
            }
        }
    }
}
