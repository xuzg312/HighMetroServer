using System.Collections.Generic;
using HighMetroServer.BaseModel;

namespace HighMetroServer.Models;

public class ResultSerialCommInfo
{
    public List<SerialCommInfo> SerialCommList { get; set; } = [];
    public ResultInfo  ReturnInfo{ get; set; } = null!;
}