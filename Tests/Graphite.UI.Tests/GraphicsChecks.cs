using Graphite.Engine.UI;
using Graphite.Engine.Persistence;
using Graphite.Game;
using Graphite.Game.Configuration;
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
        Preferences.Initialize(Path.Combine(_output, "preferences"));
        Preferences.Set(PlayerPreferences.CrtIntensity, 1f);
        Startup.Initialize();
        MyraEnvironment.MouseInfoGetter = () => _mouse;
        Directory.CreateDirectory(_output);
        NativeAudioCheck();
        MaterialChecks();
        RoundedSurfaceChecks();
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
    private void MaterialChecks()
    {
        using var source = new RenderTarget2D(GraphicsDevice, 128, 128, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        using var destination = new RenderTarget2D(GraphicsDevice, 128, 128);
        using var batch = new SpriteBatch(GraphicsDevice);
        using var pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData([GameThemes.DeepDrive.PrimaryText]);
        GraphicsDevice.SetRenderTarget(source); GraphicsDevice.Clear(Color.Transparent);
        batch.Begin(); batch.Draw(pixel, new Rectangle(40, 40, 48, 48), Color.White); batch.End();
        var parameters = new UIParameters();
        using var crt = MenuPresentation.Crt.CreateInstance(GraphicsDevice);
        Program.Check(!crt.IsActive(parameters), "CRT is inactive by default");
        CrtMaterial.Configure(parameters, 1f);
        Program.Check(crt.IsActive(parameters), "Aged CRT activates at non-zero intensity");
        parameters.Set(CrtMaterial.Noise, 0);
        parameters.Set(CrtMaterial.Vignette, 0);
        parameters.Set(CrtMaterial.Bloom, 0);
        crt.Render(new UIMaterialContext(GraphicsDevice, batch, source, destination, parameters, .2f));
        GraphicsDevice.SetRenderTarget(null);
        var colors = new Color[128 * 128]; destination.GetData(colors);
        Program.Check(colors[0].A == 0 && colors[64 * 128 + 64].A == 255, "CRT preserves transparency");
        Program.Check(colors[64 * 128 + 64] != GameThemes.DeepDrive.PrimaryText, "CRT changes fullscreen content");
        var center = colors[64 * 128 + 64].ToVector3();
        var nextRow = colors[65 * 128 + 64].ToVector3();
        var nextColumn = colors[64 * 128 + 65].ToVector3();
        Program.Check(Vector3.Distance(center, nextRow) > Vector3.Distance(center, nextColumn) * 4,
            "CRT scanlines vary across rows and therefore render horizontally");
        CrtMaterial.Configure(parameters, 0f);
        Program.Check(!crt.IsActive(parameters), "Zero intensity disables CRT processing");
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
                Program.Near(_menu.ScreenHost.Animation.Parameters.Get(CrtMaterial.Scanlines), 0, "CRT can be disabled fullscreen");
                _menu.CrtIntensityControl.Value = .5f;
                break;
            case 21:
                Program.Check(_menu.CrtEnabled, "Non-zero intensity checks CRT enablement");
                Program.Check(_menu.CrtControlVisible, "CRT progress-slider stays visible when enabled");
                Program.Near(Preferences.Get(PlayerPreferences.CrtIntensity), .5f, "Enabling CRT selects fifty percent");
                Program.Near(_menu.ScreenHost.Animation.Parameters.Get(CrtMaterial.Scanlines), .05f,
                    "Fifty percent uses the original aged strength");
                _menu.CrtIntensityControl.Value = 1;
                break;
            case 22:
                Program.Near(Preferences.Get(PlayerPreferences.CrtIntensity), 1, "Full aged CRT intensity persists immediately");
                Program.Near(_menu.ScreenHost.Animation.Parameters.Get(CrtMaterial.Scanlines), .1f,
                    "Full aged CRT is more pronounced");
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
                CrtMaterial.Configure(_fixture.Root.Animation.BaseParameters, 1f);
                CrtMaterial.Configure(_fixture.Other.Animation.BaseParameters, 0f);
                break;
            case 44:
                Program.Near(_fixture.Root.Animation.Parameters.Get(CrtMaterial.Scanlines), .1f, "Full aged CRT applies");
                Program.Near(_fixture.Other.Animation.Parameters.Get(CrtMaterial.Scanlines), 0, "Per-instance CRT override remains independent");
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
            case 69: _allocations = Ui.MaterialRenderer.AllocatedTargets; _menu.Dispose(); _menu = Ui.Open<MainMenuScreen>(); break;
            case 89:
                Program.Check(Ui.MaterialRenderer.AllocatedTargets <= _allocations, "Repeated screens do not accumulate targets");
                _menu.Dispose();
                var renderer = Ui.MaterialRenderer;
                Ui.Shutdown();
                Preferences.Shutdown();
                Program.Check(renderer.AllocatedTargets == 0, "All render targets released at shutdown");
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

        if (_frame is 4 or 8 or 12)
        {
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(GameThemes.DeepDrive.Background);
            var usage = GraphicsDevice.PresentationParameters.RenderTargetUsage;
            Ui.Draw();
            Program.Check(GraphicsDevice.PresentationParameters.RenderTargetUsage == usage,
                "Material rendering restores the back buffer usage policy");
            var pixels = new Color[Ui.ViewportWidth * Ui.ViewportHeight];
            GraphicsDevice.GetBackBufferData(pixels);
            using var capture = new Texture2D(GraphicsDevice, Ui.ViewportWidth, Ui.ViewportHeight);
            capture.SetData(pixels);
            using var file = File.Create(Path.Combine(_output, $"backbuffer-{_frame}.png"));
            capture.SaveAsPng(file, capture.Width, capture.Height);
            Program.Check(pixels[100 * Ui.ViewportWidth + 1000].A == 255,
                "Fullscreen CRT preserves opaque menu background coverage");
        }

        using var target = new RenderTarget2D(GraphicsDevice, Ui.ViewportWidth, Ui.ViewportHeight,
            false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        GraphicsDevice.SetRenderTarget(target);
        GraphicsDevice.Clear(GameThemes.DeepDrive.Background);
        var previous = GraphicsDevice.Viewport;
        Ui.Draw();
        Program.Check(GraphicsDevice.GetRenderTargets()[0].RenderTarget == target && GraphicsDevice.Viewport.Equals(previous), "Rendering restores target and viewport");
        GraphicsDevice.SetRenderTarget(null);
        if (new[] { 4, 20, 30, 42, 46, 56, 60, 65 }.Contains(_frame))
        {
            using var file = File.Create(Path.Combine(_output, $"frame-{_frame}.png"));
            target.SaveAsPng(file, target.Width, target.Height);
            var pixels = new Color[target.Width * target.Height]; target.GetData(pixels);
            Program.Check(pixels.Any(pixel => pixel.R > 150 && pixel.G > 150), "Rendered UI retains readable text");
            if (_frame == 42)
            {
                var inside = pixels[270 * target.Width + 240];
                var outside = pixels[270 * target.Width + 280];
                Program.Check(inside.R + inside.G + inside.B > outside.R + outside.G + outside.B,
                    "Material renders inside clipped panel and respects ancestor clipping");
            }
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
        Other = new UIMaterialHost(other, [MenuPresentation.Crt]);
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
        var clippedHost = new UIMaterialHost(clippedButton, [MenuPresentation.Crt]);
        CrtMaterial.Configure(clippedHost.Animation.BaseParameters, .4f);
        clipped.Widgets.Add(clippedHost);
        panel.Widgets.Add(clipped);
        Root = new UIMaterialHost(panel, [MenuPresentation.Crt], MenuPresentation.FadeStyle);
        CrtMaterial.Configure(Root.Animation.BaseParameters, 1f);
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

internal sealed class FailingScreen : UIScreen
{
    protected override Widget Build()
    {
        _ = new UIMaterialHost(Button.CreateTextButton("Unattached fixture"));
        throw new NotSupportedException("Deliberate build failure");
    }
}
