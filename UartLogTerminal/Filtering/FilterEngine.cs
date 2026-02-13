using System.Text.RegularExpressions;
using UartLogTerminal.Models;

namespace UartLogTerminal.Filtering;

public sealed class FilterEngine
{
    public bool IsMatch(LogEntry entry, string expression, bool useRegex, bool matchCase)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return true;
        }

        if (!useRegex)
        {
            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            return entry.Text.Contains(expression, comparison);
        }

        var options = RegexOptions.Compiled;
        if (!matchCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        return Regex.IsMatch(entry.Text, expression, options);
    }
}
