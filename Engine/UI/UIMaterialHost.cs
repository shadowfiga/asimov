using Graphite.Engine.UI.Animation;
using Graphite.Engine.UI.Materials;
using Microsoft.Xna.Framework;
using Myra.Events;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Graphite.Engine.UI;

/// <summary>Place the host in the layout and retain Content for its normal control API.</summary>
public sealed class UIMaterialHost : Panel, IDisposable
{
    private readonly IReadOnlyList<UIMaterial> _definitions;
    private readonly List<UIMaterialInstance> _materials = [];
    private readonly Dictionary<string, UIPlayback> _bindings = [];
    private readonly UIInteractionStyle _style;
    private readonly UIMaterialRenderer.Surface _surface = new();
    private bool _initialized, _presented, _wasPlaced, _disposed, _previousEnabled;
    private bool _enabledBeforeHide;
    private UIPlayback? _exit;
    private TaskCompletionSource<UIPlaybackState>? _hiding;
    public Widget Content
    {
        get;
    }
    public UIAnimationPlayer Animation
    {
        get;
    }
    public int OverflowPadding
    {
        get; init;
    }
    public bool IsHiding => _hiding is not null;
    public bool IsDisposed => _disposed;

    public UIMaterialHost(Widget content, UIInteractionStyle? interactions = null)
        : this(content, [], interactions) { }

    public UIMaterialHost(Widget content, IReadOnlyList<UIMaterial> materials, UIInteractionStyle? interactions = null)
        : base(new Stylesheet { PanelStyle = new WidgetStyle() }, null)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(materials);
        _ = UI.MaterialRenderer;
        if (content.Parent is not null || content.Desktop is not null)
        {
            throw new InvalidOperationException("Wrap a widget before adding it to a layout.");
        }

        Content = content;
        _definitions = materials.ToArray();
        _style = UI.Interactions.Resolve(content.GetType(), interactions);
        if (_style.Bindings.TryGetValue(UITrigger.Hide, out var hide) && hide.CreateAnimation()?.IsInfinite == true)
        {
            throw new ArgumentException("Hide animations and their sound cues must finish.", nameof(interactions));
        }

