using System;
using HighMetro.BaseModel;

namespace HighMetro.Event;

public class SocketDataEventArgs : EventArgs
{
    public SocketDataBlock Data { get; }
    public SocketDataEventArgs(SocketDataBlock data) => Data = data;
}