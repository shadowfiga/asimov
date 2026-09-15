using Graphite.Engine.Configuration;
using Graphite.Engine.Graphics;
using Graphite.Engine.Platform;
using Graphite.Engine.Persistence;
using Graphite.Engine.Scenes;
using Microsoft.Xna.Framework;

namespace Graphite.Engine.Core;

public sealed class GameHost : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly Settings _settings;
    private readonly Color _clearColor;
    private readonly Action? _initialize;

    public GameHost(Settings settings, Action? initialize = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.Validate();
        _settings = settings;
        _initialize = initialize;
        _clearColor = _settings.Graphics.ClearColor;
        _graphics = new GraphicsDeviceManager(this)
        {
            HardwareModeSwitch = !_settings.Window.Borderless,
            IsFullScreen = _settings.Window.Fullscreen,
            PreferMultiSampling = _settings.Graphics.PreferMultiSampling,
            PreferredBackBufferWidth = _settings.Window.Width,
            PreferredBackBufferHeight = _settings.Window.Height,
            SynchronizeWithVerticalRetrace = _settings.Graphics.VSync
        };

        IsFixedTimeStep = _settings.Runtime.FixedTimeStep;
        IsMouseVisible = _settings.Runtime.MouseVisible;
        TargetElapsedTime = TimeSpan.FromSeconds(1d / _settings.Runtime.TargetFramesPerSecond);
        Window.AllowUserResizing = _settings.Window.Resizable;
        Window.IsBorderless = _settings.Window.Borderless;
        Window.Title = _settings.Game.Name;
    }

    protected override void Initialize()
    {
        base.Initialize();

        var iconPath = _settings.IconPath;
        if (iconPath is not null)
        {
            WindowIcon.Apply(Window, GraphicsDevice, iconPath);
        }

        Graphite.Engine.UI.UI.Initialize(this);
        PostProcessing.Initialize(GraphicsDevice);
        Application.Reset();
        var directory = PersistencePaths.ForGame(_settings.Game.Id, _settings.Environment);
        Storage.Initialize(Path.Combine(directory, "saves"));
        var preferences = Preferences.Initialize(directory);
        if (preferences.Status is not (SaveStatus.Success or SaveStatus.NotFound))
        {
            Console.Error.WriteLine($"Could not load preferences: {preferences.Error}");
        }

        _initialize?.Invoke();
        SceneManager.Load(_settings.Game.StartupScene);
        SceneManager.CommitPendingChanges();
    }

    protected override void Update(GameTime gameTime)
    {
        if (Input.ShouldExit() || Application.IsQuitRequested)
        {
            Exit();
        }

        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Graphite.Engine.UI.UI.Update(dt);
        SceneManager.Update(dt);
        SceneManager.CommitPendingChanges();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        PostProcessing.Render(gameTime, _clearColor, DrawFrame);
    }

    private void DrawFrame(GameTime gameTime)
    {
        SceneManager.Draw(gameTime);
        base.Draw(gameTime);
        Graphite.Engine.UI.UI.Draw();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                SceneManager.Shutdown();
                Graphite.Engine.UI.UI.Shutdown();
            }
            finally
            {
                PostProcessing.Shutdown();
                Preferences.Shutdown();
                Storage.Shutdown();
            }
        }

        base.Dispose(disposing);
    }
}
