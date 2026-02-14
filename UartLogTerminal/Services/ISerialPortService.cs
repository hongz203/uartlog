namespace UartLogTerminal.Services;

public interface ISerialPortService : IDisposable
{
    event EventHandler<string>? LineReceived;
    event EventHandler<string>? ErrorOccurred;

    bool IsOpen { get; }

    string[] GetAvailablePorts();
    void Open(
        string portName,
        int baudRate,
        int dataBits,
        System.IO.Ports.Parity parity,
        System.IO.Ports.StopBits stopBits,
        System.IO.Ports.Handshake handshake,
        bool dtrEnable,
        bool rtsEnable);
    void Close();
    void SendLine(string text);
}
