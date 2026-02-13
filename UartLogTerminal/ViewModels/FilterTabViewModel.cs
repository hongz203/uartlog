using System.Collections.ObjectModel;
using System.Windows.Media;
using UartLogTerminal.Models;

namespace UartLogTerminal.ViewModels;

public sealed class FilterTabViewModel : ObservableObject
{
    private string _name = "Filter";
    private string _expression = string.Empty;
    private bool _useRegex;
    private bool _matchCase;
    private ColorOption? _selectedForegroundColor;
    private ColorOption? _selectedBackgroundColor;

    public event EventHandler? FilterSettingsChanged;

    public ObservableCollection<ColoredLogLine> Lines { get; } = new();

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                OnFilterSettingsChanged();
            }
        }
    }

    public string Expression
    {
        get => _expression;
        set
        {
            if (SetProperty(ref _expression, value))
            {
                OnFilterSettingsChanged();
            }
        }
    }

    public bool UseRegex
    {
        get => _useRegex;
        set
        {
            if (SetProperty(ref _useRegex, value))
            {
                OnFilterSettingsChanged();
            }
        }
    }

    public bool MatchCase
    {
        get => _matchCase;
        set
        {
            if (SetProperty(ref _matchCase, value))
            {
                OnFilterSettingsChanged();
            }
        }
    }

    public ColorOption? SelectedForegroundColor
    {
        get => _selectedForegroundColor;
        set
        {
            if (SetProperty(ref _selectedForegroundColor, value))
            {
                OnFilterSettingsChanged();
            }
        }
    }

    public ColorOption? SelectedBackgroundColor
    {
        get => _selectedBackgroundColor;
        set
        {
            if (SetProperty(ref _selectedBackgroundColor, value))
            {
                OnFilterSettingsChanged();
            }
        }
    }

    public Brush ForegroundBrush => SelectedForegroundColor?.Brush ?? Brushes.Black;
    public Brush BackgroundBrush => SelectedBackgroundColor?.Brush ?? Brushes.Transparent;

    public void RaiseBrushesChanged()
    {
        RaisePropertyChanged(nameof(ForegroundBrush));
        RaisePropertyChanged(nameof(BackgroundBrush));
    }

    private void OnFilterSettingsChanged()
    {
        RaiseBrushesChanged();
        FilterSettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
