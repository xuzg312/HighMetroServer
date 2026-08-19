namespace HighMetroServer.Parameters;

public class HardSetting
{
    //数据库参数
    public string Ip { get; set; } = "";
    public int Port { get; set; } = 3000;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Ip) &&
               !(Port < 1001 || Port > 65535) &&
               !string.IsNullOrWhiteSpace(UserName) &&
               !string.IsNullOrWhiteSpace(Password);
    }

}