using System.Collections.Generic;
using HighMetro.BaseModel;

namespace HighMetro.Models;

public class ResultSerialCommInfo
{
    public List<SerialCommInfo> SerialCommList { get; set; } = [];
    public ResultInfo  ReturnInfo{ get; set; } = null!;
}