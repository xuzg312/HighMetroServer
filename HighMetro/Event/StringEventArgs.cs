using System;

namespace HighMetro.Event;

public class StringEventArgs : EventArgs
{
    public string Message { get; }
    public StringEventArgs(string message)
    {
        Message = message;
    }
}