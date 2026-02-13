namespace UartLogTerminal.Models;

public sealed class LogEntry
{
    public required DateTime Timestamp { get; init; }
    public required string Text { get; init; }
}
