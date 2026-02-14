using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Microsoft.Win32;
using UartLogTerminal.Filtering;
using UartLogTerminal.Models;
using UartLogTerminal.Services;
using LineSegment = UartLogTerminal.Models.LineSegment;

namespace UartLogTerminal.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private const int MaxBufferedLines = 5000;

    private readonly ISerialPortService _serialPortService;
    private readonly FilterEngine _filterEngine = new();
    private readonly SynchronizationContext _uiContext;
    private readonly Queue<LogEntry> _entries = new();
    private readonly Queue<string> _rawLines = new();

    private string? _selectedPort;
    private int _selectedBaudRate = 115200;
    private bool _isPaused;
    private string _rawLogText = string.Empty;
    private string _connectionStatus = "Disconnected";
    private Brush _statusBrush = Brushes.Firebrick;
    private string _footerText = "Ready";
    private string _txInput = string.Empty;
    private long _totalReceived;
    private int _nextFilterNumber = 1;
    private FilterTabViewModel? _selectedFilterTab;
    private bool _isBulkLoading;
    private bool _isFilterPanelBottom;
    private bool _isDarkTheme = true;
    private int _selectedDataBits = 8;
    private Parity _selectedParity = Parity.None;
    private StopBits _selectedStopBits = StopBits.One;
    private Handshake _selectedHandshake = Handshake.None;
    private bool _dtrEnable = true;
    private bool _rtsEnable = true;
    private bool _showAdvancedSerialSettings;
    private bool _isLiveSource = true;
    private string _rawSourceName = "Live";

    public MainViewModel(ISerialPortService serialPortService)
    {
        _serialPortService = serialPortService;
        _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();

        AvailablePorts = new ObservableCollection<string>();
        BaudRates = new ObservableCollection<int> { 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 };
        DataBitsOptions = new ObservableCollection<int> { 5, 6, 7, 8 };
        ParityOptions = new ObservableCollection<Parity> { Parity.None, Parity.Even, Parity.Odd, Parity.Mark, Parity.Space };
        StopBitsOptions = new ObservableCollection<StopBits> { StopBits.One, StopBits.Two };
        HandshakeOptions = new ObservableCollection<Handshake> { Handshake.None, Handshake.RequestToSend, Handshake.XOnXOff, Handshake.RequestToSendXOnXOff };
        FilterTabs = new ObservableCollection<FilterTabViewModel>();
        ColorOptions = BuildColorOptions();

        RefreshPortsCommand = new RelayCommand(RefreshPorts);
        ConnectCommand = new RelayCommand(Connect, () => !IsConnected && !string.IsNullOrWhiteSpace(SelectedPort));
        DisconnectCommand = new RelayCommand(Disconnect, () => IsConnected);
        ClearCommand = new RelayCommand(ClearLogs);
        SendCommand = new RelayCommand(SendTx, () => IsConnected && !string.IsNullOrWhiteSpace(TxInput));
        OpenLogFileCommand = new RelayCommand(OpenLogFile);
        AddFilterTabCommand = new RelayCommand(AddFilterTab);
        RemoveSelectedFilterTabCommand = new RelayCommand(RemoveSelectedFilterTab, () => SelectedFilterTab is not null);
        ToggleFilterPanelDockCommand = new RelayCommand(ToggleFilterPanelDock);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);

        _serialPortService.LineReceived += OnLineReceived;
        _serialPortService.ErrorOccurred += OnSerialError;

        RefreshPorts();
        AddFilterTab();
    }

    public ObservableCollection<string> AvailablePorts { get; }
    public ObservableCollection<int> BaudRates { get; }
    public ObservableCollection<int> DataBitsOptions { get; }
    public ObservableCollection<Parity> ParityOptions { get; }
    public ObservableCollection<StopBits> StopBitsOptions { get; }
    public ObservableCollection<Handshake> HandshakeOptions { get; }
    public ObservableCollection<FilterTabViewModel> FilterTabs { get; }
    public ObservableCollection<ColorOption> ColorOptions { get; }

    public RelayCommand RefreshPortsCommand { get; }
    public RelayCommand ConnectCommand { get; }
    public RelayCommand DisconnectCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand SendCommand { get; }
    public RelayCommand OpenLogFileCommand { get; }
    public RelayCommand AddFilterTabCommand { get; }
    public RelayCommand RemoveSelectedFilterTabCommand { get; }
    public RelayCommand ToggleFilterPanelDockCommand { get; }
    public RelayCommand ToggleThemeCommand { get; }

    public bool IsConnected => _serialPortService.IsOpen;

    public string? SelectedPort
    {
        get => _selectedPort;
        set
        {
            if (SetProperty(ref _selectedPort, value))
            {
                ConnectCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int SelectedBaudRate
    {
        get => _selectedBaudRate;
        set => SetProperty(ref _selectedBaudRate, value);
    }

    public bool IsPaused
    {
        get => _isPaused;
        set => SetProperty(ref _isPaused, value);
    }

    public int SelectedDataBits
    {
        get => _selectedDataBits;
        set => SetProperty(ref _selectedDataBits, value);
    }

    public Parity SelectedParity
    {
        get => _selectedParity;
        set => SetProperty(ref _selectedParity, value);
    }

    public StopBits SelectedStopBits
    {
        get => _selectedStopBits;
        set => SetProperty(ref _selectedStopBits, value);
    }

    public Handshake SelectedHandshake
    {
        get => _selectedHandshake;
        set => SetProperty(ref _selectedHandshake, value);
    }

    public bool DtrEnable
    {
        get => _dtrEnable;
        set => SetProperty(ref _dtrEnable, value);
    }

    public bool RtsEnable
    {
        get => _rtsEnable;
        set => SetProperty(ref _rtsEnable, value);
    }

    public bool ShowAdvancedSerialSettings
    {
        get => _showAdvancedSerialSettings;
        set => SetProperty(ref _showAdvancedSerialSettings, value);
    }

    public string RawLogText
    {
        get => _rawLogText;
        private set => SetProperty(ref _rawLogText, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set => SetProperty(ref _connectionStatus, value);
    }

    public Brush StatusBrush
    {
        get => _statusBrush;
        private set => SetProperty(ref _statusBrush, value);
    }

    public string FooterText
    {
        get => _footerText;
        private set => SetProperty(ref _footerText, value);
    }

    public bool IsLiveSource
    {
        get => _isLiveSource;
        private set => SetProperty(ref _isLiveSource, value);
    }

    public string RawSourceName
    {
        get => _rawSourceName;
        private set => SetProperty(ref _rawSourceName, value);
    }

    public string TxInput
    {
        get => _txInput;
        set
        {
            if (SetProperty(ref _txInput, value))
            {
                SendCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public FilterTabViewModel? SelectedFilterTab
    {
        get => _selectedFilterTab;
        set
        {
            if (SetProperty(ref _selectedFilterTab, value))
            {
                RemoveSelectedFilterTabCommand.RaiseCanExecuteChanged();
                UpdateFooterCounters();
            }
        }
    }

    public bool IsFilterPanelBottom
    {
        get => _isFilterPanelBottom;
        set
        {
            if (SetProperty(ref _isFilterPanelBottom, value))
            {
                RaisePropertyChanged(nameof(FilterDockText));
            }
        }
    }

    public string FilterDockText => IsFilterPanelBottom ? "Filter Panel: Bottom" : "Filter Panel: Right";
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (SetProperty(ref _isDarkTheme, value))
            {
                RaisePropertyChanged(nameof(ThemeToggleText));
                RaisePropertyChanged(nameof(ThemePanelBrush));
                RaisePropertyChanged(nameof(ThemeEditorBrush));
                RaisePropertyChanged(nameof(ThemeListBrush));
                RaisePropertyChanged(nameof(ThemeForegroundBrush));
                RebuildAllFilterTabs();
            }
        }
    }

    public string ThemeToggleText => IsDarkTheme ? "Theme: Dark" : "Theme: Light";
    public Brush ThemePanelBrush => IsDarkTheme ? ToBrush("#252526") : ToBrush("#FFFFFF");
    public Brush ThemeEditorBrush => IsDarkTheme ? ToBrush("#1F1F1F") : ToBrush("#EFEFEF");
    public Brush ThemeListBrush => IsDarkTheme ? ToBrush("#1E1E1E") : ToBrush("#F7F7F7");
    public Brush ThemeForegroundBrush => IsDarkTheme ? ToBrush("#D4D4D4") : ToBrush("#1F1F1F");

    public void Dispose()
    {
        foreach (FilterTabViewModel tab in FilterTabs)
        {
            tab.FilterSettingsChanged -= OnFilterSettingsChanged;
        }

        _serialPortService.LineReceived -= OnLineReceived;
        _serialPortService.ErrorOccurred -= OnSerialError;
        _serialPortService.Dispose();
    }

    private static ObservableCollection<ColorOption> BuildColorOptions()
    {
        return
        [
            new ColorOption { Name = "Black", Brush = Brushes.Black },
            new ColorOption { Name = "White", Brush = Brushes.White },
            new ColorOption { Name = "Red", Brush = Brushes.Red },
            new ColorOption { Name = "Orange", Brush = Brushes.Orange },
            new ColorOption { Name = "Yellow", Brush = Brushes.Yellow },
            new ColorOption { Name = "Green", Brush = Brushes.LimeGreen },
            new ColorOption { Name = "Blue", Brush = Brushes.DeepSkyBlue },
            new ColorOption { Name = "Navy", Brush = Brushes.Navy },
            new ColorOption { Name = "Purple", Brush = Brushes.MediumPurple },
            new ColorOption { Name = "Gray", Brush = Brushes.Gray },
            new ColorOption { Name = "Light Gray", Brush = Brushes.LightGray },
            new ColorOption { Name = "Transparent", Brush = Brushes.Transparent }
        ];
    }

    private void AddFilterTab()
    {
        FilterTabViewModel tab = CreateNewFilterTab();
        tab.FilterSettingsChanged += OnFilterSettingsChanged;
        FilterTabs.Add(tab);
        SelectedFilterTab = tab;
        RebuildFilterTab(tab);
    }

    private FilterTabViewModel CreateNewFilterTab()
    {
        int idx = _nextFilterNumber++;

        ColorOption defaultForeground = ColorOptions[Math.Min(idx, ColorOptions.Count - 2)];
        ColorOption transparent = ColorOptions.First(x => x.Name == "Transparent");

        return new FilterTabViewModel
        {
            Name = $"Filter {idx}",
            SelectedForegroundColor = defaultForeground,
            SelectedBackgroundColor = transparent
        };
    }

    private void RemoveSelectedFilterTab()
    {
        if (SelectedFilterTab is null)
        {
            return;
        }

        SelectedFilterTab.FilterSettingsChanged -= OnFilterSettingsChanged;
        FilterTabs.Remove(SelectedFilterTab);
        SelectedFilterTab = FilterTabs.LastOrDefault();
        UpdateFooterCounters();
    }

    private void ToggleFilterPanelDock()
    {
        IsFilterPanelBottom = !IsFilterPanelBottom;
    }

    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

    private void RefreshPorts()
    {
        string[] ports = _serialPortService.GetAvailablePorts();
        AvailablePorts.Clear();
        foreach (string port in ports)
        {
            AvailablePorts.Add(port);
        }

        if (!string.IsNullOrWhiteSpace(SelectedPort) && AvailablePorts.Contains(SelectedPort))
        {
            return;
        }

        SelectedPort = AvailablePorts.FirstOrDefault();
        FooterText = $"Ports: {AvailablePorts.Count}";
    }

    private void Connect()
    {
        if (string.IsNullOrWhiteSpace(SelectedPort))
        {
            FooterText = "Select COM port first.";
            return;
        }

        try
        {
            _serialPortService.Open(
                SelectedPort,
                SelectedBaudRate,
                SelectedDataBits,
                SelectedParity,
                SelectedStopBits,
                SelectedHandshake,
                DtrEnable,
                RtsEnable);
            SetLiveSource();
            ConnectionStatus = $"Connected ({SelectedPort} @ {SelectedBaudRate})";
            StatusBrush = Brushes.ForestGreen;
            FooterText = "Connected.";
            RaisePropertyChanged(nameof(IsConnected));
            ConnectCommand.RaiseCanExecuteChanged();
            DisconnectCommand.RaiseCanExecuteChanged();
            SendCommand.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            FooterText = $"Connect failed: {ex.Message}";
        }
    }

    private void Disconnect()
    {
        try
        {
            _serialPortService.Close();
            ConnectionStatus = "Disconnected";
            StatusBrush = Brushes.Firebrick;
            FooterText = "Disconnected.";
            RaisePropertyChanged(nameof(IsConnected));
            ConnectCommand.RaiseCanExecuteChanged();
            DisconnectCommand.RaiseCanExecuteChanged();
            SendCommand.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            FooterText = $"Disconnect failed: {ex.Message}";
        }
    }

    private void SendTx()
    {
        string tx = TxInput.TrimEnd('\r', '\n');
        if (string.IsNullOrWhiteSpace(tx))
        {
            return;
        }

        try
        {
            _serialPortService.SendLine(tx);
            TxInput = string.Empty;
            FooterText = $"TX: {tx}";
        }
        catch (Exception ex)
        {
            FooterText = $"TX failed: {ex.Message}";
        }
    }

    private void ClearLogs()
    {
        _entries.Clear();
        _rawLines.Clear();
        RawLogText = string.Empty;
        _totalReceived = 0;

        foreach (FilterTabViewModel tab in FilterTabs)
        {
            tab.Lines.Clear();
        }

        UpdateFooterCounters();
    }

    private void OnFilterSettingsChanged(object? sender, EventArgs e)
    {
        if (sender is not FilterTabViewModel tab)
        {
            return;
        }

        RebuildFilterTab(tab);
    }

    private void OnSerialError(object? sender, string e)
    {
        _uiContext.Post(_ => FooterText = e, null);
    }

    private void OnLineReceived(object? sender, string line)
    {
        _uiContext.Post(_ =>
        {
            if (IsPaused)
            {
                return;
            }

            SetLiveSource();
            IngestLine(line, DateTime.Now);
        }, null);
    }

    private void OpenLogFile()
    {
        OpenFileDialog dlg = new()
        {
            Title = "Open Log File",
            Filter = "Log Files (*.log;*.txt)|*.log;*.txt|All Files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dlg.ShowDialog() != true)
        {
            return;
        }

        try
        {
            ClearLogs();
            SetFileSource(Path.GetFileName(dlg.FileName));

            long loaded = 0;
            _isBulkLoading = true;

            foreach (string line in File.ReadLines(dlg.FileName))
            {
                IngestLine(line, DateTime.Now);
                loaded++;
            }
            
            RawLogText = JoinLines(_rawLines);
            UpdateFooterCounters();
            FooterText = $"Loaded {loaded:N0} lines from {Path.GetFileName(dlg.FileName)}";
        }
        catch (Exception ex)
        {
            FooterText = $"Load failed: {ex.Message}";
        }
        finally
        {
            _isBulkLoading = false;
        }
    }

    private void IngestLine(string line, DateTime timestamp)
    {
        LogEntry entry = new()
        {
            Timestamp = timestamp,
            Text = line
        };

        _totalReceived++;
        AddEntry(entry);
    }

    private void AddEntry(LogEntry entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > MaxBufferedLines)
        {
            _entries.Dequeue();
        }

        EnqueueWithCap(_rawLines, FormatEntry(entry));
        if (!_isBulkLoading)
        {
            RawLogText = JoinLines(_rawLines);
        }

        foreach (FilterTabViewModel tab in FilterTabs)
        {
            if (TryIsMatch(tab, entry, out bool isMatch, out string? error))
            {
                if (isMatch)
                {
                    AppendLineToTab(tab, FormatEntry(entry));
                }
            }
            else if (!string.IsNullOrWhiteSpace(error))
            {
                FooterText = $"{tab.Name}: {error}";
            }
        }

        if (!_isBulkLoading)
        {
            UpdateFooterCounters();
        }
    }

    private void RebuildFilterTab(FilterTabViewModel tab)
    {
        tab.Lines.Clear();

        foreach (LogEntry entry in _entries)
        {
            if (TryIsMatch(tab, entry, out bool isMatch, out string? error))
            {
                if (isMatch)
                {
                    AppendLineToTab(tab, FormatEntry(entry));
                }
            }
            else if (!string.IsNullOrWhiteSpace(error))
            {
                FooterText = $"{tab.Name}: {error}";
                break;
            }
        }

        UpdateFooterCounters();
    }

    private void RebuildAllFilterTabs()
    {
        foreach (FilterTabViewModel tab in FilterTabs)
        {
            RebuildFilterTab(tab);
        }
    }

    private bool TryIsMatch(FilterTabViewModel tab, LogEntry entry, out bool isMatch, out string? error)
    {
        try
        {
            isMatch = _filterEngine.IsMatch(entry, tab.Expression, tab.UseRegex, tab.MatchCase);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            isMatch = false;
            error = $"Filter error: {ex.Message}";
            return false;
        }
    }

    private void AppendLineToTab(FilterTabViewModel tab, string formattedLine)
    {
        tab.Lines.Add(new ColoredLogLine
        {
            Text = formattedLine,
            Segments = BuildSegments(tab, formattedLine)
        });

        while (tab.Lines.Count > MaxBufferedLines)
        {
            tab.Lines.RemoveAt(0);
        }
    }

    private IReadOnlyList<LineSegment> BuildSegments(FilterTabViewModel tab, string text)
    {
        Brush baseForeground = IsDarkTheme ? Brushes.Gainsboro : Brushes.Black;
        List<(int Start, int Length)> matches = FindMatches(tab, text);
        if (matches.Count == 0)
        {
            return
            [
                new LineSegment
                {
                    Text = text,
                    ForegroundBrush = baseForeground,
                    BackgroundBrush = Brushes.Transparent
                }
            ];
        }

        List<LineSegment> segments = [];
        int cursor = 0;

        foreach ((int start, int length) in matches)
        {
            if (start > cursor)
            {
                segments.Add(new LineSegment
                {
                    Text = text.Substring(cursor, start - cursor),
                    ForegroundBrush = baseForeground,
                    BackgroundBrush = Brushes.Transparent
                });
            }

            segments.Add(new LineSegment
            {
                Text = text.Substring(start, length),
                ForegroundBrush = tab.ForegroundBrush,
                BackgroundBrush = tab.BackgroundBrush
            });

            cursor = start + length;
        }

        if (cursor < text.Length)
        {
            segments.Add(new LineSegment
            {
                Text = text[cursor..],
                ForegroundBrush = baseForeground,
                BackgroundBrush = Brushes.Transparent
            });
        }

        return segments;
    }

    private static List<(int Start, int Length)> FindMatches(FilterTabViewModel tab, string text)
    {
        if (string.IsNullOrWhiteSpace(tab.Expression))
        {
            return [];
        }

        List<(int Start, int Length)> matches = [];

        if (tab.UseRegex)
        {
            RegexOptions options = RegexOptions.Compiled;
            if (!tab.MatchCase)
            {
                options |= RegexOptions.IgnoreCase;
            }

            MatchCollection regexMatches = Regex.Matches(text, tab.Expression, options);
            foreach (Match match in regexMatches)
            {
                if (match.Success && match.Length > 0)
                {
                    matches.Add((match.Index, match.Length));
                }
            }
        }
        else
        {
            StringComparison comparison = tab.MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int idx = 0;
            while (idx < text.Length)
            {
                int found = text.IndexOf(tab.Expression, idx, comparison);
                if (found < 0)
                {
                    break;
                }

                matches.Add((found, tab.Expression.Length));
                idx = found + Math.Max(1, tab.Expression.Length);
            }
        }

        matches.Sort((a, b) => a.Start.CompareTo(b.Start));
        List<(int Start, int Length)> nonOverlapping = [];
        int lastEnd = -1;
        foreach ((int start, int length) in matches)
        {
            if (start >= lastEnd)
            {
                nonOverlapping.Add((start, length));
                lastEnd = start + length;
            }
        }

        return nonOverlapping;
    }

    private void UpdateFooterCounters()
    {
        int selectedTabCount = SelectedFilterTab?.Lines.Count ?? 0;
        FooterText = $"Received: {_totalReceived:N0} | Visible raw: {_rawLines.Count:N0} | Selected filter lines: {selectedTabCount:N0} | Filters: {FilterTabs.Count}";
    }

    private static string FormatEntry(LogEntry entry)
    {
        return $"{entry.Timestamp:HH:mm:ss.fff} | {entry.Text}";
    }

    private static void EnqueueWithCap(Queue<string> queue, string value)
    {
        queue.Enqueue(value);
        while (queue.Count > MaxBufferedLines)
        {
            queue.Dequeue();
        }
    }

    private static string JoinLines(IEnumerable<string> lines)
    {
        StringBuilder sb = new();
        foreach (string line in lines)
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            sb.Append(line);
        }

        return sb.ToString();
    }

    private static Brush ToBrush(string hex)
    {
        return (Brush)new BrushConverter().ConvertFromString(hex)!;
    }

    private void SetLiveSource()
    {
        IsLiveSource = true;
        RawSourceName = "Live";
    }

    private void SetFileSource(string fileName)
    {
        IsLiveSource = false;
        RawSourceName = string.IsNullOrWhiteSpace(fileName) ? "Loaded File" : fileName;
    }
}
