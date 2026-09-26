using MySqlConnector;

namespace HighMetroServer.Parameters;

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
        var builder = new MySqlConnectionStringBuilder
        {
            Server = DbHost,
            Port = (uint)DbPort,
            UserID = DbUser,
            Password = DbPassword,
            Database = DbDatabase,
            CharacterSet = "utf8mb4",
            Pooling = true,
            MinimumPoolSize = 2,
            MaximumPoolSize = 20,
            ConnectionTimeout = 10,
            ConnectionIdleTimeout = 300
        };
        return builder.ConnectionString;
    }
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(DbHost) &&
               DbPort is >= 1001 and <= 65535 &&
               !string.IsNullOrWhiteSpace(DbUser) &&
               !string.IsNullOrWhiteSpace(DbPassword);
    }
}