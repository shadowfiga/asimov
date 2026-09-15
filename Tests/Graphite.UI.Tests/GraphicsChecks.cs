using Graphite.Engine.UI;
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
using Graphite.Game.Scenes;
using Graphite.Game.UI.Materials;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Ui = Graphite.Engine.UI.UI;

namespace Graphite.UI.Tests;

internal sealed class GraphicsChecks : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private MainMenuScreen _menu = null!;
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
        Preferences.Initialize(Path.Combine(_output, "preferences"));
        Preferences.Set(PlayerPreferences.CrtIntensity, 1f);
        Startup.Initialize();
        MyraEnvironment.MouseInfoGetter = () => _mouse;
        Directory.CreateDirectory(_output);
        NativeAudioCheck();
        CrtShaderChecks();
        FullscreenPostProcessChecks();
        RoundedSurfaceChecks();
        MenuButtonThemeChecks();
        SceneManager.Load<BootstrapScene>();
        SceneManager.CommitPendingChanges();
        Program.Check(Ui.HostCount > 0, "Bootstrap opens the main menu");
        SceneManager.Update(0);
        SceneManager.Shutdown();
        Program.Check(Ui.HostCount == 0, "Scene teardown releases the menu's material hosts");
        var hostCount = Ui.HostCount;
        try { Ui.Open<FailingScreen>(); throw new InvalidOperationException("Expected build failure"); }
        catch (NotSupportedException) { Program.Check(Ui.HostCount == hostCount, "Failed screen build releases hosts"); }
        _menu = Ui.Open<MainMenuScreen>();
    }
    private void NativeAudioCheck()
    {
        var path = Path.Combine(_output, "silent-fixture.wav");
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            const int bytes = 2205 * 2;
            writer.Write("RIFF"u8); writer.Write(36 + bytes); writer.Write("WAVEfmt "u8);
            writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(22050);
            writer.Write(44100); writer.Write((short)2); writer.Write((short)16);
            writer.Write("data"u8); writer.Write(bytes); writer.Write(new byte[bytes]);
        }
        using (var audio = new UIAudioService { Muted = true })
        {
            using var voice = audio.Play(new UISoundCue { Asset = path, Loop = true, Volume = .3f, Pitch = .1f, Pan = -.2f });
            Program.Check(voice.IsPlaying, "Native audio fixture starts");
            audio.Volume = .2f; audio.Update(); voice.Stop(); audio.Update();
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
        GraphicsDevice.SetRenderTarget(source); GraphicsDevice.Clear(Color.Transparent);
        batch.Begin(); batch.Draw(pixel, new Rectangle(40, 40, 48, 48), Color.White); batch.End();
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
        var colors = new Color[128 * 128]; destination.GetData(colors);
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
            try { batch.Draw(pixel, new Rectangle(0, 0, width, height), probeColor); }
            finally { batch.End(); }
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

    private static void MenuButtonThemeChecks()
    {
        using var assets = new MenuAssets();
        var tokens = GameThemes.DeepDrive.MenuButton;

        var standard = new MenuButton(assets, "Standard", "settings");
        var (standardLayout, standardIcon, standardLabel, standardArrow) = Inspect(standard);
        var standardChevron = standardArrow
            ?? throw new InvalidOperationException("Standard menu button must have a trailing icon.");
        Program.Check(
            standardLayout.Padding.Left == tokens.HorizontalPadding
            && standardLayout.Padding.Right == tokens.HorizontalPadding
            && standardLayout.Padding.Top == tokens.VerticalPadding
            && standardLayout.Padding.Bottom == tokens.VerticalPadding,
            "Menu-button padding resolves from component theme tokens");
        Program.Check(
            standardLayout.ColumnSpacing == 0
            && standardIcon.Margin.Right == tokens.IconTextSpacing
            && standardChevron.Margin.Left == tokens.TextTrailingIconSpacing,
            "Menu-button icon and text gaps resolve independently from component theme tokens");
        Program.Check(ReferenceEquals(standardLabel.Font, ThemeAssets.Font(tokens.DefaultFontSize)),
            "Default menu-button font variant resolves from the theme");
        Program.Check(
            standardIcon.Width == tokens.IconSize
            && standardIcon.Height == tokens.IconSize
            && standardChevron.Width == tokens.TrailingIconSize
            && standardChevron.Height == tokens.TrailingIconSize,
            "Default menu-button icon sizes resolve from the theme");

        var withoutArrow = new MenuButton(assets, "Back", "arrow-left", arrow: false);
        var (twoColumnLayout, _, twoColumnLabel, absentArrow) = Inspect(withoutArrow);
        Program.Check(
            twoColumnLayout.ColumnsProportions.Count == 2
            && twoColumnLayout.Widgets.Count == 2
            && twoColumnLayout.Widgets.OfType<Image>().Count() == 1
            && absentArrow is null,
            "Arrowless menu buttons contain only icon and text columns");
        Program.Check(twoColumnLayout.ColumnSpacing == 0 && twoColumnLabel.Margin.Right == 0,
            "Arrowless menu buttons reserve no phantom trailing gap");

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

        compact.Resize(.1f);
        var (smallLayout, smallIcon, smallLabel, smallArrow) = Inspect(compact);
        var smallChevron = smallArrow
            ?? throw new InvalidOperationException("Resized compact menu button must have a trailing icon.");
        Program.Check(
            ReferenceEquals(smallLabel.Font, ThemeAssets.Font(tokens.CompactMinimumFontSize))
            && smallIcon.Width == tokens.MinimumIconSize
            && smallIcon.Height == tokens.MinimumIconSize
            && smallChevron.Width == tokens.MinimumTrailingIconSize
            && smallChevron.Height == tokens.MinimumTrailingIconSize,
            "Menu-button resizing respects themed font and icon minimums");
        Program.Check(
            smallLayout.Padding.Left == (int)(tokens.HorizontalPadding * .1f)
            && smallIcon.Margin.Right == (int)(tokens.IconTextSpacing * .1f)
            && smallChevron.Margin.Left == (int)(tokens.TextTrailingIconSpacing * .1f),
            "Menu-button resizing scales themed padding and gaps");

        compact.Resize(2f);
        var (largeLayout, largeIcon, largeLabel, largeArrow) = Inspect(compact);
        var largeChevron = largeArrow
            ?? throw new InvalidOperationException("Resized compact menu button must retain its trailing icon.");
        Program.Check(
            ReferenceEquals(largeLabel.Font, ThemeAssets.Font(tokens.CompactFontSize * 2))
            && largeIcon.Width == customIconSize * 2
            && largeIcon.Height == customIconSize * 2
            && largeChevron.Width == customTrailingIconSize * 2
            && largeChevron.Height == customTrailingIconSize * 2,
            "Menu-button resizing retains the selected variant and icon overrides");
        Program.Check(
            largeLayout.Padding.Left == tokens.HorizontalPadding * 2
            && largeIcon.Margin.Right == tokens.IconTextSpacing * 2
            && largeChevron.Margin.Left == tokens.TextTrailingIconSpacing * 2,
            "Menu-button theme spacing scales with the component");

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

    protected override void Update(GameTime gameTime)
    {
        _frame++;
        switch (_frame)
        {
            case 6: _mouse.Position = new Point(140, 455); break;
            case 8:
                Program.Check(_menu.SettingsButton.IsContentHighlighted,
                    "Menu hover colors the button text, icon, and arrow");
                break;
            case 10: _mouse.Position = new Point(140, 250); break;
            case 14:
                _mouse.Position = new Point(2, 2);
                _menu.SettingsButton.DoClick();
                break;
            case 19:
                Program.Check(_menu.SettingsVisible, "Settings opens from the main menu");
                _menu.CrtIntensityControl.Value = 0;
                break;
            case 20:
                Program.Check(!_menu.CrtEnabled, "Zero intensity disables CRT processing");
                Program.Check(_menu.CrtControlVisible, "CRT remains controllable at zero intensity");
                Program.Near(Preferences.Get(PlayerPreferences.CrtIntensity), 0, "Disabling CRT persists zero intensity");
                Program.Check(!PostProcessing.IsActive, "Zero intensity bypasses the global CRT pass");
                Program.Near(PostProcessing.Parameters.Get(CrtFilter.Scanlines), 0, "CRT can be disabled fullscreen");
                _menu.CrtIntensityControl.Value = .5f;
                break;
            case 21:
                Program.Check(_menu.CrtEnabled, "Non-zero intensity checks CRT enablement");
                Program.Check(_menu.CrtControlVisible, "CRT progress-slider stays visible when enabled");
                Program.Near(Preferences.Get(PlayerPreferences.CrtIntensity), .5f, "Enabling CRT selects fifty percent");
                Program.Check(PostProcessing.IsActive, "Non-zero intensity enables the global CRT pass");
                Program.Near(PostProcessing.Parameters.Get(CrtFilter.Scanlines), .05f,
                    "Fifty percent applies the original Aged scaling");
                _menu.CrtIntensityControl.Value = 1;
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
            case 31: _mouse.Position = new Point(2, 2); break;
            case 32:
                Program.Check(_fixture.Host.Animation.ActiveCount == 0, "Leaving hover needs no material cleanup");
                _mouse.Position = _hitPosition.ToPoint(); break;
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
                _graphics.PreferredBackBufferWidth = 960; _graphics.PreferredBackBufferHeight = 600; _graphics.ApplyChanges();
                break;
            case 50:
                _fixture.Root.Hide(immediate: true); Ui.Update(0);
                _fixture.Root.Show(); Ui.Update(0);
                Program.Check(_fixture.Host.Visible && _fixture.Other.Visible, "Ancestor hide preserves child visibility");
                _fixture.Dispose();
                Program.Check(_fixture.Host.IsDisposed && _fixture.Other.IsDisposed, "Teardown disposes nested hosts");
                _graphics.PreferredBackBufferWidth = 1280; _graphics.PreferredBackBufferHeight = 720; _graphics.ApplyChanges();
                _menu = Ui.Open<MainMenuScreen>();
                break;
            case 53:
                _graphics.PreferredBackBufferWidth = 960; _graphics.PreferredBackBufferHeight = 600; _graphics.ApplyChanges();
                break;
            case 57:
                _graphics.PreferredBackBufferWidth = 640; _graphics.PreferredBackBufferHeight = 480; _graphics.ApplyChanges();
                break;
            case 61:
                _graphics.PreferredBackBufferWidth = 1280; _graphics.PreferredBackBufferHeight = 720; _graphics.ApplyChanges();
                break;
            case 69:
                _allocations = Ui.MaterialRenderer.AllocatedTargets;
                _postProcessAllocations = PostProcessing.AllocatedTargets;
                _menu.Dispose(); _menu = Ui.Open<MainMenuScreen>();
                break;
            case 89:
                Program.Check(Ui.MaterialRenderer.AllocatedTargets <= _allocations, "Repeated screens do not accumulate targets");
                Program.Check(PostProcessing.AllocatedTargets <= _postProcessAllocations,
                    "Repeated screens do not accumulate global frame targets");
                _menu.Dispose();
                var renderer = Ui.MaterialRenderer;
                Ui.Shutdown();
                PostProcessing.Shutdown();
                Preferences.Shutdown();
                Program.Check(renderer.AllocatedTargets == 0, "All render targets released at shutdown");
                Program.Check(PostProcessing.AllocatedTargets == 0 && PostProcessing.TargetSize == Point.Zero,
                    "Global frame targets are released at shutdown");
                Exit();
                return;
        }
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
        Program.Check(GraphicsDevice.GetRenderTargets().Length == 0,
            "Fullscreen rendering restores the back buffer");
        Program.Check(GraphicsDevice.PresentationParameters.RenderTargetUsage == usage,
            "Fullscreen rendering preserves the back buffer usage policy");
        if (PostProcessing.IsActive)
        {
            Program.Check(PostProcessing.TargetSize == new Point(Ui.ViewportWidth, Ui.ViewportHeight),
                "Fullscreen CRT follows back buffer resizing");
        }

        if (new[] { 4, 20, 30, 42, 46, 56, 60, 65 }.Contains(_frame))
        {
            var pixels = new Color[Ui.ViewportWidth * Ui.ViewportHeight];
            GraphicsDevice.GetBackBufferData(pixels);
            using var file = File.Create(Path.Combine(_output, $"frame-{_frame}.png"));
            using var capture = new Texture2D(GraphicsDevice, Ui.ViewportWidth, Ui.ViewportHeight);
            capture.SetData(pixels);
            capture.SaveAsPng(file, capture.Width, capture.Height);
            Program.Check(pixels.Any(pixel => pixel.R > 150 && pixel.G > 150), "Rendered UI retains readable text");
            if (_frame == 42)
            {
                var rawPixels = new Color[target.Width * target.Height];
                target.GetData(rawPixels);
                var inside = rawPixels[270 * target.Width + 240];
                var outside = rawPixels[270 * target.Width + 280];
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
    public int Clicks { get; private set; }
    public UIMaterialHost Root { get; private set; } = null!;
    protected override Widget Build()
    {
        Button = new MenuButton(_assets, "Material fixture", "settings", arrow: false);
        Button.Width = 240; Button.Height = 48; Button.Left = 100; Button.Top = 100;
        Button.HorizontalAlignment = HorizontalAlignment.Left; Button.VerticalAlignment = VerticalAlignment.Top;
        Button.Click += (_, _) => Clicks++;
        Host = new UIMaterialHost(Button, interactions: MenuPresentation.FadeStyle);
        var other = Button.CreateTextButton("Independent material");
        other.Width = 240; other.Height = 48; other.Left = 440; other.Top = 100;
        other.HorizontalAlignment = HorizontalAlignment.Left; other.VerticalAlignment = VerticalAlignment.Top;
        Other = new UIMaterialHost(other, [ProbeMaterial.Shared]);
        var panel = new Panel(styleName: "root"); panel.Widgets.Add(Host); panel.Widgets.Add(Other);
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
        clippedButton.Width = 200; clippedButton.Height = 48; clippedButton.Left = 80;
        clippedButton.HorizontalAlignment = HorizontalAlignment.Left; clippedButton.VerticalAlignment = VerticalAlignment.Top;
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
                { [UITrigger.Hide] = new() { Animation = MenuPresentation.FadeOut with { Repeat = 0 } } }
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
