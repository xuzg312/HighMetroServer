using HighMetroServer.Services;

namespace HighMetroServer.BaseModel;

public class HostInfo
{
    public int Bh { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get ; set; } = string.Empty;
    public string Ip { get ; set ; } = string.Empty;
    public int Port { get; set; }
    public string Effect { get ; set ; } = string.Empty;
    public bool Open { get; set; }
    public TcpServerListenerImpl? TcpServer { get; set; }
}