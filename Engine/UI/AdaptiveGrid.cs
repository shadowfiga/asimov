using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace Graphite.Engine.UI;

/// <summary>A parent-relative grid with declarative column breakpoints, evaluated by normal layout.</summary>
public class AdaptiveGrid : Grid
{
    public ObservableCollection<UIColumnRule> ColumnRules { get; } = [];

    public AdaptiveGrid()
    {
        ColumnRules.CollectionChanged += (_, _) => InvalidateMeasure();
    }

    protected override Point InternalMeasure(Point availableSize)
    {
        foreach (var rule in ColumnRules)
        {
            var column = ColumnsProportions[rule.Column];
            column.Type = ProportionType.Pixels;
            column.Value = availableSize.X < rule.Breakpoint ? rule.CompactWidth : rule.Width;
        }
        return base.InternalMeasure(availableSize);
    }
}

/// <summary>Widths and breakpoint are logical units in the grid's parent, not physical screen pixels.</summary>
public sealed record UIColumnRule(int Column, int Width, int CompactWidth, int Breakpoint);
