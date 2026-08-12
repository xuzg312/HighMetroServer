using System;
using HighMetro.Event;
using HighMetro.Services;

namespace HighMetro.BaseModel;

public class HostInfo
{
    public int Bh { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get ; set; } = string.Empty;
    public string Ip { get ; set ; } = string.Empty;
    public int Port { get; set; }
    public string Effect { get ; set ; } = string.Empty;
    public bool Open { get; set; }
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
    //展示客户端连接消息;
    private EventHandler? _clientConnEvent;
    public event EventHandler? ClientConnEvent
    {
        add => _clientConnEvent ??= value;
        remove => _clientConnEvent -= value;
    }
    public void RaiseClientConnEvent(string message)
    {
        _clientConnEvent?.Invoke(null, new StringEventArgs(message));
    }
    public TcpServerListenerImpl? TcpServer { get; set; }
}