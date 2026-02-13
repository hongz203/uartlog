using System.IO.Ports;
using System.Text;

namespace UartLogTerminal.Services;

public sealed class SerialPortService : ISerialPortService
{
    private readonly StringBuilder _buffer = new();
    private SerialPort? _serialPort;

    public event EventHandler<string>? LineReceived;
    public event EventHandler<string>? ErrorOccurred;

    public bool IsOpen => _serialPort?.IsOpen == true;

    public string[] GetAvailablePorts()
    {
        return SerialPort.GetPortNames().OrderBy(x => x).ToArray();
    }

    public void Open(string portName, int baudRate)
    {
        if (IsOpen)
        {
            throw new InvalidOperationException("Port is already open.");
        }

        _serialPort = new SerialPort(portName, baudRate)
        {
            DtrEnable = true,
            RtsEnable = true,
            NewLine = "\n",
            ReadTimeout = 500
        };

        _serialPort.DataReceived += SerialPortOnDataReceived;
        _serialPort.ErrorReceived += SerialPortOnErrorReceived;
        _serialPort.Open();
    }

    public void Close()
    {
        if (_serialPort is null)
        {
            return;
        }

        _serialPort.DataReceived -= SerialPortOnDataReceived;
        _serialPort.ErrorReceived -= SerialPortOnErrorReceived;

        if (_serialPort.IsOpen)
        {
            _serialPort.Close();
        }

        _serialPort.Dispose();
        _serialPort = null;
        _buffer.Clear();
    }

    public void SendLine(string text)
    {
        if (_serialPort is null || !_serialPort.IsOpen)
        {
            throw new InvalidOperationException("Port is not open.");
        }

        _serialPort.WriteLine(text);
    }

    private void SerialPortOnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
    {
        ErrorOccurred?.Invoke(this, $"Serial error: {e.EventType}");
    }

    private void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (_serialPort is null || !_serialPort.IsOpen)
            {
                return;
            }

            string chunk = _serialPort.ReadExisting();
            if (string.IsNullOrEmpty(chunk))
            {
                return;
            }

            lock (_buffer)
            {
                _buffer.Append(chunk);
                EmitCompletedLines();
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex.Message);
        }
    }

    private void EmitCompletedLines()
    {
        while (true)
        {
            int newLineIndex = FindLineBreakIndex(_buffer);
            if (newLineIndex < 0)
            {
                return;
            }

            string line = _buffer.ToString(0, newLineIndex).TrimEnd('\r');
            _buffer.Remove(0, newLineIndex + 1);
            LineReceived?.Invoke(this, line);
        }
    }

    private static int FindLineBreakIndex(StringBuilder sb)
    {
        for (int i = 0; i < sb.Length; i++)
        {
            if (sb[i] == '\n')
            {
                return i;
            }
        }

        return -1;
    }

    public void Dispose()
    {
        Close();
    }
}
