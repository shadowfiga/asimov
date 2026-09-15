using System.Globalization;
using Graphite.Engine.Graphics;
using Graphite.Engine.Persistence;
using Graphite.Game.Configuration;
using Graphite.Game.Graphics;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI.Settings;

internal sealed class AccessibilitySettingsPage : SettingsPage
{
    private readonly ComboView _scale;
    private readonly PercentageControl _crt;
    internal ComboView ScaleControl => _scale;
    internal HorizontalSlider Crt => _crt.Slider;

    internal AccessibilitySettingsPage(MenuAssets assets) : base("ACCESSIBILITY")
    {
        Widgets.Add(SettingsControls.Dropdown(assets, "UI SCALE",
            RuntimePreferences.UiScaleOptions.Select(value => value.ToString("0.00", CultureInfo.InvariantCulture) + "×"),
            out _scale));
        _crt = new PercentageControl(Preferences.Get(PlayerPreferences.CrtIntensity), value =>
        {
            Preferences.Set(PlayerPreferences.CrtIntensity, value);
            CrtFilter.Configure(PostProcessing.Parameters, value);
        });
        Widgets.Add(SettingsControls.Row("CRT EFFECT", _crt));
        Synchronize();
        _scale.SelectedIndexChanged += (_, _) =>
        {
            if (_scale.SelectedIndex is { } index)
            {
                Preferences.Set(RuntimePreferences.UiScale, RuntimePreferences.UiScaleOptions[index]);
            }
        };
    }

    internal override void Synchronize()
    {
        var scale = RuntimePreferences.GetUiScale();
        _scale.SelectedIndex = Enumerable.Range(0, RuntimePreferences.UiScaleOptions.Count)
            .First(index => RuntimePreferences.UiScaleOptions[index] == scale);
        _crt.Slider.Value = Preferences.Get(PlayerPreferences.CrtIntensity);
    }
}
