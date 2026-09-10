using Graphite.Engine.Configuration;
using Graphite.Engine.Platform;
using Graphite.Engine.Scenes;
using Graphite.Engine.UI;
using Microsoft.Xna.Framework;

namespace Graphite.Engine.Core;

public sealed class GameHost : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly GraphiteSettings _settings;
    private readonly Color _clearColor;

    public GameHost()
    {
        _settings = GraphiteSettings.Load();
        _clearColor = _settings.Graphics.ParseClearColor();
        _graphics = new GraphicsDeviceManager(this)
        {
            HardwareModeSwitch = !_settings.Window.Borderless,
            IsFullScreen = _settings.Window.Fullscreen,
            PreferMultiSampling = _settings.Graphics.PreferMultiSampling,
            PreferredBackBufferWidth = _settings.Window.Width,
            PreferredBackBufferHeight = _settings.Window.Height,
            SynchronizeWithVerticalRetrace = _settings.Graphics.VSync
        };

        Content.RootDirectory = _settings.Content.RootDirectory;
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

        var iconPath = _settings.ResolveIconPath();
        if (iconPath is not null)
        {
            WindowIcon.Apply(Window, GraphicsDevice, iconPath);
        }

        Graphite.Engine.UI.UI.Initialize(this);
        Application.Reset();
        SceneManager.Initialize();
        SceneManager.Load(_settings.Game.StartupScene);
        SceneManager.CommitPendingChanges();
    }

    protected override void Update(GameTime gameTime)
    {
        if (Input.ShouldExit() || Application.IsQuitRequested)
        {
            Exit();
        }

        Time.Update(gameTime);
        SceneManager.Update(Time.DeltaTime);
        SceneManager.CommitPendingChanges();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(_clearColor);

        SceneManager.Draw(gameTime);
        Graphite.Engine.UI.UI.Draw();

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SceneManager.Shutdown();
            Graphite.Engine.UI.UI.Shutdown();
        }

        base.Dispose(disposing);
    }
}
