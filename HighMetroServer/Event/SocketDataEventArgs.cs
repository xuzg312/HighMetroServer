using System;
using HighMetroServer.BaseModel;

namespace HighMetroServer.Event;

public class SocketDataEventArgs : EventArgs
{
    public SocketDataBlock Data { get; }
    public SocketDataEventArgs(SocketDataBlock data) => Data = data;
}