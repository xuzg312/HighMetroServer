namespace HighMetro.Models;

public class HostInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public HostInfo(string code, string name)
    {
        Code = code;
        Name = name;
    }
}