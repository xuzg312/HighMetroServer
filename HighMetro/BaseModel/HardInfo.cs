namespace HighMetro.BaseModel;

public class HardInfo
{
    public int HostBh { get; set; }
    public int Bh { get; set; }
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
    public string UserName { get; set ; }= string.Empty;
    public string PassWord { get ; set ; }= string.Empty;
    public bool Open { get; set; } = false;
    public int UserId { get; set; }
    public int RealHandle { get; set; }
    public string Type { get ; set; }= string.Empty;
}