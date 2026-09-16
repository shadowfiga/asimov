using Graphite.Engine.Persistence;
using Graphite.Engine.Audio;
using Graphite.Game.Configuration;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI.Settings;

internal sealed class AudioSettingsPage : SettingsPage
{
    private readonly PercentageControl _volume;
    private readonly PercentageControl _music;
    private readonly PercentageControl _fx;
    private readonly PercentageControl _ambience;
    private readonly PercentageControl _ui;
    internal HorizontalSlider Volume => _volume.Slider;
    internal HorizontalSlider Music => _music.Slider;
    internal HorizontalSlider Fx => _fx.Slider;

    internal AudioSettingsPage() : base("AUDIO")
    {
        _volume = VolumeControl(PlayerPreferences.MasterVolume);
        _music = VolumeControl(PlayerPreferences.MusicVolume);
        _fx = VolumeControl(PlayerPreferences.FxVolume);
        _ambience = VolumeControl(AudioPreferences.AmbienceVolume);
        _ui = VolumeControl(AudioPreferences.UiVolume);
        Widgets.Add(SettingsControls.Row("MASTER VOLUME", _volume));
        Widgets.Add(SettingsControls.Row("MUSIC VOLUME", _music));
        Widgets.Add(SettingsControls.Row("FX VOLUME", _fx));
        Widgets.Add(SettingsControls.Row("AMBIENCE VOLUME", _ambience));
        Widgets.Add(SettingsControls.Row("UI VOLUME", _ui));
    }

    private static PercentageControl VolumeControl(PreferenceKey<float> key) =>
        new(Preferences.Get(key), value =>
        {
            Preferences.Set(key, value);
            PlayerPreferences.ApplyAudio();
        });

    internal override void Synchronize()
    {
        _volume.Slider.Value = Preferences.Get(PlayerPreferences.MasterVolume);
        _music.Slider.Value = Preferences.Get(PlayerPreferences.MusicVolume);
        _fx.Slider.Value = Preferences.Get(PlayerPreferences.FxVolume);
        _ambience.Slider.Value = Preferences.Get(AudioPreferences.AmbienceVolume);
        _ui.Slider.Value = Preferences.Get(AudioPreferences.UiVolume);
    }
}