        foreach (var binding in _style.Bindings.Values)
        {
            binding.CreateAnimation()?.Validate();
            if (!float.IsFinite(binding.SettleSeconds) || binding.SettleSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(interactions), "Settle time must be finite and nonnegative.");
            }
        }
        Animation = new UIAnimationPlayer(UI.Audio);
        // The outer layout belongs to the host. The original control fills that slot.
        Width = content.Width;
        Height = content.Height;
        MinWidth = content.MinWidth;
        MinHeight = content.MinHeight;
        MaxWidth = content.MaxWidth;
        MaxHeight = content.MaxHeight;
        HorizontalAlignment = content.HorizontalAlignment;
        VerticalAlignment = content.VerticalAlignment;
        Margin = content.Margin;
        Left = content.Left;
        Top = content.Top;
        Visible = content.Visible;
        content.Width = content.Height = content.MinWidth = content.MinHeight = content.MaxWidth = content.MaxHeight = null;
        content.HorizontalAlignment = HorizontalAlignment.Stretch;
        content.VerticalAlignment = VerticalAlignment.Stretch;
        content.Margin = new Thickness(0);
        content.Left = content.Top = 0;
        content.Visible = true;
        Widgets.Add(content);
        content.MouseEntered += Hover;
        content.MouseLeft += HoverExit;
        content.KeyboardFocusChanged += FocusChanged;
        content.PressedChanged += HandlePressedChanged;
        if (content is ButtonBase button)
        {
            button.Click += Click;
        }

        if (content is ListView list)
        {
            list.SelectedIndexChanged += SelectionChanged;
        }

        if (content is ComboView combo)
        {
            combo.SelectedIndexChanged += SelectionChanged;
        }

        if (content is TabControl tabs)
        {
            tabs.SelectedIndexChanged += SelectionChanged;
        }

        if (content is TreeView tree)
        {
            tree.SelectionChanged += SelectionChanged;
        }

        _previousEnabled = IsEffectivelyEnabled();
        UI.Register(this);
    }

    public UIPlayback Play(UIAnimation animation)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Animation.Play(animation);
    }
    public UIPlayback? Trigger(string trigger)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_style.Bindings.TryGetValue(trigger, out var binding))
        {
            return null;
        }

        if (_bindings.Remove(trigger, out var previous))
        {
            previous.Cancel();
        }

        var animation = binding.CreateAnimation();
        if (animation is null)
        {
            return null;
        }

        var playback = Play(animation);
        _bindings[trigger] = playback;
        return playback;
    }

    private void StopBinding(string trigger)
    {
        if (_bindings.TryGetValue(trigger, out var playback))
        {
            playback.Cancel(_style.Bindings[trigger].SettleSeconds);
        }
    }
    private void Hover(object sender, MyraEventArgs args)
    {
        if (CanInteract)
        {
            Trigger(UITrigger.Hover);
        }
    }
    private void HoverExit(object sender, MyraEventArgs args)
    {
        StopBinding(UITrigger.Hover);
        if (CanInteract)
        {
            Trigger(UITrigger.HoverExit);
        }
    }
    private void FocusChanged(object sender, MyraEventArgs args)
    {
        if (Content.IsKeyboardFocused)
        {
            if (CanInteract)
            {
                Trigger(UITrigger.Focus);
            }
        }
        else
        {
            StopBinding(UITrigger.Focus);
            if (CanInteract)
            {
                Trigger(UITrigger.Blur);
            }
        }
    }
    private void HandlePressedChanged(object sender, MyraEventArgs args)
    {
        if (!CanInteract)
        {
            return;
        }

        if (!Content.IsPressed)
        {
            StopBinding(UITrigger.Press);
        }

        Trigger(Content.IsPressed ? UITrigger.Press : UITrigger.Release);
        if (Content is CheckButtonBase)
        {
            Trigger(Content.IsPressed ? UITrigger.Selected : UITrigger.Deselected);
        }
    }
    private void Click(object sender, MyraEventArgs args)
    {
        if (CanInteract)
        {
            Trigger(UITrigger.Click);
        }
    }
    private void SelectionChanged(object sender, MyraEventArgs args)
    {
        if (!CanInteract)
        {
            return;
        }

        var selected = Content switch
        {
            ListView list => list.SelectedIndex.HasValue,
            ComboView combo => combo.SelectedIndex.HasValue,
            TabControl tabs => tabs.SelectedIndex.HasValue,
            TreeView tree => tree.SelectedNode is not null,
            _ => false
        };
        Trigger(selected ? UITrigger.Selected : UITrigger.Deselected);
    }
    private bool CanInteract => !_disposed && !IsHiding && IsEffectivelyVisible() && IsEffectivelyEnabled();
    private bool IsEffectivelyVisible()
    {
        if (!Content.Visible)
        {
            return false;
        }

        for (Widget? widget = this; widget is not null; widget = widget.Parent)
        {
            if (!widget.Visible)
            {
                return false;
            }
        }

        return true;
    }
    private bool IsEffectivelyEnabled()
    {
        if (!Content.Enabled)
        {
            return false;
        }

        for (Widget? widget = this; widget is not null; widget = widget.Parent)
        {
            if (!widget.Enabled)
            {
                return false;
            }
        }

        return true;
    }

    public void Show()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsHiding)
        {
            _exit?.Cancel();
            _exit = null;
            _hiding!.TrySetResult(UIPlaybackState.Cancelled);
            _hiding = null;
            Enabled = _enabledBeforeHide;
            _presented = false;
        }
        Visible = Content.Visible = true;
    }

    public Task<UIPlaybackState> Hide(bool immediate = false)
    {
        if (_disposed || !Visible)
        {
            return Task.FromResult(UIPlaybackState.Completed);
        }

        if (immediate)
        {
            FinishHide(UIPlaybackState.Cancelled);
            return Task.FromResult(UIPlaybackState.Cancelled);
        }
        if (_hiding is not null)
        {
            return _hiding.Task;
        }

        _hiding = new TaskCompletionSource<UIPlaybackState>();
        var task = _hiding.Task;
        _enabledBeforeHide = Enabled;
        Enabled = false;
        Animation.CancelAll();
        _bindings.Clear();
        _exit = Trigger(UITrigger.Hide);
        if (_exit is null)
        {
            FinishHide(UIPlaybackState.Completed);
        }

        return task;
    }
    private void FinishHide(UIPlaybackState state)
    {
        Animation.CancelAll();
        _bindings.Clear();
        Visible = false;
        _presented = false;
        if (_hiding is not null)
        {
            Enabled = _enabledBeforeHide;
        }

        _exit = null;
        var pending = _hiding;
        _hiding = null;
        ReleaseGraphics();
        pending?.TrySetResult(state);
    }

    internal void Update(float dt)
    {
        if (_disposed)
        {
            return;
        }

        if (Desktop is null)
        {
            if (_wasPlaced)
            {
                Dispose();
            }

            return;
        }
        _wasPlaced = true;
        if (!IsEffectivelyVisible())
        {
            if (IsHiding)
            {
                FinishHide(UIPlaybackState.Cancelled);
            }
            else if (_presented)
            {
                Animation.CancelAll();
                _bindings.Clear();
                _presented = false;
                ReleaseGraphics();
            }

            return;
        }
        if (!_presented)
        {
            _presented = true;
            Trigger(UITrigger.Show);
        }
        var enabled = IsEffectivelyEnabled();
        if (enabled != _previousEnabled)
        {
            _previousEnabled = enabled;
            if (!IsHiding)
            {
                if (!enabled)
                {
                    Animation.CancelAll();
                    _bindings.Clear();
                }
                Trigger(enabled ? UITrigger.Enabled : UITrigger.Disabled);
            }
        }
        Animation.Update(dt);
        foreach (var key in _bindings.Where(pair => pair.Value.IsFinished).Select(pair => pair.Key).ToArray())
        {
            _bindings.Remove(key);
        }

        if (IsHiding && _exit?.IsFinished == true)
        {
            FinishHide(_exit.State);
        }
    }

    public override void InternalRender(RenderContext context)
    {
        if (_disposed)
        {
            return;
        }

        if (!_initialized)
        {
            try
            {
                foreach (var definition in _definitions)
                {
                    _materials.Add(definition.CreateInstance(UI.GraphicsDevice));
                }

                _initialized = true;
            }
            catch
            {
                ReleaseGraphics();
                throw;
            }
        }
        UI.MaterialRenderer.Render(this, context, _surface, _materials, Animation);
    }

    private void ReleaseGraphics()
    {
        foreach (var material in _materials)
        {
            material.Dispose();
        }

        _materials.Clear();
        _initialized = false;
        UI.MaterialRenderer.Release(_surface);
    }
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        FinishHide(UIPlaybackState.Cancelled);
        Content.MouseEntered -= Hover;
        Content.MouseLeft -= HoverExit;
        Content.KeyboardFocusChanged -= FocusChanged;
        Content.PressedChanged -= HandlePressedChanged;
        if (Content is ButtonBase button)
        {
            button.Click -= Click;
        }

        if (Content is ListView list)
        {
            list.SelectedIndexChanged -= SelectionChanged;
        }

        if (Content is ComboView combo)
        {
            combo.SelectedIndexChanged -= SelectionChanged;
        }

        if (Content is TabControl tabs)
        {
            tabs.SelectedIndexChanged -= SelectionChanged;
        }

        if (Content is TreeView tree)
        {
            tree.SelectionChanged -= SelectionChanged;
        }

        Animation.Dispose();
        UI.Unregister(this);
    }
}
