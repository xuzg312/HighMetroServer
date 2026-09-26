using System.Threading.Tasks;
using HighMetroServer.BaseModel;
using HighMetroServer.Models;
using HighMetroServer.Parameters;

namespace HighMetroServer.Services;

public interface IDbService
{
    Task<ResultInfo> TestConnection(DbSetting setting);
    Task<ResultInfo> VerifyUser(LoginSetting loginSetting,DbSetting dbSetting);
    Task<ResultInfo> VerifyHost(HostSetting hostSetting,DbSetting dbSetting);
    Task<ResultHostInfo> GetHostList(DbSetting dbSetting);
    Task<ResultInfo> GetHostInfo(HostInfo hostInfo);
    Task<ResultInfo> GetHardCamera(HardInfo hardInfo);
    Task<ResultSerialCommInfo> GetCommInfoList(HostInfo hostInfo, string commType);
    Task<ResultInfo> AddHardCamera(HardInfo hardInfo);
    Task<ResultInfo> EditHardCamera(HardInfo hardInfo);
    Task<ResultInfo> AddHeart(MainInfoBean mainInfoBean);
    Task<ResultInfo> SavePersonDay(MainInfoBean mainInfoBean);
    Task<ResultInfo> AddError(CameraBean cameraBean);
    Task<ResultInfo> AddAlarm(CameraBean cameraBean);
    Task<ResultInfo> EditCommInfo(SerialComm serialComm);
    Task<ResultInfo> AddCommInfo(SerialComm serialComm);
    Task<ResultCamAlarmInfo> QueryCamAlarm(CameraBean cameraBean,DataBaseQueryPage page );
    Task<ResultInfo> QueryCamAlarmCount(CameraBean cameraBean);
    Task<ResultInfo> AddHost(HostInfo hostInfo,DbSetting dbSetting);
    Task<ResultInfo> EditHost(HostInfo hostInfo,DbSetting dbSetting);
}