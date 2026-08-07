using System;
using System.Collections.Generic;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using HighMetro.BaseModel;
using HighMetro.ClassLib;
using HighMetro.Event;
using HighMetro.Models;
using HighMetro.Services;

namespace HighMetro.ViewModels;

public partial class MainPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private SerialPortOptions? _serialConfig;

    [ObservableProperty]
    private HostOptions? _hostConfig;
    
    [ObservableProperty]
    private CamOptions? _camConfig;
    
    [ObservableProperty]
    private string? _ascMessageText;
    
    [ObservableProperty]
    private string? _hexMessageText;
    
    public MainPageViewModel(HostInfo hostInfo, HardInfo hardInfo, List<SerialCommInfo> serialCommList,IDbService dbService)
    {
        ParaSetupModules.HostInfo = hostInfo;
        ParaSetupModules.HostInfo.AscDataProdEvent += OnShowTcpAscDataProdEvent;
        ParaSetupModules.HostInfo.HexDataProdEvent += OnShowTcpHexDataProdEvent;
        ParaSetupModules.CamInfo = hardInfo;
        ParaSetupModules.SerialCommList = serialCommList;
        ParaSetupModules.DbService = dbService;
        HostConfig = new HostOptions(hostInfo.Ip, hostInfo.Port, hostInfo.Code, hostInfo.Name);
        CamConfig = new CamOptions(hardInfo.Ip, hardInfo.Port, hardInfo.UserName);
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
    //ASC消息显示；
    private void OnShowTcpAscDataProdEvent(object? obj, EventArgs arg)
    {
        if (arg is StringEventArgs stringEventArgs)
        {
            var message = stringEventArgs.Message;
            // 将更新操作提交到 UI 线程队列
            Dispatcher.UIThread.Post(() => { AscMessageText = message; }); 
        }
    }
    //十六进制消息显示；
    private void OnShowTcpHexDataProdEvent(object? obj, EventArgs arg)
    {
        if (arg is SocketDataEventArgs socketDataEventArgs)
        {
            SocketDataBlock socketDataBlock = socketDataEventArgs.Data;
            ParseMessage parseMessage = new ParseMessage();
            var message = parseMessage.ParseHexMessage(socketDataBlock);
            // 将更新操作提交到 UI 线程队列
            Dispatcher.UIThread.Post(() => { HexMessageText = message; }); 
        }
    }
}