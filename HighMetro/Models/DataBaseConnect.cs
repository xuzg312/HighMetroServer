namespace HighMetro.Models;

public class DataBaseConnect
{
    private static DataBaseConnect? _instance;
    private static readonly object LockObj = new object();

    private string? _connectionString;
    // 私有构造函数，禁止外部 new
    private DataBaseConnect()
    {
    }

    // 全局访问入口
    public static DataBaseConnect Instance
    {
        get
        {
            lock (LockObj)
            {
                _instance ??= new DataBaseConnect();
                return _instance;
            }
        }
    }

    public void SetDataBaseConn(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string GetConnectionString()
    {
        return _connectionString?? string.Empty;
    }
}