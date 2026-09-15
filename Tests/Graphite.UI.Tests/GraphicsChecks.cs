using Graphite.Engine.UI;
using FontStashSharp;
using Graphite.Engine.Audio;
using Graphite.Engine.Graphics;
using Graphite.Engine.Persistence;
using Graphite.Game;
using Graphite.Game.Configuration;
using Graphite.Game.Graphics;
using Graphite.Engine.Scenes;
using Graphite.Engine.UI.Animation;
using Graphite.Engine.UI.Audio;
using Graphite.Engine.UI.Materials;
using Graphite.Engine.UI.Theming;
using Graphite.Game.UI;
using Graphite.Game.UI.Settings;
using Graphite.Game.Scenes;
using Graphite.Game.UI.Materials;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Ui = Graphite.Engine.UI.UI;

namespace Graphite.UI.Tests;

internal sealed class GraphicsChecks : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private MainMenuUI _menu = null!;
    private FixtureScreen _fixture = null!;
    private MouseInfo _mouse = new();
    private int _frame;
    private int _allocations;
    private int _postProcessAllocations;
    private Vector2 _hitPosition;
    private Task<UIPlaybackState>? _hide;
    private readonly string _output = Path.GetFullPath(".artifacts/ui-checks");

    public GraphicsChecks()
    {
        _graphics = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = 1280, PreferredBackBufferHeight = 720 };
        IsFixedTimeStep = false;
    }
    protected override void Initialize()
    {
        base.Initialize();
        Ui.Initialize(this);
        PostProcessing.Initialize(GraphicsDevice);
        EnvironmentOverlay.Initialize(GraphicsDevice, Graphite.Engine.Configuration.SettingsEnvironment.Staging);
        Preferences.Initialize(Path.Combine(_output, "preferences"));
        Preferences.Remove(RuntimePreferences.UiScale);
        Preferences.Remove(RuntimePreferences.Display);
        DisplaySettings.Initialize(this, _graphics, new Graphite.Engine.Configuration.WindowSettings
        {
            Width = 1280,
            Height = 720,
            Resizable = true,
            Fullscreen = false,
            Borderless = false
        });
        Preferences.Set(PlayerPreferences.CrtIntensity, 1f);
        Startup.Initialize();
        MyraEnvironment.MouseInfoGetter = () => _mouse;
        Directory.CreateDirectory(_output);
        EnvironmentOverlayChecks.Run(GraphicsDevice, _output);
        FontRenderingChecks();
        NativeAudioCheck();
        CrtShaderChecks();
        FullscreenPostProcessChecks();
        RoundedSurfaceChecks();
        SquareSurfaceChecks();
        ProgressSurfaceChecks.Run(GraphicsDevice, _output);
        MenuButtonThemeChecks();
        DialogChecks();
        ConfirmationDialogChecks.Run();
        DeclarativeLayoutChecks.Run();
        GlobalScaleChecks();
        SettingsBorderChecks();
        DisplayChecks();
        SettingsDialogChecks();
        SceneManager.Load<BootstrapScene>();
        SceneManager.CommitPendingChanges();
        Program.Check(Ui.HostCount > 0, "Bootstrap opens the main menu");
        SceneManager.Update(0);
        SceneManager.Shutdown();
        Program.Check(Ui.HostCount == 0, "Scene teardown releases the menu's material hosts");
        var hostCount = Ui.HostCount;
        try
        {
            Ui.Open<FailingScreen>();
            throw new InvalidOperationException("Expected build failure");
        }
        catch (NotSupportedException)
        {
            Program.Check(Ui.HostCount == hostCount, "Failed screen build releases hosts");
        }
        _menu = Ui.Open<MainMenuUI>();
    }
    private void FontRenderingChecks()
    {
        var logical = ThemeAssets.Font(18);
        var font = (DynamicSpriteFont)ThemeAssets.ResolveFont(logical, 2.8f);
        var small = (DynamicSpriteFont)ThemeAssets.ResolveFont(font, .6f);
        Program.Near(font.FontSystem.FontResolutionFactor, 3, "1440p max-scale text uses a 3x glyph atlas");
        Program.Near(small.FontSystem.FontResolutionFactor, 1, "Small text returns to a 1x atlas to avoid undersampling");
        Program.Check(font.FontSize == logical.FontSize && font.LineHeight == logical.LineHeight,
            "Resolution changes preserve logical font size and line height");
        var styleFont = (DynamicSpriteFont)ThemeAssets.ResolveFont(Myra.Graphics2D.UI.Styles.Stylesheet.Current.LabelStyle.Font, 2.8f);
        Program.Near(styleFont.FontSystem.FontResolutionFactor, 3, "Default style fonts participate in resolution selection");
        Program.Check(ReferenceEquals(font, ThemeAssets.ResolveFont(logical, 2.7f)), "Nearby viewport scales share a font atlas");
        var atlasCount = ThemeAssets.ResolutionFontCount;
        foreach (var scale in new[] { .6f, 2.8f, 2.1f, .8f, 2.8f })
        {
            ThemeAssets.ResolveFont(logical, scale);
        }
        Program.Check(ThemeAssets.ResolutionFontCount == atlasCount, "Repeated scaling reuses font resources");
        using var original = new FontSystem(new FontSystemSettings { FontResolutionFactor = 1, KernelWidth = 0, KernelHeight = 0 });
        original.AddFont(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Abel", "Abel-Regular.ttf")));
        var baseline = original.GetFont(18);
        using var batch = new SpriteBatch(GraphicsDevice);
        using var target = new RenderTarget2D(GraphicsDevice, 1160, 255);
        GraphicsDevice.SetRenderTarget(target);
        GraphicsDevice.Clear(GameThemes.DeepDrive.RaisedSurface);
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        baseline.DrawText(batch, "SETTINGS  ACCESSIBILITY  UI SCALE  1.75×", new Vector2(20, 20),
            GameThemes.DeepDrive.PrimaryText, scale: new Vector2(2.8f));
        font.DrawText(batch, "SETTINGS  ACCESSIBILITY  UI SCALE  1.75×", new Vector2(20, 100),
            GameThemes.DeepDrive.PrimaryText, scale: new Vector2(2.8f));
        var row = 190;
        foreach (var smallFont in new[] { baseline, small })
        {
            smallFont.DrawText(batch, "SETTINGS  ACCESSIBILITY  UI SCALE  0.75×", new Vector2(20, row),
                GameThemes.DeepDrive.PrimaryText, scale: new Vector2(.6f));
            row += 30;
        }
        batch.End();
        GraphicsDevice.SetRenderTarget(null);
        using var output = File.Create(Path.Combine(_output, "font-resolution-comparison.png"));
        target.SaveAsPng(output, target.Width, target.Height);
        var pixels = new Color[target.Width * target.Height];
        target.GetData(pixels);
        Program.Check(SoftPixelRatio(100) < SoftPixelRatio(20) * .8f, "High-scale glyphs have fewer soft edge pixels than enlarged 1x glyphs");
        Program.Check(pixels.AsSpan(190 * target.Width, 25 * target.Width).SequenceEqual(pixels.AsSpan(220 * target.Width, 25 * target.Width)),
            "Small text is pixel-identical to the original 1x rasterization");
        float SoftPixelRatio(int top)
        {
            var region = pixels.Skip(top * target.Width).Take(60 * target.Width).Where(pixel => pixel.R > 25).ToArray();
            return region.Count(pixel => pixel.R < 220) / (float)region.Length;
        }
    }

    private void NativeAudioCheck()
    {
        var savedMaster = Preferences.Get(PlayerPreferences.MasterVolume);
        Preferences.Set("audio.muted", true);
        PlayerPreferences.ApplyAudio();
        Program.Check(Preferences.Get(PlayerPreferences.MasterVolume) == 0 && SoundEffect.MasterVolume == 0 && MediaPlayer.Volume == 0,
            "Legacy mute migrates to a visible zero master level");
        Preferences.Initialize(Path.Combine(_output, "preferences"));
        Program.Check(!Preferences.Remove<bool>("audio.muted"), "Legacy mute key is retired on disk");
        Preferences.Set(PlayerPreferences.MasterVolume, savedMaster);
        PlayerPreferences.ApplyAudio();
        var path = Path.Combine(_output, "silent-fixture.wav");
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            const int bytes = 2205 * 2;
            writer.Write("RIFF"u8);
            writer.Write(36 + bytes);
            writer.Write("WAVEfmt "u8);
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(22050);
            writer.Write(44100);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write("data"u8);
            writer.Write(bytes);
            writer.Write(new byte[bytes]);
        }
        using (var audio = new UIAudioService { Muted = true })
        {
            using var voice = audio.Play(new UISoundCue { Asset = path, Loop = true, Volume = .3f, Pitch = .1f, Pan = -.2f });
            Program.Check(voice.IsPlaying, "Native audio fixture starts");
            audio.Volume = .2f;
            audio.Update();
            voice.Stop();
            audio.Update();
            Program.Check(!voice.IsPlaying, "Native audio fixture stops and cleans up");
        }
        File.Delete(path);
    }
    private void CrtShaderChecks()
    {
        using var source = new RenderTarget2D(GraphicsDevice, 128, 128, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        using var destination = new RenderTarget2D(GraphicsDevice, 128, 128);
        using var batch = new SpriteBatch(GraphicsDevice);
        using var pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData([GameThemes.DeepDrive.PrimaryText]);
        GraphicsDevice.SetRenderTarget(source);
        GraphicsDevice.Clear(Color.Transparent);
        batch.Begin();
        batch.Draw(pixel, new Rectangle(40, 40, 48, 48), Color.White);
        batch.End();
        var parameters = new ScreenFilterParameters();
        using var crt = CrtPresentation.Aged.CreateInstance(GraphicsDevice);
        Program.Check(!crt.IsActive(parameters), "CRT is inactive by default");
        CrtFilter.Configure(parameters, 1f);
        Program.Check(crt.IsActive(parameters), "Aged CRT activates at non-zero intensity");
        Program.Near(parameters.Get(CrtFilter.Scanlines), .1f, "Aged CRT restores its original scanline strength");
        Program.Near(parameters.Get(CrtFilter.Noise), .02f, "Aged CRT restores its original grain strength");
        Program.Near(parameters.Get(CrtFilter.Vignette), .28f, "Aged CRT restores its original vignette strength");
        Program.Near(parameters.Get(CrtFilter.Bloom), .2f, "Aged CRT restores its original bloom strength");
        parameters.Set(CrtFilter.Noise, 0);
        parameters.Set(CrtFilter.Vignette, 0);
        parameters.Set(CrtFilter.Bloom, 0);
        crt.Render(new ScreenFilterContext(GraphicsDevice, batch, source, destination, parameters, .2f));
        GraphicsDevice.SetRenderTarget(null);
        var colors = new Color[128 * 128];
        destination.GetData(colors);
        Program.Check(colors[0].A == 0 && colors[64 * 128 + 64].A == 255, "CRT preserves transparency");
        Program.Check(Enumerable.Range(40, 48).Any(y => colors[y * 128 + 64] != GameThemes.DeepDrive.PrimaryText),
            "CRT changes fullscreen content");
        var rowDelta = Enumerable.Range(60, 7)
            .Max(y => Vector3.Distance(colors[y * 128 + 64].ToVector3(), colors[(y + 1) * 128 + 64].ToVector3()));
        var columnDelta = Enumerable.Range(60, 7)
            .Max(x => Vector3.Distance(colors[64 * 128 + x].ToVector3(), colors[64 * 128 + x + 1].ToVector3()));
        Program.Check(rowDelta > columnDelta * 4,
            "CRT scanlines vary by screen-space row and cannot render vertically");
        var repeatDelta = Enumerable.Range(50, 24)
            .Max(y => Vector3.Distance(colors[y * 128 + 64].ToVector3(), colors[(y + 2) * 128 + 64].ToVector3()));
        Program.Check(repeatDelta < .001f, "Original CRT scanlines repeat every two physical rows");

        // Restore and verify the original two-dimensional, twelve-hertz grain.
        GraphicsDevice.SetRenderTarget(source);
        GraphicsDevice.Clear(GameThemes.DeepDrive.PrimaryText);
        GraphicsDevice.SetRenderTarget(null);
        CrtFilter.Configure(parameters, 0f);
        parameters.Set(CrtFilter.Noise, .02f);
        crt.Render(new ScreenFilterContext(GraphicsDevice, batch, source, destination, parameters, .02f));
        GraphicsDevice.SetRenderTarget(null);
        var firstGrain = new Color[128 * 128];
        destination.GetData(firstGrain);
        crt.Render(new ScreenFilterContext(GraphicsDevice, batch, source, destination, parameters, .2f));
        GraphicsDevice.SetRenderTarget(null);
        destination.GetData(colors);
        Program.Check(firstGrain.Where((color, index) => color != colors[index]).Any(),
            "Original CRT grain refreshes over time");
        var horizontalGrain = 0f;
        var verticalGrain = 0f;
        for (var y = 32; y < 96; y++)
        {
            for (var x = 32; x < 96; x++)
            {
                var current = colors[y * 128 + x].ToVector3();
                horizontalGrain += Vector3.Distance(current, colors[y * 128 + x + 1].ToVector3());
                verticalGrain += Vector3.Distance(current, colors[(y + 1) * 128 + x].ToVector3());
            }
        }
        Program.Check(horizontalGrain > 0 && verticalGrain > 0,
            "Original CRT grain varies across both screen dimensions");

        // Restore and verify the old radial falloff rather than a UI-bound mask.
        CrtFilter.Configure(parameters, 0f);
        parameters.Set(CrtFilter.Vignette, .28f);
        crt.Render(new ScreenFilterContext(GraphicsDevice, batch, source, destination, parameters, .2f));
        GraphicsDevice.SetRenderTarget(null);
        destination.GetData(colors);
        var center = colors[64 * 128 + 64].ToVector3().Length();
        var side = colors[64 * 128 + 4].ToVector3().Length();
        var corner = colors[4 * 128 + 4].ToVector3().Length();
        Program.Check(center > side && side > corner, "Original CRT radial vignette darkens the screen edges");

        CrtFilter.Configure(parameters, 0f);
        Program.Check(!crt.IsActive(parameters), "Zero intensity disables CRT processing");
    }

    private void FullscreenPostProcessChecks()
    {
        var presentation = GraphicsDevice.PresentationParameters;
        var width = presentation.BackBufferWidth;
        var height = presentation.BackBufferHeight;
        var time = new GameTime(TimeSpan.FromSeconds(.2), TimeSpan.FromSeconds(.2));
        var probeColor = new Color(176, 184, 188);
        using var batch = new SpriteBatch(GraphicsDevice);
        using var pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData([Color.White]);

        void DrawProbe(GameTime _)
        {
            batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp);
            try
            {
                batch.Draw(pixel, new Rectangle(0, 0, width, height), probeColor);
            }
            finally
            {
                batch.End();
            }
        }

        CrtFilter.Configure(PostProcessing.Parameters, 0);
        Program.Check(!PostProcessing.IsActive, "Zero intensity disables global CRT processing");
        Program.Check(PostProcessing.AllocatedTargets == 0, "Inactive global CRT allocates no frame targets");
        PostProcessing.Render(time, Color.Black, DrawProbe);
        var unprocessed = new Color[width * height];
        GraphicsDevice.GetBackBufferData(unprocessed);
        Program.Check(unprocessed.All(color => color == probeColor),
            "Inactive global CRT leaves non-UI frame content unchanged");

        CrtFilter.Configure(PostProcessing.Parameters, 1);
        Program.Check(PostProcessing.IsActive, "Non-zero intensity activates global CRT processing");
        var usage = presentation.RenderTargetUsage;
        PostProcessing.Render(time, Color.Black, DrawProbe);
        Program.Check(GraphicsDevice.GetRenderTargets().Length == 0,
            "Global CRT restores the back buffer after composition");
        Program.Check(GraphicsDevice.PresentationParameters.RenderTargetUsage == usage,
            "Global CRT preserves the back buffer usage policy");
        Program.Check(PostProcessing.AllocatedTargets == 2,
            "Global CRT owns one source and one destination frame target");
        Program.Check(PostProcessing.TargetSize == new Point(width, height),
            "Global CRT targets cover the complete back buffer");
        Program.Check(PostProcessing.SourceDepthFormat == presentation.DepthStencilFormat
            && PostProcessing.SourceMultiSampleCount == presentation.MultiSampleCount,
            "Global CRT capture preserves gameplay depth and multisampling");

        var processed = new Color[width * height];
        GraphicsDevice.GetBackBufferData(processed);
        var left = new Rectangle(8, 48, 48, 48);
        var right = new Rectangle(width - 56, 48, 48, 48);
        Program.Check(RegionChanged(unprocessed, processed, width, left),
            "Global CRT reaches non-UI content at the left screen edge");
        Program.Check(RegionChanged(unprocessed, processed, width, right),
            "Global CRT reaches non-UI content at the right screen edge");

        using var previousTarget = new RenderTarget2D(GraphicsDevice, 96, 64, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        GraphicsDevice.SetRenderTarget(previousTarget);
        var previousViewport = GraphicsDevice.Viewport;
        PostProcessing.Render(time, Color.Black, DrawProbe);
        var targets = GraphicsDevice.GetRenderTargets();
        Program.Check(targets.Length == 1 && ReferenceEquals(targets[0].RenderTarget, previousTarget)
            && GraphicsDevice.Viewport.Equals(previousViewport),
            "Global CRT restores an existing render target and viewport");
        GraphicsDevice.SetRenderTarget(null);
    }

    private static bool RegionChanged(
        IReadOnlyList<Color> before, IReadOnlyList<Color> after, int width, Rectangle region)
    {
        for (var y = region.Top; y < region.Bottom; y++)
        {
            for (var x = region.Left; x < region.Right; x++)
            {
                if (before[y * width + x] != after[y * width + x])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void RoundedSurfaceChecks()
    {
        using var target = new RenderTarget2D(GraphicsDevice, 80, 40);
        using var context = new RenderContext { Opacity = 1 };
        var pixels = new Color[80 * 40];
        var panel = new Panel { Width = 80, Height = 40 };
        panel.Measure(new Point(80, 40));
        panel.Arrange(target.Bounds);
        foreach (var radius in new[] { UIBorderRadii.Default.Zero, UIBorderRadii.Default.Md, UIBorderRadii.Default.Full })
        {
            GraphicsDevice.SetRenderTarget(target);
            GraphicsDevice.Clear(Color.Transparent);
            context.Begin();
            context.Scissor = target.Bounds;
            panel.Background = new RoundedRectangleBrush(Color.White, radius);
            panel.Render(context);
            context.End();
            GraphicsDevice.SetRenderTarget(null);
            target.GetData(pixels);
            Program.Check(pixels[20 * 80 + 40].A == 255, "Rounded surfaces retain their center");
            Program.Check(pixels[0].A == (radius == 0 ? 255 : 0), "Zero is square; rounded corners remain transparent");
        }

        GraphicsDevice.SetRenderTarget(target);
        GraphicsDevice.Clear(Color.Transparent);
        context.Begin();
        context.Scissor = target.Bounds;
        panel.Background = new RoundedRectangleBrush(Color.Transparent, UIBorderRadii.Default.Full, Color.White, 2);
        panel.Render(context);
        context.End();
        GraphicsDevice.SetRenderTarget(null);
        target.GetData(pixels);
        Program.Check(pixels[40].A == 255 && pixels[20 * 80 + 40].A == 0, "Rounded borders preserve a transparent fill");
    }

    private void SquareSurfaceChecks()
    {
        var radii = GameThemes.DeepDrive.BorderRadius;
        Program.Check(new[] { radii.Zero, radii.Xs, radii.Sm, radii.Md, radii.Lg, radii.Xl, radii.Full }.All(radius => radius == 0),
            "Every DEEP DRIVE radius token is zero, including Full");
        var navigation = new SettingsNavigationButton("VIDEO");
        foreach (var selected in new[] { false, true })
        {
            navigation.Select(selected);
            foreach (var brush in new[] { navigation.Background, navigation.OverBackground,
                navigation.FocusedBackground, navigation.PressedBackground, navigation.DisabledBackground })
            {
                Program.Check(brush is RoundedRectangleBrush { Radius: 0 },
                    "Sidebar idle, selected, hover, focus, pressed and disabled states all use square corners");
            }
        }
        using var target = new RenderTarget2D(GraphicsDevice, 240, 140);
        using var context = new RenderContext { Opacity = 1 };
        var pixels = new Color[target.Width * target.Height];
        var fill = new Color(40, 40, 40, 128);
        var border = new Color(128, 128, 128, 128);
        foreach (var scale in new[] { .4f, .5f, .8f, 1f, 1.25f, 1.6f, 2f })
            foreach (var stroke in new[] { 1, 2 })
                foreach (var offset in new[] { 11, 12, 13 })
                {
                    var panel = new Panel
                    {
                        Width = 80,
                        Height = 40,
                        Left = offset,
                        Top = offset,
                        TransformOrigin = Vector2.Zero,
                        Scale = new Vector2(scale),
                        Background = new RoundedRectangleBrush(fill, radii.Xs, border, stroke)
                    };
                    panel.Measure(target.Bounds.Size);
                    panel.Arrange(target.Bounds);
                    GraphicsDevice.SetRenderTarget(target);
                    GraphicsDevice.Clear(Color.Transparent);
                    context.Begin();
                    context.Scissor = target.Bounds;
                    panel.Render(context);
                    context.End();
                    GraphicsDevice.SetRenderTarget(null);
                    target.GetData(pixels);
                    var occupied = Enumerable.Range(0, pixels.Length).Where(index => pixels[index].A > 0).ToArray();
                    Program.Check(occupied.Length > 0, "Square surface renders at fractional scale");
                    var left = occupied.Min(index => index % target.Width);
                    var right = occupied.Max(index => index % target.Width);
                    var top = occupied.Min(index => index / target.Width);
                    var bottom = occupied.Max(index => index / target.Width);
                    for (var y = top; y <= bottom; y++)
                    {
                        for (var x = left; x <= right; x++)
                        {
                            var pixel = pixels[y * target.Width + x];
                            Program.Check(pixel.A == 128, $"Square surface has no row gaps or doubled opacity at scale {scale}");
                            if (x == left || x == right || y == top || y == bottom)
                            {
                                Program.Check(pixel == border, $"All four outline edges and corners remain continuous at scale {scale}");
                            }
                        }
                    }
                    Program.Check(pixels[(top + bottom) / 2 * target.Width + (left + right) / 2] == fill,
                        "Square outline does not overwrite the surface fill");
                }
    }

    private static void MenuButtonThemeChecks()
    {
        using var assets = new MenuAssets();
        var tokens = GameThemes.DeepDrive.MenuButton.Standard;

        var standard = new MenuButton(assets, "Standard", "settings");
        var (standardLayout, standardIcon, standardLabel, standardArrow) = Inspect(standard);
        var standardChevron = standardArrow
            ?? throw new InvalidOperationException("Standard menu button must have a trailing icon.");
        standard.Width = 500;
        standard.Height = 68;
        standard.Measure(new Point(500, 68));
        standard.Arrange(new Rectangle(0, 0, 500, 68));
        var iconPosition = standardIcon.ToGlobal(Point.Zero);
        var labelPosition = standardLabel.ToGlobal(Point.Zero);
        Program.Check(
            standardLayout.Padding.Left == tokens.HorizontalPadding
            && standardLayout.Padding.Right == tokens.HorizontalPadding
            && standardLayout.Padding.Top == tokens.VerticalPadding
            && standardLayout.Padding.Bottom == tokens.VerticalPadding,
            "Menu-button padding resolves from component theme tokens");
        Program.Check(
            standardLayout.ColumnSpacing == 0
            && standardLayout.ColumnsProportions[1].Type == ProportionType.Pixels
            && standardLayout.ColumnsProportions[1].Value == tokens.IconTextSpacing
            && standardLayout.ColumnsProportions[3].Type == ProportionType.Pixels
            && standardLayout.ColumnsProportions[3].Value == tokens.TextTrailingIconSpacing,
            "Menu-button icon and text gaps resolve independently from component theme tokens");
        Program.Check(
            standardIcon.Bounds.Width == tokens.IconSize
            && standardIcon.Bounds.Height == tokens.IconSize
            && labelPosition.X - (iconPosition.X + standardIcon.Bounds.Width) == tokens.IconTextSpacing,
            "Menu-button spacing does not compress or distort the leading icon");
        Program.Check(ReferenceEquals(standardLabel.Font, ThemeAssets.Font(tokens.DefaultFontSize)),
            "Default menu-button font variant resolves from the theme");
        Program.Check(
            standardIcon.Width == tokens.IconSize
            && standardIcon.Height == tokens.IconSize
            && standardChevron.Width == tokens.TrailingIconSize
            && standardChevron.Height == tokens.TrailingIconSize,
            "Default menu-button icon sizes resolve from the theme");

        var withoutArrow = new MenuButton(assets, "Back", "arrow-left", arrow: false);
        var (arrowlessLayout, _, _, absentArrow) = Inspect(withoutArrow);
        Program.Check(
            arrowlessLayout.ColumnsProportions.Count == 3
            && arrowlessLayout.Widgets.Count == 2
            && arrowlessLayout.Widgets.OfType<Image>().Count() == 1
            && absentArrow is null,
            "Arrowless menu buttons contain only icon, spacing, and text columns");
        Program.Check(
            arrowlessLayout.ColumnSpacing == 0
            && arrowlessLayout.ColumnsProportions[^1].Type == ProportionType.Fill,
            "Arrowless menu buttons reserve no phantom trailing gap");

        var textOnly = new MenuButton(assets, "KEEP", size: MenuButtonSize.Confirmation);
        var textLayout = (Grid)textOnly.Content;
        var textLabel = textLayout.Widgets.OfType<Label>().Single();
        Program.Check(textLayout.Widgets.Count == 1 && textLayout.ColumnsProportions.Count == 1
            && !textLayout.Widgets.OfType<Image>().Any(),
            "Omitting an icon creates a true text-only button without a default chevron or reserved icon gaps");
        foreach (var width in new[] { 140, 200, 400 })
        {
            textOnly.Measure(new Point(width, 44));
            textOnly.Arrange(new Rectangle(0, 0, width, 44));
            var center = textLabel.ToGlobal(new Vector2(textLabel.Bounds.Width / 2f, textLabel.Bounds.Height / 2f));
            Program.Check(Math.Abs(center.X - Math.Min(width, textOnly.Width!.Value) / 2f) <= 1
                && Math.Abs(center.Y - textOnly.Height!.Value / 2f) <= 1,
                "Text-only labels stay centered as their parent constrains the component");
            Program.Check(textLayout.Padding.Left == GameThemes.DeepDrive.MenuButton.Confirmation.HorizontalPadding,
                "Parent reflow does not rewrite the button's themed padding");
        }
        var trailingOnly = new MenuButton(assets, "NEXT", arrow: true);
        var trailingLayout = (Grid)trailingOnly.Content;
        Program.Check(trailingLayout.Widgets.OfType<Image>().Count() == 1
            && trailingLayout.ColumnsProportions.Count == 3
            && trailingLayout.ColumnsProportions[0].Type == ProportionType.Fill,
            "A trailing icon can be requested independently without a leading icon or gap");

        var cancel = new MenuButton(assets, "CANCEL", tone: MenuButtonTone.Danger);
        var cancelLabel = ((Grid)cancel.Content).Widgets.OfType<Label>().Single();
        Program.Check(cancelLabel.TextColor == GameThemes.DeepDrive.Danger
            && cancelLabel.OverTextColor == GameThemes.DeepDrive.DangerHighlight
            && cancelLabel.FocusedTextColor == GameThemes.DeepDrive.DangerHighlight
            && cancelLabel.DisabledTextColor == GameThemes.DeepDrive.Disabled
            && cancelLabel.PressedTextColor == GameThemes.DeepDrive.DeepBlack,
            "Cancel text uses themed red states, readable pressed text, and neutral disabled text");

        const int customIconSize = 41;
        const int customTrailingIconSize = 29;
        var compact = new MenuButton(
            assets,
            "Compact",
            "settings",
            textVariant: MenuButtonTextVariant.Compact,
            iconSize: customIconSize,
            trailingIconSize: customTrailingIconSize);
        var (_, compactIcon, compactLabel, compactArrow) = Inspect(compact);
        var compactChevron = compactArrow
            ?? throw new InvalidOperationException("Compact menu button must have a trailing icon.");
        Program.Check(ReferenceEquals(compactLabel.Font, ThemeAssets.Font(tokens.CompactFontSize)),
            "Compact menu-button font variant is selectable");
        Program.Check(
            compactIcon.Width == customIconSize
            && compactIcon.Height == customIconSize
            && compactChevron.Width == customTrailingIconSize
            && compactChevron.Height == customTrailingIconSize,
            "Leading and trailing menu-button icon sizes are independently selectable");

        var prominent = new MenuButton(
            assets,
            "Prominent",
            "play",
            textVariant: MenuButtonTextVariant.Prominent);
        var (_, _, prominentLabel, _) = Inspect(prominent);
        Program.Check(ReferenceEquals(prominentLabel.Font, ThemeAssets.Font(tokens.ProminentFontSize)),
            "Prominent menu-button font variant is selectable");

        foreach (var size in Enum.GetValues<MenuButtonSize>())
        {
            var style = GameThemes.DeepDrive.MenuButton.Size(size);
            var preset = new MenuButton(assets, "Preset", "settings", size: size);
            var (layout, icon, label, arrow) = Inspect(preset);
            Program.Check(preset.Width == style.Width && preset.Height == style.Height
                && layout.Padding.Left == style.HorizontalPadding && layout.Padding.Top == style.VerticalPadding
                && layout.ColumnsProportions[1].Value == style.IconTextSpacing
                && layout.ColumnsProportions[3].Value == style.TextTrailingIconSpacing
                && icon.Width == style.IconSize && arrow!.Width == style.TrailingIconSize
                && ReferenceEquals(label.Font, ThemeAssets.Font(style.DefaultFontSize)),
                "Each theme size supplies complete button and content dimensions on construction");
            preset.Measure(new Point(300, 100));
            preset.Arrange(new Rectangle(0, 0, 300, 100));
            Program.Check(preset.Bounds.Width <= 300 && preset.Width == style.Width
                && icon.Bounds.Width == icon.Bounds.Height && layout.Padding.Left == style.HorizontalPadding,
                "The backend fits a preset to its parent without changing authored sizes or distorting icons");
        }
        compact.Measure(new Point(300, 100));
        compact.Arrange(new Rectangle(0, 0, 300, 100));
        Program.Check(compactIcon.Width == customIconSize && compactChevron.Width == customTrailingIconSize
            && ReferenceEquals(compactLabel.Font, ThemeAssets.Font(tokens.CompactFontSize)),
            "Explicit font and icon overrides survive parent-driven reflow");

        Program.Check(RejectsSizeOverride(() =>
                _ = new MenuButton(assets, "Invalid", "settings", iconSize: 0), "iconSize"),
            "Menu-button rejects a zero leading-icon size override");
        Program.Check(RejectsSizeOverride(() =>
                _ = new MenuButton(assets, "Invalid", "settings", trailingIconSize: -1), "trailingIconSize"),
            "Menu-button rejects a negative trailing-icon size override");
        Program.Check(RejectsSizeOverride(() =>
                _ = new MenuButton(assets, "Invalid", "settings", iconSize: tokens.MinimumIconSize - 1), "iconSize"),
            "Menu-button rejects a leading-icon override below the themed minimum");
    }

    private static void DialogChecks()
    {
        var child = new Panel
        {
            Width = 160,
            Height = 80,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        var dialog = new Graphite.Game.UI.Dialog(child);
        var root = new Panel { Width = 800, Height = 600 };
        root.Widgets.Add(dialog);
        root.Measure(new Point(800, 600));
        root.Arrange(new Rectangle(0, 0, 800, 600));
        var childPosition = child.ToGlobal(Point.Zero);

        Program.Check(
            ReferenceEquals(dialog.Content, child)
            && dialog.Widgets.Count == 1
            && ReferenceEquals(child.Parent, dialog),
            "Dialog owns exactly the supplied child");
        Program.Check(
            dialog.HorizontalAlignment == HorizontalAlignment.Stretch
            && dialog.VerticalAlignment == VerticalAlignment.Stretch
            && dialog.Bounds.Width == 800
            && dialog.Bounds.Height == 600,
            "Dialog fills its parent");
        Program.Check(
            child.HorizontalAlignment == HorizontalAlignment.Center
            && child.VerticalAlignment == VerticalAlignment.Center
            && childPosition.X == 320
            && childPosition.Y == 260,
            $"Dialog centers its child (actual position: {childPosition})");
        Program.Check(
            dialog.Background is SolidBrush { Color: var color }
            && color == GameThemes.DeepDrive.DialogScrim
            && color.A is > 0 and < 255,
            "Dialog uses the themed translucent scrim");
        var inputFallsThrough = dialog.InputFallsThrough(new Point(8, 8));
        Program.Check(
            !inputFallsThrough
            && dialog.AcceptsKeyboardFocus,
            "Dialog blocks pointer interaction behind its scrim and can capture keyboard focus");

        var parented = new Label();
        root.Widgets.Add(parented);
        try
        {
            _ = new Graphite.Game.UI.Dialog(parented);
            throw new InvalidOperationException("Expected dialog to reject an already-parented child.");
        }
        catch (InvalidOperationException exception)
        {
            Program.Check(
                exception.Message.StartsWith("Add a dialog around", StringComparison.Ordinal),
                "Dialog rejects children that already belong to another container");
        }
    }

    private static (Grid Layout, Image Icon, Label Label, Image? Arrow) Inspect(MenuButton button)
    {
        var layout = button.Content as Grid
            ?? throw new InvalidOperationException("Menu button content must be a grid.");
        var icons = layout.Widgets.OfType<Image>().ToArray();
        if (icons.Length is < 1 or > 2)
        {
            throw new InvalidOperationException("Menu button must contain one leading icon and at most one trailing icon.");
        }

        return (
            layout,
            icons[0],
            layout.Widgets.OfType<Label>().Single(),
            icons.Length == 2 ? icons[1] : null);
    }

    private static bool RejectsSizeOverride(Action create, string parameter)
    {
        try
        {
            create();
            return false;
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return exception.ParamName == parameter;
        }
    }

    private void GlobalScaleChecks()
    {
        using (var menu = Ui.Open<MainMenuUI>())
        {
            SettingsButton(menu).DoClick();
            menu.Settings!.SelectPage(2);
            foreach (var (width, height, preference, name) in new[]
            {
                (1280, 720, 1f, "720"),
                (1920, 1080, 1f, "1080"),
                (1920, 1080, 1.25f, "1080-125"),
                (640, 480, 1f, "small"),
                (2560, 1440, 1.75f, "1440-175"),
                (2560, 1440, .75f, "1440-75"),
                (1280, 720, 1.75f, "720-175"),
                (1280, 720, .75f, "720-75")
            })
            {
                _graphics.PreferredBackBufferWidth = width;
                _graphics.PreferredBackBufferHeight = height;
                _graphics.ApplyChanges();
                menu.Settings!.UiScaleControl.SelectedIndex = (int)((preference - .75f) / .25f);
                Ui.Update(1);
                Program.Near(Preferences.Get(RuntimePreferences.UiScale), preference,
                    "Settings UI-scale dropdown saves the global runtime preference immediately");
                var expectedScale = Math.Min(width / 1600f, height / 900f) * preference;
                Program.Near(Ui.Scale, expectedScale, "Engine combines window scale and the runtime preference");
                foreach (var dialogLabel in menu.Settings!.Frame.GetChildren(true).OfType<Label>())
                {
                    Program.Near(((DynamicSpriteFont)dialogLabel.Font).FontSystem.FontResolutionFactor,
                        Math.Max(1, MathF.Ceiling(expectedScale)), "Existing dialog text resolves the current physical glyph density");
                }
                var chevron = ((Grid)menu.Settings!.UiScaleControl.Parent).Widgets.OfType<Image>().Single();
                Program.Check(chevron.Bounds.Width == 18 && chevron.Bounds.Height == 18,
                    "Dropdown chevron spacing does not squeeze its icon bounds");
                var overlay = menu.Settings!.Overlay;
                var overlayEnd = overlay.ToGlobal(new Vector2(overlay.Bounds.Width, overlay.Bounds.Height));
                Program.Check(overlay.ToGlobal(Point.Zero) == Point.Zero
                    && Math.Abs(overlayEnd.X - width) <= 2 && Math.Abs(overlayEnd.Y - height) <= 2,
                    "Open dialog scrim still covers the entire physical viewport");
                var panel = menu.Settings!.Frame;
                var center = panel.ToGlobal(new Vector2(panel.Bounds.Width / 2f, panel.Bounds.Height / 2f));
                Program.Check(Math.Abs(center.X - width / 2f) <= 2 && Math.Abs(center.Y - height / 2f) <= 2,
                    "Existing dialog stays centered after resizing and runtime preference changes");
                var panelStart = panel.ToGlobal(Vector2.Zero);
                var panelEnd = panel.ToGlobal(new Vector2(panel.Bounds.Width, panel.Bounds.Height));
                Program.Check(Math.Abs(panelEnd.X - panelStart.X - Math.Min(1080, Ui.LayoutSize.X - GameThemes.DeepDrive.Spacing.Xl * 2) * expectedScale) <= 2,
                    "Dialog content grows with the same global scale as the rest of the UI");
                var (_, icon, label, _) = Inspect(SettingsButton(menu));
                var iconStart = icon.ToGlobal(Vector2.Zero);
                var iconEnd = icon.ToGlobal(new Vector2(icon.Bounds.Width, icon.Bounds.Height));
                Program.Check(SettingsButton(menu).Width == 416 && SettingsButton(menu).Height == 54,
                    "Main-menu buttons use compact dimensions without changing the global scale");
                Program.Check(Math.Abs(iconEnd.X - iconStart.X - GameThemes.DeepDrive.MenuButton.Menu.IconSize * expectedScale) <= 1
                    && Math.Abs((iconEnd.X - iconStart.X) - (iconEnd.Y - iconStart.Y)) <= 1,
                    $"Global scaling preserves square icons and their themed size ({name}: {iconStart} to {iconEnd}, bounds {icon.Bounds}, scale {expectedScale})");
                Program.Check(Math.Abs(label.ToGlobal(Vector2.Zero).X - iconEnd.X
                    - GameThemes.DeepDrive.MenuButton.Menu.IconTextSpacing * expectedScale) <= 1,
                    "Global scaling preserves the themed icon/text gap");

                // Exercise real pointer dispatch at the transformed slider, not direct Value assignment.
                var slider = menu.Settings!.CrtIntensityControl;
                slider.Value = .5f;
                Ui.Update(0);
                _mouse.Position = slider.ToGlobal(new Vector2(slider.Bounds.Width / 2f, slider.Bounds.Height / 2f)).ToPoint();
                Ui.Update(0);
                _mouse.IsLeftButtonDown = true;
                Ui.Update(0);
                _mouse.Position = slider.ToGlobal(new Vector2(slider.Bounds.Width * .75f, slider.Bounds.Height / 2f)).ToPoint();
                Ui.Update(0);
                _mouse.IsLeftButtonDown = false;
                Ui.Update(0);
                Program.Check(slider.Value is > .65f and < .85f,
                    $"Scaled slider responds to dragging at its rendered location ({name}, actual {slider.Value})");
                Ui.Draw();
                var pixels = new Color[width * height];
                GraphicsDevice.GetBackBufferData(pixels);
                using var capture = new Texture2D(GraphicsDevice, width, height);
                capture.SetData(pixels);
                using var output = File.Create(Path.Combine(_output, $"scale-dialog-{name}.png"));
                capture.SaveAsPng(output, width, height);
            }

            Program.Check(menu.Settings!.UiScaleControl.Widgets.Count == 5, "UI scale offers five quarter-step presets");
            // Open and select actual popup rows after every live resize, including at 175%.
            foreach (var index in new[] { 4, 0, 3, 1, 2 })
            {
                var dropdown = menu.Settings!.UiScaleControl;
                ClickAt(((Grid)dropdown.Parent).Widgets.OfType<Image>().Single());
                Program.Check(dropdown.IsExpanded, "Clicking UI scale opens its dropdown");
                foreach (var option in dropdown.Widgets.OfType<Label>())
                {
                    Program.Near(((DynamicSpriteFont)option.Font).FontSystem.FontResolutionFactor,
                        Math.Max(1, MathF.Ceiling(Ui.Scale)), "Popup option text uses the current glyph density");
                }
                if (index == 0)
                {
                    Ui.Draw();
                    var pixels = new Color[Ui.ViewportWidth * Ui.ViewportHeight];
                    GraphicsDevice.GetBackBufferData(pixels);
                    using var capture = new Texture2D(GraphicsDevice, Ui.ViewportWidth, Ui.ViewportHeight);
                    capture.SetData(pixels);
                    using var output = File.Create(Path.Combine(_output, "scale-dropdown-175.png"));
                    capture.SaveAsPng(output, capture.Width, capture.Height);
                }
                ClickAt(dropdown.Widgets[index]);
                var targetScale = .75f + index * .25f;
                Program.Check(dropdown.SelectedIndex == index && !dropdown.IsExpanded,
                    "Choosing a scale preset selects the row and closes the dropdown");
                Program.Near(Preferences.Get(RuntimePreferences.UiScale), targetScale, "Dropdown selection persists the exact quarter-step scale");
                Program.Near(Ui.Scale, .8f * targetScale, "Dropdown selection resizes all UI without reopening settings");
            }

            Preferences.Remove(RuntimePreferences.UiScale);
            Ui.Update(0);
            Program.Check(menu.Settings!.UiScaleControl.SelectedIndex == 1, "The dropdown reflects externally reset preferences");
            Preferences.Set(RuntimePreferences.UiScale.Name, 1.31f);
            Ui.Update(0);
            Ui.Draw();
            Program.Near(Preferences.Get(RuntimePreferences.UiScale), 1.25f, "Legacy slider values snap to the nearest dropdown preset");
            Program.Check(menu.Settings!.UiScaleControl.SelectedIndex == 2, "Dropdown selection matches the migrated scale");
        }
        using (var reopened = Ui.Open<MainMenuUI>())
        {
            SettingsButton(reopened).DoClick();
            Program.Check(reopened.Settings!.UiScaleControl.SelectedIndex == 2, "New settings controls restore the saved scale");
        }

        Preferences.Set(RuntimePreferences.UiScale, 1.25f);
        using (var fixture = Ui.Open<FixtureScreen>())
        {
            Ui.Update(1);
            Program.Near(Ui.Scale, 1f, "New screens inherit persisted global magnification");
            Program.Check(fixture.Button.ToGlobal(Point.Zero) == new Point(100, 100),
                "An unrelated screen scales without its own resize handler");
            Preferences.Set(RuntimePreferences.UiScale, 1.5f);
            Ui.Update(0);
            Program.Near(Ui.Scale, 1.2f, "Already-open non-menu screens react to preference changes");
            var liveLabel = fixture.Button.GetChildren(true).OfType<Label>().Single();
            Program.Near(((DynamicSpriteFont)liveLabel.Font).FontSystem.FontResolutionFactor, 2,
                "Text on unrelated material-hosted screens updates without reopening");
            var lateInput = new TextBox { Text = "Late input", Left = 100, Top = 500, Width = 200, Height = 40 };
            ((Panel)fixture.Root.Content).Widgets.Add(lateInput);
            Ui.Update(0);
            Program.Near(((DynamicSpriteFont)lateInput.Font).FontSystem.FontResolutionFactor, 2,
                "Dynamically added text inputs resolve fonts even when scale is unchanged");
            _mouse.Position = fixture.Button.ToGlobal(new Vector2(40, 20)).ToPoint();
            Ui.Update(0);
            _mouse.IsLeftButtonDown = true;
            Ui.Update(0);
            _mouse.IsLeftButtonDown = false;
            Ui.Update(0);
            Program.Check(fixture.Clicks == 1, "Scaled material-hosted button receives a real pointer click");
            Preferences.Remove(RuntimePreferences.UiScale);
            Ui.Update(0);
            Program.Near(Ui.Scale, .8f, "Removing the preference restores the automatic scale live");
            Program.Near(((DynamicSpriteFont)lateInput.Font).FontSystem.FontResolutionFactor, 1,
                "Input text returns to low-density rasterization at small scales");
            foreach (var (legacy, expected) in new[] { (.5f, .75f), (2f, 1.75f) })
            {
                Preferences.Set(RuntimePreferences.UiScale.Name, legacy);
                Ui.Update(0);
                Program.Near(Ui.Scale, .8f * expected, "Legacy scales clamp globally without opening a settings dialog");
                Program.Near(Preferences.Get(RuntimePreferences.UiScale), expected, "Clamped scale is persisted");
            }
            Preferences.Remove(RuntimePreferences.UiScale);
            Ui.Update(0);
        }
        Preferences.Set(PlayerPreferences.CrtIntensity, 1f);
        CrtFilter.Configure(PostProcessing.Parameters, 1f);
        _mouse = new MouseInfo();
    }

    private void SettingsBorderChecks()
    {
        foreach (var size in new[] { new Point(1280, 720), new Point(2560, 1440) })
        {
            _graphics.PreferredBackBufferWidth = size.X;
            _graphics.PreferredBackBufferHeight = size.Y;
            _graphics.ApplyChanges();
            Preferences.Set(RuntimePreferences.UiScale, .75f);
            using var settings = Ui.Open<SettingsDialog>();
            _mouse = new MouseInfo();
            Ui.Update(1);
            Program.Near(Ui.Scale, size.Y / 900f * .75f, "Minimum-scale borders use the real viewport scale");
            CheckEdges(settings.WindowModeControl, "idle-dropdown");
            CheckEdges(settings.Navigation[1], "idle-sidebar");
            settings.ResolutionControl.Enabled = false;
            CheckEdges(settings.ResolutionControl, "disabled-dropdown");
            _mouse.Position = settings.WindowModeControl.ToGlobal(new Vector2(30, 20)).ToPoint();
            Ui.Update(0);
            CheckEdges(settings.WindowModeControl, "hover-dropdown");
            ClickAt(settings.WindowModeControl);
            Program.Check(settings.WindowModeControl.IsExpanded, "Minimum-scale dropdown opens at its drawn position");
            CheckEdges(settings.WindowModeControl, "expanded-dropdown");
            var popup = settings.WindowModeControl.Widgets[0].Parent;
            while (popup is not null && popup is not ListView)
            {
                popup = popup.Parent;
            }
            Program.Check(popup is not null, "Expanded dropdown has a real list surface");
            CheckEdges(popup!, "popup-list");
            ClickAt(settings.Navigation[0]);
            settings.SelectPage(2);
            Ui.Update(0);
            CheckEdges(settings.UiScaleControl, "ui-scale-dropdown");

            void CheckEdges(Widget widget, string name)
            {
                using var target = new RenderTarget2D(GraphicsDevice, size.X, size.Y, false,
                    SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                GraphicsDevice.SetRenderTarget(target);
                GraphicsDevice.Clear(Color.Transparent);
                Ui.Draw();
                GraphicsDevice.SetRenderTarget(null);
                var pixels = new Color[size.X * size.Y];
                target.GetData(pixels);
                using var output = File.Create(Path.Combine(_output, $"border-75-{size.Y}-{name}.png"));
                target.SaveAsPng(output, size.X, size.Y);
                var start = widget.ToGlobal(Vector2.Zero);
                var end = widget.ToGlobal(new Vector2(widget.Bounds.Width, widget.Bounds.Height));
                var left = (int)MathF.Ceiling(start.X);
                var top = (int)MathF.Ceiling(start.Y);
                var right = (int)MathF.Floor(end.X) - 1;
                var bottom = (int)MathF.Floor(end.Y) - 1;
                for (var x = left; x <= right; x++)
                {
                    Edge(x, top);
                    Edge(x, bottom);
                }
                for (var y = top; y <= bottom; y++)
                {
                    Edge(left, y);
                    Edge(right, y);
                }

                void Edge(int x, int y)
                {
                    var pixel = pixels[y * size.X + x];
                    Program.Check(pixel == GameThemes.DeepDrive.Border || pixel == GameThemes.DeepDrive.SelectionHighlight,
                        $"{name} at {size.Y}p/0.75x has a continuous border at {x},{y}: {pixel}");
                }
            }
        }
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.ApplyChanges();
        Preferences.Remove(RuntimePreferences.UiScale);
        _mouse = new MouseInfo();
        Ui.Update(0);
    }

    private void DisplayChecks()
    {
        DisplaySettings.Update(0);
        var initial = DisplaySettings.Current;
        using (var menu = Ui.Open<MainMenuUI>())
        {
            SettingsButton(menu).DoClick();
            Ui.Update(1);
            var dropdown = menu.Settings!.ResolutionControl;
            var sizes = DisplaySettings.Resolutions(WindowMode.Windowed);
            var next = sizes.First(size => size != initial.Resolution);
            ClickAt(((Grid)dropdown.Parent).Widgets.OfType<Image>().Single());
            Program.Check(dropdown.IsExpanded, "Resolution dropdown opens through its icon");
            ClickAt(dropdown.Widgets[sizes.ToList().IndexOf(next)]);
            Program.Check(DisplaySettings.Current == initial, "Display changes wait until the next engine update");
            DisplaySettings.Update(0);
            Ui.Update(0);
            Program.Check(DisplaySettings.Current.Resolution == next && DisplaySettings.NeedsConfirmation,
                "Selecting a resolution resizes the native backbuffer and starts confirmation");
            Program.Check(Preferences.Get(RuntimePreferences.Display) == "", "A preview does not overwrite saved display settings");
            Program.Check(menu.Settings!.DisplayConfirmation.Visible && DisplaySettings.SecondsRemaining == 15,
                "Resolution preview presents the fifteen-second confirmation dialog");
            var keepLayout = (Grid)menu.Settings!.KeepDisplayButton.Content;
            var cancelLayout = (Grid)menu.Settings!.RevertDisplayButton.Content;
            Program.Check(keepLayout.Widgets.Count == 1 && cancelLayout.Widgets.Count == 1
                && cancelLayout.Widgets.OfType<Label>().Single().Text == "CANCEL",
                "Graphics confirmation contains only KEEP and CANCEL labels, without icons");
            CaptureDisplay("display-confirmation");
            _mouse.Position = menu.Settings!.RevertDisplayButton.ToGlobal(new Vector2(100, 22)).ToPoint();
            Ui.Update(0);
            Program.Check(menu.Settings!.RevertDisplayButton.IsContentHighlighted
                && cancelLayout.Widgets.OfType<Label>().Single().TextColor == GameThemes.DeepDrive.DangerHighlight,
                "Hovering the graphics Cancel button uses red instead of the normal orange highlight");
            CaptureDisplay("display-confirmation-cancel-hover");
            ClickAt(menu.Settings!.RevertDisplayButton);
            Program.Check(DisplaySettings.Current == initial && !DisplaySettings.NeedsConfirmation,
                "The Revert button restores the previous native size");

            DisplaySettings.Preview(WindowMode.Windowed, next);
            DisplaySettings.Update(0);
            DisplaySettings.Update(16);
            Program.Check(DisplaySettings.Current == initial && !menu.Settings!.DisplayConfirmation.Visible,
                "Unconfirmed display changes automatically revert after timeout");

            DisplaySettings.Preview(WindowMode.Windowed, next);
            DisplaySettings.Update(0);
            Ui.Update(0);
            ClickAt(menu.Settings!.KeepDisplayButton);
            Program.Check(!DisplaySettings.NeedsConfirmation
                && Preferences.Get(RuntimePreferences.Display) == DisplaySettings.Current.ToString(),
                "The Keep button saves mode and resolution together");
            var confirmed = DisplaySettings.Current;

            var mode = menu.Settings!.WindowModeControl;
            ClickAt(((Grid)mode.Parent).Widgets.OfType<Image>().Single());
            Program.Check(mode.IsExpanded, "Display mode dropdown opens at the resized native size");
            ClickAt(mode.Widgets[1]);
            DisplaySettings.Update(0);
            Ui.Update(0);
            Program.Check(DisplaySettings.Current.Mode == WindowMode.BorderlessFullscreen
                && DisplaySettings.Current.Resolution == DisplaySettings.DesktopResolution,
                "Borderless fullscreen uses the full native desktop");
            Program.Check(_graphics.IsFullScreen && !_graphics.HardwareModeSwitch && Window.IsBorderless,
                "Borderless uses soft fullscreen without switching the monitor mode");
            Program.Check(!menu.Settings!.ResolutionControl.Enabled && menu.Settings!.ResolutionControl.Widgets.Count == 1,
                "Borderless resolution is displayed accurately and cannot be changed independently");
            Program.Near(Ui.Scale, Math.Min(Ui.ViewportWidth / 1600f, Ui.ViewportHeight / 900f),
                "Borderless mode updates global UI scale immediately");
            CaptureDisplay("display-borderless");
            ClickAt(menu.Settings!.RevertDisplayButton);
            Program.Check(DisplaySettings.Current == confirmed && !_graphics.IsFullScreen && !Window.IsBorderless,
                "Reverting borderless restores a decorated window and its confirmed resolution");

            DisplaySettings.Preview(WindowMode.Fullscreen, DisplaySettings.DesktopResolution);
            DisplaySettings.Update(0);
            Program.Check(DisplaySettings.Current.Mode == WindowMode.Fullscreen && _graphics.HardwareModeSwitch,
                "Exclusive fullscreen uses a supported display mode");
            DisplaySettings.Revert();
            Program.Check(DisplaySettings.Current == confirmed, "Exclusive fullscreen can be reverted safely");

            Preferences.Remove(RuntimePreferences.Display);
            DisplaySettings.Update(0);
            Ui.Update(0);
            Program.Check(DisplaySettings.Current == initial && menu.Settings!.ResolutionControl.Enabled,
                "Resetting the global display preference restores environment defaults reactively");
            CaptureDisplay("display-settings");
        }
        Preferences.Set(RuntimePreferences.Display, "Windowed:1024x768");
        DisplaySettings.Update(0);
        using (var reopened = Ui.Open<MainMenuUI>())
        {
            SettingsButton(reopened).DoClick();
            Program.Check(DisplaySettings.Current.Resolution == new Point(1024, 768)
                && reopened.Settings!.ResolutionControl.SelectedIndex is not null,
                "New screens inherit and display the global resolution");
        }
        Preferences.Remove(RuntimePreferences.Display);
        DisplaySettings.Update(0);
        _mouse = new MouseInfo();
    }

    private void SettingsDialogChecks()
    {
        var volume = Preferences.Get(PlayerPreferences.MasterVolume);
        var music = Preferences.Get(PlayerPreferences.MusicVolume);
        var fx = Preferences.Get(PlayerPreferences.FxVolume);
        using (var menu = Ui.Open<MainMenuUI>())
        using (var settings = Ui.Open<SettingsDialog>())
        {
            Ui.Update(1);
            Program.Check(settings.Navigation.Count == 3 && settings.SelectedPage == 0,
                "Reusable settings opens on Video with only three functional submenus");
            ClickAt(SettingsButton(menu));
            Program.Check(menu.Settings is null, "A standalone settings dialog blocks the underlying main menu");
            foreach (var index in new[] { 2, 1, 0, 1 })
            {
                ClickAt(settings.Navigation[index]);
                Program.Check(settings.SelectedPage == index
                    && settings.Navigation.Count(button => button.Selected) == 1,
                    "Sidebar clicks show exactly one selected settings page");
                Program.Check(settings.Navigation.All(button => button.Bounds.Width == settings.Navigation[0].Bounds.Width),
                    "Sidebar buttons have consistent full-column widths");
                CaptureDisplay($"settings-page-{index}");
            }
            DragVolume(settings.VolumeControl, .75f);
            Program.Check(AudioMixer.MasterVolume is > .65f and < .85f,
                "Audio volume responds to dragging and applies immediately to the runtime mixer");
            Program.Near(Preferences.Get(PlayerPreferences.MasterVolume), AudioMixer.MasterVolume, "Audio volume is persisted immediately");
            DragVolume(settings.MusicVolumeControl, .35f);
            Program.Check(AudioMixer.MusicVolume is > .25f and < .45f, "Music slider responds to dragging");
            DragVolume(settings.FxVolumeControl, .6f);
            Program.Check(AudioMixer.FxVolume is > .5f and < .7f, "FX slider responds to dragging");
            Program.Near(MediaPlayer.Volume, AudioMixer.MasterVolume * AudioMixer.MusicVolume,
                "Music receives master times music gain");
            Program.Near(SoundEffect.MasterVolume, AudioMixer.MasterVolume * AudioMixer.FxVolume,
                "All game and UI effects receive master times FX gain");
            Program.Check(!Ui.Audio.Muted && Ui.Audio.Volume == 1, "UI cues are not muted or double-attenuated");
            settings.VolumeControl.Value = 0;
            Program.Check(MediaPlayer.Volume == 0 && SoundEffect.MasterVolume == 0, "Zero master silences both channels");
            settings.VolumeControl.Value = .75f;
            Preferences.Initialize(Path.Combine(_output, "preferences"));
            Program.Near(Preferences.Get(PlayerPreferences.MusicVolume), AudioMixer.MusicVolume, "Music volume survives disk reload");
            Program.Near(Preferences.Get(PlayerPreferences.FxVolume), AudioMixer.FxVolume, "FX volume survives disk reload");
            CaptureDisplay("settings-audio-sliders");

            settings.SelectPage(0);
            var previous = DisplaySettings.Current;
            var next = DisplaySettings.Resolutions(WindowMode.Windowed).First(size => size != previous.Resolution);
            DisplaySettings.Preview(WindowMode.Windowed, next);
            DisplaySettings.Update(0);
            settings.Back();
            Program.Check(DisplaySettings.Current == previous && settings.IsOpen && !settings.IsClosing,
                "Back first dismisses and reverts the display confirmation, not the whole dialog");
            ClickAt(settings.BackButton);
            Ui.Update(1);
            Program.Check(!settings.IsOpen && menu.IsOpen, "Back closes only settings and leaves its underlying screen alive");
        }
        using (var reopened = Ui.Open<SettingsDialog>())
        {
            Program.Check(reopened.VolumeControl.Value is > .65f and < .85f
                && reopened.MusicVolumeControl.Value is > .25f and < .45f
                && reopened.FxVolumeControl.Value is > .5f and < .7f,
                "An independently reopened settings dialog restores audio preferences");
        }
        using (var credits = Ui.Open<CreditsDialog>())
        {
            Ui.Update(1);
            var center = credits.Frame.ToGlobal(new Vector2(credits.Frame.Bounds.Width / 2f, credits.Frame.Bounds.Height / 2f));
            Program.Check(Math.Abs(center.X - Ui.ViewportWidth / 2f) <= 2 && Math.Abs(center.Y - Ui.ViewportHeight / 2f) <= 2,
                "The extracted Credits dialog centers independently of the main menu");
            ClickAt(credits.BackButton);
            Ui.Update(1);
            Program.Check(!credits.IsOpen, "Credits owns its Back action and lifecycle");
        }
        var idleHosts = Ui.HostCount;
        for (var index = 0; index < 5; index++)
        {
            using var settings = Ui.Open<SettingsDialog>();
            settings.SelectPage(index % 3);
            Ui.Update(1);
        }
        Program.Check(Ui.HostCount == idleHosts, "Repeated standalone dialogs release their material hosts");
        Preferences.Set(PlayerPreferences.MasterVolume, volume);
        Preferences.Set(PlayerPreferences.MusicVolume, music);
        Preferences.Set(PlayerPreferences.FxVolume, fx);
        PlayerPreferences.ApplyAudio();
        _mouse = new MouseInfo();
    }

    private void DragVolume(HorizontalSlider slider, float value)
    {
        slider.Value = .5f;
        Ui.Update(0);
        _mouse.Position = slider.ToGlobal(new Vector2(slider.Bounds.Width / 2f, 12)).ToPoint();
        Ui.Update(0);
        _mouse.IsLeftButtonDown = true;
        Ui.Update(0);
        _mouse.Position = slider.ToGlobal(new Vector2(slider.Bounds.Width * value, 12)).ToPoint();
        Ui.Update(0);
        _mouse.IsLeftButtonDown = false;
        Ui.Update(0);
    }

    private void CaptureDisplay(string name)
    {
        PostProcessing.Render(new GameTime(), GameThemes.DeepDrive.Background, _ => Ui.Draw());
        EnvironmentOverlay.Draw();
        Program.Check(PostProcessing.TargetSize == new Point(Ui.ViewportWidth, Ui.ViewportHeight),
            "Display changes resize the whole-game CRT render targets");
        var pixels = new Color[Ui.ViewportWidth * Ui.ViewportHeight];
        GraphicsDevice.GetBackBufferData(pixels);
        using var capture = new Texture2D(GraphicsDevice, Ui.ViewportWidth, Ui.ViewportHeight);
        capture.SetData(pixels);
        using var output = File.Create(Path.Combine(_output, name + ".png"));
        capture.SaveAsPng(output, capture.Width, capture.Height);
    }

    private static MenuButton SettingsButton(MainMenuUI menu)
    {
        // Inspect the rendered widget tree in tests without exposing controls through the game API.
        var root = typeof(UIScreen).GetField("_root", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(menu) as Widget ?? throw new InvalidOperationException("The menu must be open.");
        return root.GetChildren(true).OfType<MenuButton>().Single(button =>
            button.GetChildren(true).OfType<Label>().Any(label => label.Text == "SETTINGS"));
    }

    private void ClickAt(Widget widget, Vector2? localPoint = null)
    {
        _mouse.Position = widget.ToGlobal(localPoint ?? new Vector2(widget.Bounds.Width / 2f, widget.Bounds.Height / 2f)).ToPoint();
        Ui.Update(0);
        _mouse.IsLeftButtonDown = true;
        Ui.Update(0);
        _mouse.IsLeftButtonDown = false;
        Ui.Update(0);
    }

    protected override void Update(GameTime gameTime)
    {
        _frame++;
        switch (_frame)
        {
            case 1:
                SettingsButton(_menu).DoClick();
                DisplaySettings.Preview(WindowMode.BorderlessFullscreen, DisplaySettings.DesktopResolution);
                break;
            case 2:
                Ui.Update(1);
                break;
            case 3:
                DisplaySettings.Revert();
                _menu.Dispose();
                _menu = Ui.Open<MainMenuUI>();
                Ui.Update(1);
                break;
            case 6:
                _mouse.Position = SettingsButton(_menu).ToGlobal(new Vector2(40, 20)).ToPoint();
                break;
            case 8:
                Program.Check(SettingsButton(_menu).IsContentHighlighted,
                    "Menu hover colors the button text, icon, and arrow");
                break;
            case 10:
                _mouse.Position = new Point(140, 250);
                break;
            case 14:
                _mouse.Position = new Point(2, 2);
                SettingsButton(_menu).DoClick();
                _menu.Settings!.SelectPage(2);
                break;
            case 19:
                Program.Check(_menu.Settings?.IsOpen == true, "Settings opens from the main menu");
                _menu.Settings!.CrtIntensityControl.Value = 0;
                break;
            case 20:
                Program.Check(_menu.Settings!.CrtIntensityControl.Value == 0, "Zero intensity disables CRT processing");
                Program.Check(_menu.Settings!.CrtIntensityControl.Visible, "CRT remains controllable at zero intensity");
                Program.Near(Preferences.Get(PlayerPreferences.CrtIntensity), 0, "Disabling CRT persists zero intensity");
                Program.Check(!PostProcessing.IsActive, "Zero intensity bypasses the global CRT pass");
                Program.Near(PostProcessing.Parameters.Get(CrtFilter.Scanlines), 0, "CRT can be disabled fullscreen");
                _menu.Settings!.CrtIntensityControl.Value = .5f;
                break;
            case 21:
                Program.Check(_menu.Settings!.CrtIntensityControl.Value > 0, "Non-zero intensity checks CRT enablement");
                Program.Check(_menu.Settings!.CrtIntensityControl.Visible, "CRT progress-slider stays visible when enabled");
                Program.Near(Preferences.Get(PlayerPreferences.CrtIntensity), .5f, "Enabling CRT selects fifty percent");
                Program.Check(PostProcessing.IsActive, "Non-zero intensity enables the global CRT pass");
                Program.Near(PostProcessing.Parameters.Get(CrtFilter.Scanlines), .05f,
                    "Fifty percent applies the original Aged scaling");
                _menu.Settings!.CrtIntensityControl.Value = 1;
                break;
            case 22:
                Program.Near(Preferences.Get(PlayerPreferences.CrtIntensity), 1, "Full aged CRT intensity persists immediately");
                Program.Near(PostProcessing.Parameters.Get(CrtFilter.Scanlines), .1f,
                    "Full aged CRT uses the original full strength");
                Ui.Close(_menu);
                Program.Check(_menu.IsClosing && _menu.IsOpen, "Close waits for exit animation");
                break;
            case 27:
                Program.Check(!_menu.IsOpen, "Screen closes after finite exit");
                _fixture = Ui.Open<FixtureScreen>();
                break;
            case 29:
                Program.Check(_fixture.Host.Animation.ActiveCount == 0, "Idle buttons have no material animation");
                _hitPosition = _fixture.Button.ToGlobal(new Vector2(40, 20));
                _mouse.Position = _hitPosition.ToPoint();
                break;
            case 30:
                Program.Check(_fixture.Host.Animation.ActiveCount == 0, "Pointer hover uses ordinary widget states");
                Program.Check(_fixture.Host.Animation.Frame.IsIdentity, "Ordinary hover does not transform the button");
                Program.Check(_fixture.Button.ContainsGlobalPoint(_hitPosition.ToPoint()), "Hover leaves hitbox stable");
                Ui.Update(1.2f);
                Program.Check(_fixture.Host.Animation.ActiveCount == 0, "Hover remains free of material playback");
                Program.Check(_fixture.Other.Animation.ActiveCount == 0, "Hover leaves other buttons unaffected");
                break;
            case 31:
                _mouse.Position = new Point(2, 2);
                break;
            case 32:
                Program.Check(_fixture.Host.Animation.ActiveCount == 0, "Leaving hover needs no material cleanup");
                _mouse.Position = _hitPosition.ToPoint();
                break;
            case 33:
                Program.Check(_fixture.Host.Animation.ActiveCount == 0, "Hover reentry remains an ordinary widget state");
                _fixture.Button.Enabled = false;
                break;
            case 34:
                Program.Check(_fixture.Host.Animation.ActiveCount == 0, "Disabling a hovered button starts no effect");
                _mouse.Position = new Point(2, 2);
                break;
            case 35:
                _mouse.Position = _hitPosition.ToPoint();
                break;
            case 36:
                Program.Check(_fixture.Host.Animation.ActiveCount == 0, "Disabled control ignores hover");
                Program.Check(_fixture.Host.Animation.ActiveCount == 0, "Disabled control has no material effect");
                _fixture.Button.Enabled = true;
                _mouse.Position = new Point(2, 2);
                _fixture.Button.SetKeyboardFocus();
                break;
            case 37:
                Program.Check(_fixture.Host.Animation.ActiveCount == 0, "Keyboard focus alone does not animate");
                _fixture.Button.DoClick();
                Program.Check(_fixture.Clicks == 1, "Original click handler preserved");
                _hide = _fixture.Host.Hide();
                Program.Check(!_hide.IsCompleted, "Host hide waits");
                Program.Check(ReferenceEquals(_hide, _fixture.Host.Hide()), "Repeated hide is idempotent");
                _fixture.Host.Show();
                Program.Check(_hide.Result == UIPlaybackState.Cancelled && _fixture.Host.Enabled, "Show interrupts hide");
                break;
            case 40:
                ProbeMaterial.Configure(_fixture.Root.Animation.BaseParameters, 1f);
                ProbeMaterial.Configure(_fixture.Other.Animation.BaseParameters, 0f);
                break;
            case 44:
                Program.Near(_fixture.Root.Animation.Parameters.Get(ProbeMaterial.Amount), 1f,
                    "Test material applies to the root host");
                Program.Near(_fixture.Other.Animation.Parameters.Get(ProbeMaterial.Amount), 0,
                    "Per-instance material parameters remain independent");
                _graphics.PreferredBackBufferWidth = 960;
                _graphics.PreferredBackBufferHeight = 600;
                _graphics.ApplyChanges();
                break;
            case 50:
                _fixture.Root.Hide(immediate: true);
                Ui.Update(0);
                _fixture.Root.Show();
                Ui.Update(0);
                Program.Check(_fixture.Host.Visible && _fixture.Other.Visible, "Ancestor hide preserves child visibility");
                _fixture.Dispose();
                Program.Check(_fixture.Host.IsDisposed && _fixture.Other.IsDisposed, "Teardown disposes nested hosts");
                _graphics.PreferredBackBufferWidth = 1280;
                _graphics.PreferredBackBufferHeight = 720;
                _graphics.ApplyChanges();
                _menu = Ui.Open<MainMenuUI>();
                break;
            case 53:
                _graphics.PreferredBackBufferWidth = 960;
                _graphics.PreferredBackBufferHeight = 600;
                _graphics.ApplyChanges();
                break;
            case 57:
                _graphics.PreferredBackBufferWidth = 640;
                _graphics.PreferredBackBufferHeight = 480;
                _graphics.ApplyChanges();
                break;
            case 61:
                _graphics.PreferredBackBufferWidth = 1280;
                _graphics.PreferredBackBufferHeight = 720;
                _graphics.ApplyChanges();
                break;
            case 69:
                _allocations = Ui.MaterialRenderer.AllocatedTargets;
                _postProcessAllocations = PostProcessing.AllocatedTargets;
                _menu.Dispose();
                _menu = Ui.Open<MainMenuUI>();
                break;
            case 89:
                Program.Check(Ui.MaterialRenderer.AllocatedTargets <= _allocations, "Repeated screens do not accumulate targets");
                Program.Check(PostProcessing.AllocatedTargets <= _postProcessAllocations,
                    "Repeated screens do not accumulate global frame targets");
                _menu.Dispose();
                var renderer = Ui.MaterialRenderer;
                Ui.Shutdown();
                EnvironmentOverlay.Shutdown();
                PostProcessing.Shutdown();
                DisplaySettings.Shutdown();
                Preferences.Shutdown();
                Program.Check(renderer.AllocatedTargets == 0, "All render targets released at shutdown");
                Program.Check(PostProcessing.AllocatedTargets == 0 && PostProcessing.TargetSize == Point.Zero,
                    "Global frame targets are released at shutdown");
                Exit();
                return;
        }
        DisplaySettings.Update(.04f);
        Ui.Update(.04f);
    }
    protected override void Draw(GameTime gameTime)
    {
        if (_frame >= 89)
        {
            return;
        }

        using var target = new RenderTarget2D(GraphicsDevice, Ui.ViewportWidth, Ui.ViewportHeight,
            false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        GraphicsDevice.SetRenderTarget(target);
        GraphicsDevice.Clear(GameThemes.DeepDrive.Background);
        var previous = GraphicsDevice.Viewport;
        Ui.Draw();
        Program.Check(GraphicsDevice.GetRenderTargets()[0].RenderTarget == target && GraphicsDevice.Viewport.Equals(previous), "Rendering restores target and viewport");
        GraphicsDevice.SetRenderTarget(null);

        var usage = GraphicsDevice.PresentationParameters.RenderTargetUsage;
        PostProcessing.Render(gameTime, GameThemes.DeepDrive.Background, _ => Ui.Draw());
        EnvironmentOverlay.Draw();
        Program.Check(GraphicsDevice.GetRenderTargets().Length == 0,
            "Fullscreen rendering restores the back buffer");
        Program.Check(GraphicsDevice.PresentationParameters.RenderTargetUsage == usage,
            "Fullscreen rendering preserves the back buffer usage policy");
        if (PostProcessing.IsActive)
        {
            Program.Check(PostProcessing.TargetSize == new Point(Ui.ViewportWidth, Ui.ViewportHeight),
                "Fullscreen CRT follows back buffer resizing");
        }

        if (new[] { 2, 4, 20, 30, 42, 46, 56, 60, 65 }.Contains(_frame))
        {
            var pixels = new Color[Ui.ViewportWidth * Ui.ViewportHeight];
            GraphicsDevice.GetBackBufferData(pixels);
            using var file = File.Create(Path.Combine(_output, $"frame-{_frame}.png"));
            using var capture = new Texture2D(GraphicsDevice, Ui.ViewportWidth, Ui.ViewportHeight);
            capture.SetData(pixels);
            capture.SaveAsPng(file, capture.Width, capture.Height);
            Program.Check(pixels.Any(pixel => pixel.R > 150 && pixel.G > 150), "Rendered UI retains readable text");
            Program.Check(Enumerable.Range(Ui.ViewportHeight - 60, 60)
                .Any(y => pixels.AsSpan(y * Ui.ViewportWidth, 200).ToArray().Any(pixel => pixel.R > 100)),
                "The bottom-left environment print survives dialogs, scene changes, CRT, and resizing");
            if (_frame == 42)
            {
                var rawPixels = new Color[target.Width * target.Height];
                target.GetData(rawPixels);
                var insidePoint = _fixture.Root.ToGlobal(new Point(240, 270));
                var outsidePoint = _fixture.Root.ToGlobal(new Point(280, 270));
                var inside = rawPixels[insidePoint.Y * target.Width + insidePoint.X];
                var outside = rawPixels[outsidePoint.Y * target.Width + outsidePoint.X];
                Program.Check(inside.R + inside.G + inside.B > outside.R + outside.G + outside.B,
                    "Material renders inside clipped panel and respects ancestor clipping");
            }
        }

        if (_frame is 4 or 8 or 12)
        {
            var pixels = new Color[Ui.ViewportWidth * Ui.ViewportHeight];
            GraphicsDevice.GetBackBufferData(pixels);
            using var capture = new Texture2D(GraphicsDevice, Ui.ViewportWidth, Ui.ViewportHeight);
            capture.SetData(pixels);
            using var file = File.Create(Path.Combine(_output, $"backbuffer-{_frame}.png"));
            capture.SaveAsPng(file, capture.Width, capture.Height);
            Program.Check(pixels[100 * Ui.ViewportWidth + 1000].A == 255,
                "Fullscreen CRT preserves opaque menu background coverage");
        }
    }
}

internal sealed class FixtureScreen : UIScreen
{
    private readonly MenuAssets _assets = new();
    public Button Button { get; private set; } = null!;
    public UIMaterialHost Host { get; private set; } = null!;
    public UIMaterialHost Other { get; private set; } = null!;
    public int Clicks
    {
        get; private set;
    }
    public UIMaterialHost Root { get; private set; } = null!;
    protected override Widget Build()
    {
        Button = new MenuButton(_assets, "Material fixture", "settings", arrow: false);
        Button.Width = 240;
        Button.Height = 48;
        Button.Left = 100;
        Button.Top = 100;
        Button.HorizontalAlignment = HorizontalAlignment.Left;
        Button.VerticalAlignment = VerticalAlignment.Top;
        Button.Click += (_, _) => Clicks++;
        Host = new UIMaterialHost(Button, interactions: MenuPresentation.FadeStyle);
        var other = Button.CreateTextButton("Independent material");
        other.Width = 240;
        other.Height = 48;
        other.Left = 440;
        other.Top = 100;
        other.HorizontalAlignment = HorizontalAlignment.Left;
        other.VerticalAlignment = VerticalAlignment.Top;
        Other = new UIMaterialHost(other, [ProbeMaterial.Shared]);
        var panel = new Panel(styleName: "root");
        panel.Widgets.Add(Host);
        panel.Widgets.Add(Other);
        var clipped = new Panel
        {
            Left = 100,
            Top = 240,
            Width = 160,
            Height = 80,
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        var clippedButton = Button.CreateTextButton("Clipped fixture");
        clippedButton.Width = 200;
        clippedButton.Height = 48;
        clippedButton.Left = 80;
        clippedButton.HorizontalAlignment = HorizontalAlignment.Left;
        clippedButton.VerticalAlignment = VerticalAlignment.Top;
        var clippedHost = new UIMaterialHost(clippedButton, [ProbeMaterial.Shared]);
        ProbeMaterial.Configure(clippedHost.Animation.BaseParameters, .4f);
        clipped.Widgets.Add(clippedHost);
        panel.Widgets.Add(clipped);
        Root = new UIMaterialHost(panel, [ProbeMaterial.Shared], MenuPresentation.FadeStyle);
        ProbeMaterial.Configure(Root.Animation.BaseParameters, 1f);
        try
        {
            _ = new UIMaterialHost(new Button(), interactions: new UIInteractionStyle
            {
                Bindings = new Dictionary<string, UIInteractionBinding>
                {
                    [UITrigger.Hide] = new() { Animation = MenuPresentation.FadeOut with { Repeat = 0 } }
                }
            });
            throw new InvalidOperationException("Expected infinite hide rejection");
        }
        catch (ArgumentException) { }
        return Root;
    }

    protected override void OnDestroy() => _assets.Dispose();
}

internal sealed class ProbeMaterial : UIMaterial
{
    public static ProbeMaterial Shared { get; } = new();
    public static UIParameter<float> Amount { get; } = new("ProbeAmount", 0);

    public static void Configure(UIParameters parameters, float amount) => parameters.Set(Amount, amount);

    public override UIMaterialInstance CreateInstance(GraphicsDevice graphicsDevice) => new ProbeMaterialInstance();

    private sealed class ProbeMaterialInstance : UIMaterialInstance
    {
        public override bool IsActive(UIParameters parameters) => parameters.Get(Amount) > 0;
        public override void Render(UIMaterialContext context) => context.Draw();
    }
}

internal sealed class FailingScreen : UIScreen
{
    protected override Widget Build()
    {
        _ = new UIMaterialHost(Button.CreateTextButton("Unattached fixture"));
        throw new NotSupportedException("Deliberate build failure");
    }
}
