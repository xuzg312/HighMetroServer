namespace HighMetro.Models;

public class DbSetting
{
    //数据库参数
    public string DbHost { get; set; } = "";
    public int DbPort { get; set; } = 3306;
    private string DbDatabase { get; set; } = "HighSpeed";
    public string DbUser { get; set; } = "";
    public string DbPassword { get; set; } = "";
    public string GetConnectionString()
    {
        return $"server={DbHost};port={DbPort};database={DbDatabase};uid={DbUser};pwd={DbPassword};SslMode=None;";
    }
}