namespace HighMetro.Parameters;

public class HostSetting
{
    //工控机参数
    public int Bh { get; set; }
    public bool IsValid()
    {
        return Bh>0;
    }
}