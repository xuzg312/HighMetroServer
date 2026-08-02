using HighMetro.Models;

namespace HighMetro.Services;

public interface IDbService
{
    bool TestConnection(DbSetting setting);
    bool VerifyUser(string username, string password);
}