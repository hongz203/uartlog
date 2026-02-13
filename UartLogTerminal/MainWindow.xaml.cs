using System.Windows;
using UartLogTerminal.Services;
using UartLogTerminal.ViewModels;

namespace UartLogTerminal;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel(new SerialPortService());
        DataContext = _viewModel;
        Closed += (_, _) => _viewModel.Dispose();
    }
}
