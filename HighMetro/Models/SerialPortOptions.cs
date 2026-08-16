using CommunityToolkit.Mvvm.ComponentModel;
using System.IO.Ports;
    
namespace HighMetro.Models;

public partial class SerialPortOptions : ObservableObject
{
    [ObservableProperty] 
    private int _id;
    
    [ObservableProperty] 
    private string _name;
    
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

    public SerialPortOptions(
        int id,
        string name,
        string portName,
        int baudRate,
        int dataBits, 
        int parity,
        int stopBits)
    {
        Id = id;
        Name = name;
        PortName = portName;
        BaudRate = baudRate;
        DataBits = dataBits;
        Parity = parity;
        StopBits = stopBits;
    }
}