using HighMetro.Services;

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
    public bool IsValid()
    {
        return HostBh>0 && 
               Bh>0 && 
               !string.IsNullOrWhiteSpace(Ip) && 
               Port>1000 && 
               !string.IsNullOrWhiteSpace(UserName) && 
               !string.IsNullOrWhiteSpace(PassWord);
    }
    public CamRemoteLinkImpl? CamRemoteLinkImpl{ get; set; }
}