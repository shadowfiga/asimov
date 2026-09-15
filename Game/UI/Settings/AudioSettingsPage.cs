using Graphite.Engine.Persistence;
using Graphite.Game.Configuration;
using Myra.Graphics2D.UI;

namespace Graphite.Game.UI.Settings;

internal sealed class AudioSettingsPage : SettingsPage
{
    private readonly PercentageControl _volume;
    private readonly PercentageControl _music;
    private readonly PercentageControl _fx;
    internal HorizontalSlider Volume => _volume.Slider;
    internal HorizontalSlider Music => _music.Slider;
    internal HorizontalSlider Fx => _fx.Slider;

    internal AudioSettingsPage() : base("AUDIO")
    {
        _volume = VolumeControl(PlayerPreferences.MasterVolume);
        _music = VolumeControl(PlayerPreferences.MusicVolume);
        _fx = VolumeControl(PlayerPreferences.FxVolume);
        Widgets.Add(SettingsControls.Row("MASTER VOLUME", _volume));
        Widgets.Add(SettingsControls.Row("MUSIC VOLUME", _music));
        Widgets.Add(SettingsControls.Row("FX VOLUME", _fx));
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
    }
}
