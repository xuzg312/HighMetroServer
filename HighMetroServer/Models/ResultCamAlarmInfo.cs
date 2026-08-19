using System.Collections.Generic;
using HighMetroServer.BaseModel;

namespace HighMetroServer.Models;

public class ResultCamAlarmInfo
{
    public ResultInfo  ReturnInfo{ get; set; } = null!;
    public List<CameraBean> CameraList { get; set; } = [];
}