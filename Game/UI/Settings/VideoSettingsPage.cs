using Graphite.Engine.Graphics;
using Graphite.Game.UI.Theming;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI.Settings;

internal sealed class VideoSettingsPage : SettingsPage
{
    private readonly ComboView _mode;
    private readonly ComboView _resolution;
    private readonly Label _error;
    private Point[] _resolutions = [];
    private bool _syncing;
    internal ComboView Mode => _mode;
    internal ComboView Resolution => _resolution;

    internal VideoSettingsPage(MenuAssets assets) : base("VIDEO")
    {
        Widgets.Add(SettingsControls.Dropdown(assets, "DISPLAY MODE",
            ["WINDOWED", "BORDERLESS FULLSCREEN", "FULLSCREEN"], out _mode));
        Widgets.Add(SettingsControls.Dropdown(assets, "RESOLUTION", [], out _resolution));
        _error = new Label { Font = ThemeAssets.Font(16), TextColor = GameThemes.DeepDrive.SelectionHighlight, Visible = false };
        Widgets.Add(_error);
        Synchronize();
        _mode.SelectedIndexChanged += (_, _) =>
        {
            if (!_syncing && _mode.SelectedIndex is { } index)
            {
                DisplaySettings.Preview((WindowMode)index, DisplaySettings.Current.Resolution);
            }
        };
        _resolution.SelectedIndexChanged += (_, _) =>
        {
            if (!_syncing && _resolution.SelectedIndex is { } index && index >= 0 && index < _resolutions.Length)
            {
                DisplaySettings.Preview(DisplaySettings.Current.Mode, _resolutions[index]);
            }
        };
    }

    internal override void Synchronize()
    {
        _syncing = true;
        try
        {
            var current = DisplaySettings.Current;
            _mode.SelectedIndex = (int)current.Mode;
            var sizes = DisplaySettings.Resolutions(current.Mode).ToArray();
            if (!_resolutions.SequenceEqual(sizes))
            {
                _resolutions = sizes;
                _resolution.Widgets.Clear();
                foreach (var size in sizes)
                {
                    _resolution.Widgets.Add(SettingsControls.Option($"{size.X} × {size.Y}"));
                }
            }
            var index = Array.IndexOf(_resolutions, current.Resolution);
            _resolution.SelectedIndex = index >= 0 ? index : null;
            _resolution.Enabled = current.Mode != WindowMode.BorderlessFullscreen;
            _resolution.Tooltip = _resolution.Enabled ? "" : "Borderless fullscreen uses the desktop resolution.";
            _error.Text = DisplaySettings.Error ?? "";
            _error.Visible = DisplaySettings.Error is not null;
        }
        finally
        {
            _syncing = false;
        }
    }

    internal override void Attach()
    {
        base.Attach();
        DisplaySettings.Changed += Synchronize;
    }

    public override void Dispose()
    {
        base.Dispose();
        DisplaySettings.Changed -= Synchronize;
    }
}
