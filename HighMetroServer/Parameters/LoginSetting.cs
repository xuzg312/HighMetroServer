namespace HighMetroServer.Parameters;

public class LoginSetting
{
    //登陆参数
    public string LoginUser { get; set; } = "";
    public string LoginPassword { get; set; } = "";
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(LoginUser) &&
               !string.IsNullOrWhiteSpace(LoginPassword);
    }
}