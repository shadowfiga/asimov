using System.Buffers.Binary;
using System.Text;

namespace Graphite.Engine.Audio;

/// <summary>Immutable decoded PCM. Load during preload, not in a combat update.</summary>
public sealed class AudioClip
{
    private readonly float[] _samples;
    public int SampleRate
    {
        get;
    }
    public int Channels
    {
        get;
    }
    public int FrameCount => _samples.Length / Channels;
    public TimeSpan Duration => TimeSpan.FromSeconds(FrameCount / (double)SampleRate);

    public AudioClip(ReadOnlySpan<float> samples, int sampleRate, int channels)
    {
        if (sampleRate is < 8000 or > 192000 || channels is < 1 or > 2 || samples.IsEmpty || samples.Length % channels != 0)
        {
            throw new ArgumentException("Audio requires nonempty mono/stereo PCM at 8–192 kHz.");
        }
        _samples = samples.ToArray();
        if (_samples.Any(sample => !float.IsFinite(sample) || MathF.Abs(sample) > 1))
        {
            throw new ArgumentException("PCM samples must be finite and between -1 and 1.", nameof(samples));
        }
        SampleRate = sampleRate;
        Channels = channels;
    }

    /// <summary>RIFF WAV: PCM 8/16/24/32-bit or IEEE float32, including extensible WAV.</summary>
    public static AudioClip ReadWave(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
        if (new string(reader.ReadChars(4)) != "RIFF")
        {
            throw new InvalidDataException("Expected a RIFF WAV file.");
        }
        var remaining = (long)reader.ReadUInt32() - 4;
        if (new string(reader.ReadChars(4)) != "WAVE" || remaining < 0)
        {
            throw new InvalidDataException("Expected a WAVE container.");
        }
        byte[] format = [];
        byte[] data = [];
        while (remaining >= 8)
        {
            var id = new string(reader.ReadChars(4));
            var size = reader.ReadUInt32();
            var padded = (long)size + (size & 1);
            remaining -= 8;
            if (padded > remaining || size > 512 * 1024 * 1024)
            {
                throw new InvalidDataException("Invalid or oversized WAV chunk.");
            }
            var bytes = reader.ReadBytes((int)size);
            if (bytes.Length != size)
            {
                throw new EndOfStreamException("Truncated WAV chunk.");
            }
            if (id == "fmt ")
            {
                format = bytes;
            }
            else if (id == "data")
            {
                data = bytes;
            }
            if ((size & 1) != 0)
            {
                reader.ReadByte();
            }
            remaining -= padded;
        }
        if (format.Length < 16 || data.Length == 0)
        {
            throw new InvalidDataException("WAV requires format and audio data chunks.");
        }
        var encoding = BinaryPrimitives.ReadUInt16LittleEndian(format);
        var channels = BinaryPrimitives.ReadUInt16LittleEndian(format.AsSpan(2));
        var rate = BinaryPrimitives.ReadInt32LittleEndian(format.AsSpan(4));
        var alignment = BinaryPrimitives.ReadUInt16LittleEndian(format.AsSpan(12));
        var bits = BinaryPrimitives.ReadUInt16LittleEndian(format.AsSpan(14));
        if (encoding == 0xfffe)
        {
            if (format.Length < 40 || BinaryPrimitives.ReadUInt16LittleEndian(format.AsSpan(16)) < 22)
            {
                throw new InvalidDataException("Invalid extensible WAV format.");
            }
            var subformat = new Guid(format.AsSpan(24, 16));
            encoding = subformat == new Guid("00000001-0000-0010-8000-00aa00389b71") ? (ushort)1
                : subformat == new Guid("00000003-0000-0010-8000-00aa00389b71") ? (ushort)3 : (ushort)0;
        }
        if (channels is < 1 or > 2 || rate is < 8000 or > 192000 ||
            !((encoding == 1 && bits is 8 or 16 or 24 or 32) || (encoding == 3 && bits == 32)) ||
            alignment != channels * (bits / 8) || data.Length % alignment != 0)
        {
            throw new NotSupportedException("Use mono/stereo PCM 8/16/24/32-bit or float32 WAV at 8–192 kHz.");
        }
        var stride = bits / 8;
        var samples = new float[data.Length / stride];
        for (var i = 0; i < samples.Length; i++)
        {
            var bytes = data.AsSpan(i * stride, stride);
            var value = encoding == 3 ? BinaryPrimitives.ReadSingleLittleEndian(bytes) : bits switch
            {
                8 => (bytes[0] - 128) / 128f,
                16 => BinaryPrimitives.ReadInt16LittleEndian(bytes) / 32768f,
                24 => ((bytes[0] | bytes[1] << 8 | bytes[2] << 16) << 8 >> 8) / 8388608f,
                _ => (float)(BinaryPrimitives.ReadInt32LittleEndian(bytes) / 2147483648d)
            };
            if (!float.IsFinite(value))
            {
                throw new InvalidDataException("Non-finite WAV sample.");
            }
            samples[i] = Math.Clamp(value, -1, 1);
        }
        return new AudioClip(samples, rate, channels);
    }

    internal float Sample(double frame, int channel)
    {
        var index = (int)frame;
        if (index < 0 || index >= FrameCount)
        {
            return 0;
        }
        var selected = Math.Min(channel, Channels - 1);
        var a = _samples[index * Channels + selected];
        var b = _samples[Math.Min(index + 1, FrameCount - 1) * Channels + selected];
        return a + (b - a) * (float)(frame - index);
    }

    internal byte[] ToPcm16(bool mono = false, int? sampleRate = null)
    {
        var rate = sampleRate ?? SampleRate;
        var channels = mono ? 1 : Channels;
        var frames = (int)Math.Ceiling(FrameCount * (rate / (double)SampleRate));
        var count = frames * channels;
        var result = new byte[count * 2];
        for (var i = 0; i < count; i++)
        {
            var position = (i / channels) * (SampleRate / (double)rate);
            var value = mono && Channels == 2
                ? (Resampled(position, 0, rate) + Resampled(position, 1, rate)) * .5f
                : Resampled(position, i % channels, rate);
            BinaryPrimitives.WriteInt16LittleEndian(result.AsSpan(i * 2), ToInt16(value));
        }
        return result;
    }

    // Offline low-pass resampling for high-rate SFX. Never run this in the real-time mixer.
    private float Resampled(double position, int channel, int rate)
    {
        if (rate >= SampleRate)
        {
            return Sample(position, channel);
        }
        var cutoff = rate / (double)SampleRate;
        var radius = 16 / cutoff;
        double total = 0;
        double weights = 0;
        for (var frame = (int)Math.Ceiling(position - radius); frame <= position + radius; frame++)
        {
            var distance = frame - position;
            var phase = Math.PI * distance * cutoff;
            var sinc = Math.Abs(phase) < .0000001 ? 1 : Math.Sin(phase) / phase;
            var weight = sinc * .5 * (1 + Math.Cos(Math.PI * distance / radius));
            total += _samples[Math.Clamp(frame, 0, FrameCount - 1) * Channels + channel] * weight;
            weights += weight;
        }
        return (float)(total / weights);
    }

    internal static short ToInt16(float value) => (short)Math.Clamp((int)MathF.Round(value * 32768), short.MinValue, short.MaxValue);
}
