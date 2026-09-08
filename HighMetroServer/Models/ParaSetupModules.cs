using System;
using System.Collections.Generic;
using HighMetroServer.BaseModel;
using HighMetroServer.Event;
using HighMetroServer.Services;

namespace HighMetroServer.Models;

public static class ParaSetupModules
{
    public static HostInfo? HostInfo{ get; set; }
    public static HardInfo? CamInfo{ get; set; }
    public static List<SerialCommInfo>? SerialCommList{ get; set; }
    public static UserInfo? UserInfo{ get; set; }
    public static IDbService? DbService{ get; set; }
    //展示ASC消息；
    private static EventHandler? _ascDataProdEvent;
    public static event EventHandler? AscDataProdEvent
    {
        add => _ascDataProdEvent ??= value;
        remove => _ascDataProdEvent -= value;
    }
    public static void RaiseAscDataProdEvent(string message)
    {
        _ascDataProdEvent?.Invoke(null, new StringEventArgs(message));
    }
    //展示十六进制消息；
    private static EventHandler? _hexDataProdEvent;
    public static event EventHandler? HexDataProdEvent
    {
        add => _hexDataProdEvent ??= value;
        remove => _hexDataProdEvent -= value;
    }
    public static void RaiseHexDataProdEvent(SocketDataBlock socketDataBlock)
    {
        _hexDataProdEvent?.Invoke(null, new SocketDataEventArgs(socketDataBlock));
    }
    //接收数据；
    private static EventHandler? _tcpServerBufferDataProdEvent;
    public static event EventHandler? TcpServerBufferDataProdEvent
    {
        add => _tcpServerBufferDataProdEvent ??= value;
        remove => _tcpServerBufferDataProdEvent -= value;
    }
    public static void RaiseTcpServerBufferDataProdEvent(SocketDataBlock socketDataBlock)
    {
        _tcpServerBufferDataProdEvent?.Invoke(null, new SocketDataEventArgs(socketDataBlock));
    }
    //展示客户端连接消息;
    private static EventHandler? _tcpClientConnEvent;
    public static event EventHandler? TcpClientConnEvent
    {
        add => _tcpClientConnEvent ??= value;
        remove => _tcpClientConnEvent -= value;
    }
    public static void RaiseTcpClientConnEvent(string message)
    {
        _tcpClientConnEvent?.Invoke(null, new StringEventArgs(message));
    }
    //展示COMM连接消息;
    private static EventHandler? _commBufferDataProdEvent;
    public static event EventHandler? CommBufferDataProdEvent
    {
        add => _commBufferDataProdEvent ??= value;
        remove => _commBufferDataProdEvent -= value;
    }
    public static void RaiseCommBufferDataProdEvent(SocketDataBlock socketDataBlock)
    {
        _commBufferDataProdEvent?.Invoke(null, new SocketDataEventArgs(socketDataBlock));
    }
}