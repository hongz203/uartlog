using System.Windows.Media;

namespace UartLogTerminal.Models;

public sealed class LineSegment
{
    public required string Text { get; init; }
    public required Brush ForegroundBrush { get; init; }
    public required Brush BackgroundBrush { get; init; }
}
