using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using ApiTestingStudio.Domain.Enums;

namespace ApiTestingStudio.UI.Converters;

/// <summary>
/// Maps a <see cref="RunStatus"/> to a semantic brush for live node coloring in the Workflow
/// Designer. Status is never conveyed by colour alone — the node always shows its status label too
/// (see <c>.claude/UI_GUIDELINES.md</c>). Resolves brushes from the merged theme dictionaries by
/// semantic key, mirroring <see cref="HttpVerbToBrushConverter"/>.
/// </summary>
public sealed class RunStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Pending has no dedicated colour; a neutral grey reads as "not started".
        if (value is not RunStatus status || status == RunStatus.Pending)
        {
            return Brushes.Gray;
        }

        var key = status switch
        {
            RunStatus.Running => "Semantic.Info.Brush",
            RunStatus.Passed => "Semantic.Success.Brush",
            RunStatus.Failed => "Semantic.Error.Brush",
            RunStatus.Cancelled => "Semantic.Warning.Brush",
            _ => "Semantic.Info.Brush",
        };

        return System.Windows.Application.Current?.TryFindResource(key) as Brush ?? Brushes.Gray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
