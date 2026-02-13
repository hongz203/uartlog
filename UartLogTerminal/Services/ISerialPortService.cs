namespace UartLogTerminal.Services;

public interface ISerialPortService : IDisposable
{
    event EventHandler<string>? LineReceived;
    event EventHandler<string>? ErrorOccurred;

    bool IsOpen { get; }

    string[] GetAvailablePorts();
    void Open(string portName, int baudRate);
    void Close();
    void SendLine(string text);
}
