using System.Collections.Generic;
using HighMetro.BaseModel;
using HighMetro.Services;

namespace HighMetro.Models;

public class ParaSetupModules
{
    public static HostInfo? HostInfo{ get; set; }
    public static HardInfo? HardInfo{ get; set; }
    public static List<SerialCommInfo>? SerialCommList{ get; set; }
    public static UserInfo? UserInfo{ get; set; }
    
    public static IDbService? DbService{ get; set; }
}