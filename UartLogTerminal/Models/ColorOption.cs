using System.Windows.Media;

namespace UartLogTerminal.Models;

public sealed class ColorOption
{
    public required string Name { get; init; }
    public required Brush Brush { get; init; }
}
