using System;
using HighMetro.Event;
using HighMetro.Services;

namespace HighMetro.BaseModel;

public class SerialCommInfo
{
    public int Bh { get; set; }
    public int HostBh { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CommName { get; set; } = string.Empty;
    public int BaudRate { get; set; }
    public int Parity { get; set; }
    public int DataBits { get; set; }
    public int StopBits { get; set; }
    public int Id { get; set; }
    public int Sign { get; set; }
    public string CommType { get; set; } = string.Empty;
    public bool Open { get; set; }
    public bool IsValid()
    {
        return HostBh>0 && 
               Bh>0 && 
               !string.IsNullOrWhiteSpace(CommName) && 
               BaudRate>0 && 
               DataBits>0 && 
               Parity>=0 &&
               StopBits>0;
    }
    //接收数据；
    private EventHandler? _bufferDataProdEvent;
    public event EventHandler? BufferDataProdEvent
    {
        add => _bufferDataProdEvent ??= value;
        remove => _bufferDataProdEvent -= value;
    }
    public EventHandler? GetBufferDataProdEvent()
    {
        return _bufferDataProdEvent;
    }
    public void RaiseBufferDataProdEvent(SocketDataBlock socketDataBlock)
    {
        _bufferDataProdEvent?.Invoke(null, new SocketDataEventArgs(socketDataBlock));
    }
    public CommSerialImpl? CommSerialImpl{ get; set; }
}