using HighMetro.Models;
using HighMetro.Parameters;

namespace HighMetro.Services;

public interface IConfigService
{
    DbSetting LoadDbConfig();
    void SaveDbConfig(DbSetting setting);
    LoginSetting LoadLoginConfig();
    void SaveLoginConfig(LoginSetting setting);
    
    HostSetting LoadHostConfig();
    
    void SaveHostConfig(HostSetting setting);
}