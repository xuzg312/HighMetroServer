using System;
using HighMetroServer.BaseModel;

namespace HighMetroServer.Event;

public class LogEventArgs : EventArgs
{
    public LogBean Message { get; }
    public LogEventArgs(LogBean logBean)
    {
        Message = logBean;
    }
}