using CommunityToolkit.Mvvm.ComponentModel;
using System.IO.Ports;
    
namespace HighMetro.Models;

public partial class SerialPortOptions : ObservableObject
{
    [ObservableProperty] 
    private string _portName;
    
    [ObservableProperty] 
    private int _baudRate;
    
    [ObservableProperty] 
    private int _dataBits;
    
    [ObservableProperty] 
    private int _parity;
    
    [ObservableProperty] 
    private int _stopBits;

    public SerialPortOptions(string portName,int baudRate,int dataBits, int parity,int stopBits)
    {
        PortName = portName;
        BaudRate = baudRate;
        DataBits = _dataBits;
        Parity = parity;
        StopBits = stopBits;
    }
}