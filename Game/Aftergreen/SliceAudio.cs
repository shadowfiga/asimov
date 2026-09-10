using Microsoft.Xna.Framework.Audio;

namespace Graphite.Game.Aftergreen;

public sealed class SliceAudio : IDisposable
{
    private readonly Dictionary<string, SoundEffect> _sounds = [];
    private SoundEffectInstance? _vacuum;
    private bool _available = true;
    public bool Muted { get; set; }

    public SliceAudio()
    {
        try
        {
            _sounds["intake"] = Tone(420, .075f, .25f);
            _sounds["paper"] = Tone(750, .06f, .1f);
            _sounds["release"] = Tone(110, .28f, .35f);
            _sounds["deposit"] = Tone(220, .4f, .22f);
            _sounds["bot"] = Tone(960, .1f, .13f);
            _sounds["relic"] = Tone(660, .9f, .2f);
            _sounds["upgrade"] = Tone(520, .35f, .2f);
            _sounds["notice"] = Tone(340, .13f, .12f);
            _sounds["departure"] = Tone(65, 1.5f, .3f);
            _sounds["vacuum"] = Tone(80, 1, .035f, true);
            _vacuum = _sounds["vacuum"].CreateInstance();
            _vacuum.IsLooped = true;
        }
        catch (Exception ex) when (ex is NoAudioHardwareException or InvalidOperationException)
        {
            _available = false;
        }
    }

    private static SoundEffect Tone(float hz, float duration, float volume, bool loop = false)
    {
        const int rate = 22050;
        var count = (int)(rate * duration);
        var bytes = new byte[count * 2];
        for (var i = 0; i < count; i++)
        {
            var time = (float)i / rate;
            var envelope = loop ? 1 : MathF.Min(1, time * 80) * MathF.Pow(1 - (float)i / count, 1.7f);
            var wave = MathF.Sin(MathF.Tau * hz * time) + .25f * MathF.Sin(MathF.Tau * hz * 2 * time);
            var sample = (short)(wave * envelope * volume * short.MaxValue);
            bytes[i * 2] = (byte)sample;
            bytes[i * 2 + 1] = (byte)(sample >> 8);
        }
        return new SoundEffect(bytes, rate, AudioChannels.Mono);
    }

    public void Play(string kind)
    {
        if (_available && !Muted && _sounds.TryGetValue(kind, out var sound))
        {
            sound.Play();
        }
    }

    public void Update(bool vacuum)
    {
        if (!_available || _vacuum == null)
        {
            return;
        }
        if (vacuum && !Muted && _vacuum.State != SoundState.Playing)
        {
            _vacuum.Play();
        }
        else if ((!vacuum || Muted) && _vacuum.State == SoundState.Playing)
        {
            _vacuum.Stop();
        }
    }

    public void Dispose()
    {
        _vacuum?.Dispose();
        foreach (var sound in _sounds.Values)
        {
            sound.Dispose();
        }
    }
}
