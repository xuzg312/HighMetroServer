using System.Collections.Generic;
using HighMetroServer.BaseModel;

namespace HighMetroServer.Models;

public class ResultHostInfo
{
    public List<HostInfo> HostList { get; set; } = [];
    public ResultInfo  ReturnInfo{ get; set; } = null!;
}