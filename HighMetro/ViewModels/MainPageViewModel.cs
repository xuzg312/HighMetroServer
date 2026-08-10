using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    private HostOptions _hostConfig;
    
    [ObservableProperty]
    private CamOptions _camConfig;
    
    [ObservableProperty]
    private SerialPortOptions? _serialConfig1;

    [ObservableProperty]
    private SerialPortOptions? _serialConfig2;

    [ObservableProperty]
    private string? _ascMessageText;
    
    [ObservableProperty]
    private string? _hexMessageText;
    
    public MainPageViewModel()
    {
        ParaSetupModules.HostInfo!.AscDataProdEvent += OnShowAscDataProdEvent;
        ParaSetupModules.HostInfo.HexDataProdEvent += OnShowHexDataProdEvent;
        HostConfig = new HostOptions(
            ParaSetupModules.HostInfo.Ip, 
            ParaSetupModules.HostInfo.Port, 
            ParaSetupModules.HostInfo.Code, 
            ParaSetupModules.HostInfo.Name);
        CamConfig = new CamOptions(
            ParaSetupModules.CamInfo!.Ip, 
            ParaSetupModules.CamInfo.Port, 
            ParaSetupModules.CamInfo.UserName);
    }
    public void InitSerialPorts()
    {
        if (ParaSetupModules.SerialCommList!.Count == 0)
        {
            Console.WriteLine("初始化：SerialPortOptions");
            SerialConfig1 = new SerialPortOptions(
                "COM1",19200,0,0,0
            );
            return;
        }
        var serialCommInfo = ParaSetupModules.SerialCommList[0];
        serialCommInfo.AscDataProdEvent += OnShowAscDataProdEvent;
        SerialConfig1 = new SerialPortOptions(
            serialCommInfo.CommName,
            serialCommInfo.BaudRate,
            serialCommInfo.DataBits,
            serialCommInfo.Parity,
            serialCommInfo.StopBits
        );
        if (ParaSetupModules.SerialCommList!.Count == 1)
        {
            return;
        }
        serialCommInfo = ParaSetupModules.SerialCommList[1];
        serialCommInfo.AscDataProdEvent += OnShowAscDataProdEvent;
        SerialConfig2 = new SerialPortOptions(
            serialCommInfo.CommName,
            serialCommInfo.BaudRate,
            serialCommInfo.DataBits,
            serialCommInfo.Parity,
            serialCommInfo.StopBits
        );
    }
    //ASC消息显示；
    private void OnShowAscDataProdEvent(object? obj, EventArgs arg)
    {
        if (arg is not StringEventArgs stringEventArgs)
        {
            return;
        }
        var message = stringEventArgs.Message;
        // 将更新操作提交到 UI 线程队列
        Dispatcher.UIThread.Post(() => { AscMessageText = message; }); 
    }
    //十六进制消息显示；
    private void OnShowHexDataProdEvent(object? obj, EventArgs arg)
    {
        if (arg is not SocketDataEventArgs socketDataEventArgs)
        {
            return;
        }
        var socketDataBlock = socketDataEventArgs.Data;
        var parseMessage = new ParseMessage();
        var message = parseMessage.ParseHexMessage(socketDataBlock);
        // 将更新操作提交到 UI 线程队列
        Dispatcher.UIThread.Post(() => { HexMessageText = message; }); 
    }
    private void PostRefreshBindings()
    {
        Dispatcher.UIThread.Post(() =>
        {
            OnPropertyChanged(nameof(SerialConfig1));
            OnPropertyChanged(nameof(SerialConfig2));
            Console.WriteLine("-----PostRefreshBindings------");
        }, DispatcherPriority.ApplicationIdle);
    }
}