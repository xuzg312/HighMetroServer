using HighMetroServer.BaseModel;
using HighMetroServer.Models;
using HighMetroServer.Parameters;

namespace HighMetroServer.Services;

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
    ResultInfo AddHeart(MainInfoBean mainInfoBean);
    ResultInfo SavePersonDay(MainInfoBean mainInfoBean);
    ResultInfo AddError(CameraBean cameraBean);
    ResultInfo AddAlarm(CameraBean cameraBean);
    ResultInfo EditCommInfo(SerialComm serialComm);
    ResultInfo AddCommInfo(SerialComm serialComm);
    ResultCamAlarmInfo QueryCamAlarm(CameraBean cameraBean,DataBaseQueryPage page );
    ResultInfo QueryCamAlarmCount(CameraBean cameraBean);
    ResultInfo AddHost(HostInfo hostInfo,DbSetting dbSetting);
    ResultInfo EditHost(HostInfo hostInfo,DbSetting dbSetting);
}