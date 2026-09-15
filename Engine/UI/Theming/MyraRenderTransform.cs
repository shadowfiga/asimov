using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;

namespace Graphite.Engine.UI.Theming;

/// <summary>Isolates access to Myra 1.6.5's internal current drawing transform.</summary>
internal static class MyraRenderTransform
{
    // A cached delegate avoids reflection/boxing for every surface. UI.Scale alone is not enough:
    // nested widgets, popups and material captures must use their actual render-context transform.
    internal static readonly Func<RenderContext, Matrix> GetMatrix = CreateGetter();

    private static Func<RenderContext, Matrix> CreateGetter()
    {
        var context = Expression.Parameter(typeof(RenderContext), "context");
        var transform = typeof(RenderContext).GetField("Transform", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new NotSupportedException("Myra render transform API changed.");
        var matrix = Expression.Field(Expression.Field(context, transform), "Matrix");
        return Expression.Lambda<Func<RenderContext, Matrix>>(matrix, context).Compile();
    }
}
