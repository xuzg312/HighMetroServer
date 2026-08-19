using System;

namespace HighMetroServer.Event;

public class StringEventArgs : EventArgs
{
    public string Message { get; }
    public StringEventArgs(string message)
    {
        Message = message;
    }
}