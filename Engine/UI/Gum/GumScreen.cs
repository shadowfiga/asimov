using Gum.Forms.Controls;
using Gum.Wireframe;

namespace Graphite.Engine.UI.Gum;

public abstract class GumScreen : UIScreen
{
    protected StackPanel Root { get; private set; } = null!;

    protected override void OnOpen()
    {
        Root = new StackPanel();
        Root.AddToRoot();
        Build(Root);
    }

    protected override void OnClose()
    {
        Root.Visual.RemoveFromManagers();
    }

    protected abstract void Build(StackPanel root);
}
