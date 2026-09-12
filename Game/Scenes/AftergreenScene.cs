using Graphite.Engine.Core;
using Graphite.Engine.Scenes;
using Graphite.Game.Aftergreen;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Graphite.Game.Scenes;

public sealed class AftergreenScene : Scene
{
    private SliceController _controller = null!;
    protected internal override void OnLoad()
    {
        _controller = Create("Aftergreen").Add<SliceController>();
    }

    protected internal override void Draw(GameTime gameTime) => _controller.Render();
}

public sealed class SliceController : Behaviour
{
    private SliceState _state = null!;
    private SliceRenderer _renderer = null!;
    private SliceAudio _audio = null!;
    private KeyboardState _previous;
    private MouseState _previousMouse;
    private float _saveTime;
    private SlicePhase _resumePhase;
    private bool _started;
    private bool _resetConfirmation;
    private int _renderFrame;

    protected internal override void Start()
    {
        SliceSave? save = null;
        string? error = null;
        try
        {
            save = SliceDiagnostics.RenderDirectory == null ? SliceState.LoadSave() : null;
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or InvalidDataException)
        {
            error = "Save could not be loaded. A new expedition has started.";
        }
        _state = new(SliceConfig.Load(), save);
        _renderer = new(_state);
        _audio = new();
        _state.Feedback += OnFeedback;
        _resumePhase = _state.Phase;
        _previous = Keyboard.GetState();
        _previousMouse = Mouse.GetState();
        if (error != null)
        {
            _state.Notify(error);
        }
        _started = true;
    }

    private void OnFeedback(Feedback feedback)
    {
        _renderer.Burst(feedback);
        _audio.Play(feedback.Kind);
    }

    private void Save()
    {
        if (SliceDiagnostics.RenderDirectory != null)
        {
            return;
        }
        try
        {
            _state.Save();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _state.Notify("Could not save to disk. Your expedition is still running.");
        }
    }

