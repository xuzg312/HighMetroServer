namespace HighMetro.Parameters;

public class DbSetting
{
    //数据库参数
    public string DbHost { get; set; } = "";
    public int DbPort { get; set; } = 3306;
    public string DbDatabase { get; set; } = "HighSpeed";
    public string DbUser { get; set; } = "";
    public string DbPassword { get; set; } = "";
    public string GetConnectionString()
    {
        return "server=" + DbHost + 
               ";port=" + DbPort+ 
               ";user=" + DbUser + 
               ";password=" + DbPassword + ";"+
               "database="+DbDatabase+";"+
               "charset=utf8mb4;" +
               //"Pooling=true;"+         // 启用连接池（默认true）
               //"MinimumPoolSize=5;"+    // 最小连接数（预热连接）
               //"MaximumPoolSize=100;"+  // 最大连接数
               "ConnectionTimeout=10;"+ // 连接超时（秒）
               "ConnectionLifeTime=300;"; // 连接生命周期（秒）
    }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(DbHost) &&
               DbPort is >= 1001 and <= 65535 &&
               !string.IsNullOrWhiteSpace(DbUser) &&
               !string.IsNullOrWhiteSpace(DbPassword);
    }
}