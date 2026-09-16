using Microsoft.Xna.Framework;

namespace Graphite.Engine.Audio;

/// <summary>Scene/entity ownership. Disposing stops owned voices and unregisters owned ambience zones.</summary>
public sealed class AudioScope : IDisposable
{
    private readonly List<IDisposable> _owned = [];
    private bool _disposed;

    public AudioVoice Play(AudioClip clip, AudioPlayOptions? options = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Own(AudioManager.Current.Play(clip, options));
    }

    public AudioVoice Play2D(AudioClip clip, Vector2 position, SpatialAudioSettings? spatial = null, AudioPlayOptions? options = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Own(AudioManager.Current.Play2D(clip, position, spatial, options));
    }

    public T Own<T>(T resource) where T : IDisposable
    {
        ArgumentNullException.ThrowIfNull(resource);
        ObjectDisposedException.ThrowIf(_disposed, this);
        _owned.RemoveAll(item => item is AudioVoice { IsPlaying: false });
        _owned.Add(resource);
        return resource;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        foreach (var resource in _owned)
        {
            resource.Dispose();
        }
        _owned.Clear();
    }
}
