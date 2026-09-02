namespace Graphite.Engine.UI.Controls;

public abstract class UIControl
{
    protected UIControl(string name)
    {
        Name = name;
    }

    public string Name { get; }
}
