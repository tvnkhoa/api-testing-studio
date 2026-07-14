using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.UI.Converters;

/// <summary>
/// Maps an <see cref="HttpVerb"/> to a semantic badge brush. State is never conveyed by colour
/// alone — the badge always shows the verb text as well (see <c>.claude/UI_GUIDELINES.md</c>).
/// Resolves brushes from the merged theme dictionaries by semantic key.
/// </summary>
public sealed class HttpVerbToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = value switch
        {
            HttpVerb.Get => "Semantic.Info.Brush",
            HttpVerb.Post => "Semantic.Success.Brush",
            HttpVerb.Put or HttpVerb.Patch => "Semantic.Warning.Brush",
            HttpVerb.Delete => "Semantic.Error.Brush",
            _ => "Semantic.Info.Brush",
        };

        return System.Windows.Application.Current?.TryFindResource(key) as Brush ?? Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
