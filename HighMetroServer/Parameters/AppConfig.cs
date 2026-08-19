using HighMetroServer.Models;

namespace HighMetroServer.Parameters;

public class AppConfig
{
    // 对应 JSON 中的 "database" 节点
    public DbSetting? Database { get; set; } = new();
    public LoginSetting? LoginInfo { get; set; } = new();
    public HostSetting? HostInfo { get; set; } = new();
}