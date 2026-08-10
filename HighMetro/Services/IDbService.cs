using HighMetro.BaseModel;
using HighMetro.Models;
using HighMetro.Parameters;

namespace HighMetro.Services;

public interface IDbService
{
    ResultInfo TestConnection(DbSetting setting);
    ResultInfo VerifyUser(LoginSetting loginSetting,DbSetting dbSetting);
    ResultInfo VerifyHost(HostSetting hostSetting,DbSetting dbSetting);
    ResultHostInfo GetHostList(DbSetting dbSetting);
    ResultInfo GetHostInfo(HostInfo hostInfo);
    ResultInfo GetHardCamera(HardInfo hardInfo);
    ResultSerialCommInfo GetCommInfoList(HostInfo hostInfo, string commType);
    ResultInfo AddHardCamera(HardInfo hardInfo);
    ResultInfo EditHardCamera(HardInfo hardInfo);
}