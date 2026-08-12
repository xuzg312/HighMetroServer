using System;
using System.Collections.Generic;
using HighMetro.BaseModel;
using HighMetro.Event;
using HighMetro.Services;

namespace HighMetro.Models;

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
    public static EventHandler? GetAscDataProdEvent()
    {
        return _ascDataProdEvent;
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
}