namespace UartLogTerminal.Models;

public sealed class ColoredLogLine
{
    public required string Text { get; init; }
    public required IReadOnlyList<LineSegment> Segments { get; init; }
}
