using System.Collections.Generic;
using HighMetro.BaseModel;

namespace HighMetro.Models;

public class ResultHostInfo
{
    public List<HostInfo> HostList { get; set; } = [];
    public ResultInfo  ReturnInfo{ get; set; } = null!;
}