using HighMetro.Models;
using MySqlConnector;

namespace HighMetro.Services;

public class DbService: IDbService
{
    public bool TestConnection(DbSetting setting)
    {
        try
        {
            using var conn = new MySqlConnection(setting.GetConnectionString());
            conn.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool VerifyUser(string username, string password)
    {
        // =========【重要】自行替换为你的用户表查询SQL =========
        // 示例SQL：SELECT COUNT(1) FROM sys_user WHERE username=@user AND password=@pwd
        // 请勿明文存储密码！生产环境使用哈希
        return username == "admin" && password == "123456";
    }
}