    protected internal override void Update(float dt)
    {
        if (!Myra.MyraEnvironment.Game.IsActive && SliceDiagnostics.RenderDirectory == null)
        {
            if (_state.Phase != SlicePhase.Paused)
            {
                _resumePhase = _state.Phase;
                _state.Phase = SlicePhase.Paused;
                Save();
            }
            _audio.Update(false);
            return;
        }
        var key = Keyboard.GetState();
        var mouse = Mouse.GetState();
        bool Press(Keys k) => key.IsKeyDown(k) && !_previous.IsKeyDown(k);
        var click = mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
        var action = click ? _renderer.Hit(new(mouse.X, mouse.Y)) : null;
        if (Press(Keys.M))
        {
            _audio.Muted = !_audio.Muted;
        }
        if (_state.Card != null)
        {
            if (Press(Keys.Enter) || Press(Keys.Escape) || action == "close")
            {
                _state.Card = null;
                Save();
            }
        }
        else if (_state.Phase == SlicePhase.Paused)
        {
            if (action == "menu")
            {
                SceneManager.Load<MainMenuScene>();
                return;
            }
            if (Press(Keys.Enter) || Press(Keys.Escape) || action == "resume")
            {
                _state.Phase = _resumePhase;
                _resetConfirmation = false;
            }
            if (action == "reset")
            {
                if (_resetConfirmation)
                {
                    _state.Feedback -= OnFeedback;
                    _renderer.Dispose();
                    _state = new(SliceConfig.Load());
                    _renderer = new(_state);
                    _state.Feedback += OnFeedback;
                    _resetConfirmation = false;
                    Save();
                }
                else
                {
                    _resetConfirmation = true;
                }
            }
        }
        else if (Press(Keys.Escape))
        {
            if (_state.Phase is SlicePhase.Workshop or SlicePhase.Biodome)
            {
                _state.Phase = _state.Data.Departures > 0 ? SlicePhase.Teaser : SlicePhase.Exterior;
            }
            else if (_state.Phase != SlicePhase.Departure)
            {
                _resumePhase = _state.Phase;
                _state.Phase = SlicePhase.Paused;
                Save();
            }
        }
        else
        {
            if (Press(Keys.E))
            {
                _state.Interact();
            }
            if (_state.Phase == SlicePhase.Workshop)
            {
                for (var i = 0; i < 3; i++)
                {
                    if (Press(Keys.D1 + i) || action == "upgrade" + i)
                    {
                        _state.BuyUpgrade(i);
                    }
                }
                if (Press(Keys.D4) || action == "bot")
                {
                    _state.BuildBot();
                }
                if (Press(Keys.B) || action == "dome")
                {
                    _state.EnterBiodome();
                }
            }
            else if (_state.Phase == SlicePhase.Biodome)
            {
                if (Press(Keys.D1) || action == "finch")
                {
                    _state.ChooseEcology(Ecology.PaperFinch);
                    Save();
                }
                if (Press(Keys.D2) || action == "grass")
                {
                    _state.ChooseEcology(Ecology.ScrubGrass);
                    Save();
                }
                if (action == "depart")
                {
                    _state.BeginDeparture();
                    Save();
                }
            }
            if (action == "back")
            {
                _state.Phase = _state.Data.Departures > 0 ? SlicePhase.Teaser : SlicePhase.Exterior;
                Save();
            }
            if (action == "explore")
            {
                _state.Phase = SlicePhase.Teaser;
            }
        }
        var movement = new Vector2((key.IsKeyDown(Keys.D) || key.IsKeyDown(Keys.Right) ? 1 : 0) - (key.IsKeyDown(Keys.A) || key.IsKeyDown(Keys.Left) ? 1 : 0),
            (key.IsKeyDown(Keys.S) || key.IsKeyDown(Keys.Down) ? 1 : 0) - (key.IsKeyDown(Keys.W) || key.IsKeyDown(Keys.Up) ? 1 : 0));
        _state.Update(dt, new(movement, _renderer.ScreenToWorld(new(mouse.X, mouse.Y)) - _state.Player,
            mouse.LeftButton == ButtonState.Pressed || key.IsKeyDown(Keys.Space), mouse.RightButton == ButtonState.Pressed || key.IsKeyDown(Keys.LeftShift)));
        _audio.Update(_state.VacuumActive);
        _renderer.Update(dt);
        _renderer.ResetConfirmation = _resetConfirmation;
        _renderer.Muted = _audio.Muted;
        _saveTime += dt;
        if (_saveTime > 10)
        {
            Save();
            _saveTime = 0;
        }
        _previous = key;
        _previousMouse = mouse;
    }

    public void Render()
    {
        if (_started)
        {
            if (SliceDiagnostics.RenderDirectory is { } directory)
            {
                var names = new[] { "title", "exterior", "workshop", "relic", "biodome", "finch", "grass", "departure", "complete" };
                _state.Card = null;
                _state.Phase = _renderFrame switch
                {
                    0 => SlicePhase.Paused,
                    1 => SlicePhase.Exterior,
                    2 => SlicePhase.Workshop,
                    3 => SlicePhase.Exterior,
                    4 or 5 or 6 => SlicePhase.Biodome,
                    7 => SlicePhase.Departure,
                    _ => SlicePhase.Complete
                };
                if (_renderFrame == 2)
                {
                    _state.Data.Scrap = 350;
                    _state.Data.Components = 3;
                    _state.Data.RoutingChip = true;
                }
                if (_renderFrame == 3)
                {
                    _state.Card = "relic";
                }
                if (_renderFrame == 5)
                {
                    _state.Data.Ecology = Ecology.PaperFinch;
                }
                if (_renderFrame == 6)
                {
                    _state.Data.Ecology = Ecology.ScrubGrass;
                }
                _renderer.Draw(Path.Combine(directory, names[_renderFrame] + ".png"));
                _renderFrame++;
                if (_renderFrame == names.Length)
                {
                    Myra.MyraEnvironment.Game.Exit();
                }
            }
            else
            {
                _renderer.Draw();
            }
        }
    }

    protected internal override void OnDestroy()
    {
        if (!_started)
        {
            return;
        }
        Save();
        _state.Feedback -= OnFeedback;
        _renderer.Dispose();
        _audio.Dispose();
    }
}
