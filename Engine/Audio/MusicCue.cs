namespace Graphite.Engine.Audio;

public enum MusicTransition
{
    Immediate,
    NextBar
}

/// <summary>Aligned alternate mixes, not additive stems. Frame markers use the clips' sample rate.</summary>
public sealed class MusicCue
{
    public string Id
    {
        get;
    }
    public IReadOnlyList<AudioClip> Intensities
    {
        get;
    }
    public double BeatsPerMinute
    {
        get;
    }
    public int BeatsPerBar
    {
        get;
    }
    public int FirstBeatFrame
    {
        get;
    }
    public int LoopStartFrame
    {
        get;
    }
    public int LoopEndFrame
    {
        get;
    }
    public int SampleRate => Intensities[0].SampleRate;

    public MusicCue(string id, IEnumerable<AudioClip> intensities, double beatsPerMinute, int beatsPerBar = 4,
        int loopStartFrame = 0, int? loopEndFrame = null, int firstBeatFrame = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(intensities);
        var clips = intensities.ToArray();
        if (clips.Length == 0 || clips.Any(clip => clip is null) ||
            clips.Any(clip => clip.SampleRate != clips[0].SampleRate || clip.FrameCount != clips[0].FrameCount))
        {
            throw new ArgumentException("Music intensities must have matching sample rates and frame counts.", nameof(intensities));
        }
        if (!double.IsFinite(beatsPerMinute) || beatsPerMinute is <= 0 or > 1000 || beatsPerBar is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(beatsPerMinute));
        }
        var end = loopEndFrame ?? clips[0].FrameCount;
        if (loopStartFrame < 0 || end <= loopStartFrame || end > clips[0].FrameCount ||
            firstBeatFrame < 0 || firstBeatFrame >= end || clips[0].FrameCount - end > end - loopStartFrame)
        {
            throw new ArgumentException("Invalid loop/beat markers, or a reverb tail longer than the loop.");
        }
        Id = id;
        Intensities = Array.AsReadOnly(clips);
        BeatsPerMinute = beatsPerMinute;
        BeatsPerBar = beatsPerBar;
        LoopStartFrame = loopStartFrame;
        LoopEndFrame = end;
        FirstBeatFrame = firstBeatFrame;
    }

    internal float Sample(long elapsedOutputFrames, int channel, float intensity)
    {
        var sourceFrame = elapsedOutputFrames * (SampleRate / (double)MusicTransport.SampleRate);
        var loopLength = LoopEndFrame - LoopStartFrame;
        var position = sourceFrame;
        var tailPosition = -1d;
        if (sourceFrame >= LoopEndFrame)
        {
            var phase = (sourceFrame - LoopEndFrame) % loopLength;
            position = LoopStartFrame + phase;
            tailPosition = LoopEndFrame + phase;
        }
        var scaled = intensity * (Intensities.Count - 1);
        var low = (int)scaled;
        var high = Math.Min(low + 1, Intensities.Count - 1);
        var blend = scaled - low;
        var a = Read(Intensities[low]);
        var b = Read(Intensities[high]);
        return a + (b - a) * blend;

        float Read(AudioClip clip)
        {
            var sample = clip.Sample(position, channel);
            // Audio after LoopEndFrame is an authored tail, overlaid onto the next loop.
            if (tailPosition >= 0)
            {
                sample += clip.Sample(tailPosition, channel);
            }
            return sample;
        }
    }
}
