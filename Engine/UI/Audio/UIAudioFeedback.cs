using Myra.Events;
using Myra.Graphics2D.UI;

namespace Graphite.Engine.UI.Audio;

/// <summary>Optional one-shot control sounds, independent of material/rendering wrappers.</summary>
public sealed class UIAudioFeedback : IDisposable
{
    private readonly ButtonBase _button;
    private readonly UISoundCue? _hover;
    private readonly UISoundCue? _click;

    public UIAudioFeedback(ButtonBase button, UISoundCue? hover = null, UISoundCue? click = null)
    {
        ArgumentNullException.ThrowIfNull(button);
        Validate(hover);
        Validate(click);
        _button = button;
        _hover = hover;
        _click = click;
        button.MouseEntered += Hover;
        button.KeyboardFocusChanged += Focus;
        button.Click += Click;
    }

    private static void Validate(UISoundCue? cue)
    {
        if (cue is not null && (cue.Loop || cue.Delay != 0))
        {
            throw new ArgumentException("Control feedback requires immediate, non-looping sound cues.", nameof(cue));
        }
    }

    private void Hover(object sender, MyraEventArgs args) => Play(_hover);
    private void Focus(object sender, MyraEventArgs args)
    {
        if (_button.IsKeyboardFocused && !_button.IsMouseInside)
        {
            Play(_hover);
        }
    }
    private void Click(object sender, MyraEventArgs args) => Play(_click);

    private void Play(UISoundCue? cue)
    {
        if (cue is null)
        {
            return;
        }
        for (Widget? parent = _button; parent is not null; parent = parent.Parent)
        {
            if (!parent.Enabled || !parent.Visible)
            {
                return;
            }
        }
        // UIAudioService owns the one-shot so scene/dialog teardown doesn't cut off a click.
        UI.Audio.Play(cue);
    }

    public void Dispose()
    {
        _button.MouseEntered -= Hover;
        _button.KeyboardFocusChanged -= Focus;
        _button.Click -= Click;
    }
}
