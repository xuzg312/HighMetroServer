using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using HighMetro.BaseModel;
using HighMetro.Models;
using HighMetro.Services;

namespace HighMetro.ViewModels;

public partial class MainPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private SerialPortOptions? _serialConfig;

    [ObservableProperty]
    private HostOptions? _hostConfig;
    
    public MainPageViewModel(HostInfo hostInfo, HardInfo hardInfo, List<SerialCommInfo> serialCommList,IDbService dbService)
    {
        ParaSetupModules.HostInfo = hostInfo;
        ParaSetupModules.HardInfo = hardInfo;
        ParaSetupModules.SerialCommList = serialCommList;
        ParaSetupModules.DbService = dbService;
        HostConfig = new HostOptions(hostInfo.Ip, hostInfo.Port, hostInfo.Code, hostInfo.Name);
        if (serialCommList.Count > 0)
        {
            SerialCommInfo serialCommInfo = serialCommList[0];
            SerialConfig = new SerialPortOptions(
                serialCommInfo.CommName,
                serialCommInfo.BaudRate,
                serialCommInfo.DataBits,
                serialCommInfo.Parity,
                serialCommInfo.StopBits
            );
        }
    }
}