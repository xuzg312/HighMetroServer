using System.Collections.Generic;
using HighMetro.BaseModel;

namespace HighMetro.Models;

public class ResultCamAlarmInfo
{
    public ResultInfo  ReturnInfo{ get; set; } = null!;
    public List<CameraBean> CameraList { get; set; } = [];
}