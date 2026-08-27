using System;
using Avalonia.Threading;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using HighMetroServer.BaseModel;
using HighMetroServer.ClassLib;
using HighMetroServer.Event;
using HighMetroServer.Models;

namespace HighMetroServer.ViewModels;

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

    private readonly ConcurrentQueue<string> _log4Queue = [];
    private readonly StringBuilder _stringBuilder = new ();
    private int _pendingUpdate;
    public MainPageViewModel()
    {
        ParaSetupModules.AscDataProdEvent += OnShowAscDataProdEvent;
        ParaSetupModules.HexDataProdEvent += OnShowHexDataProdEvent;
        HostConfig = new HostOptions(
            ParaSetupModules.HostInfo!.Ip, 
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
            return;
        }
        var serialCommInfo = ParaSetupModules.SerialCommList[0];
        SerialConfig1 = new SerialPortOptions(
            serialCommInfo.Id,
            serialCommInfo.Name,
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
        SerialConfig2 = new SerialPortOptions(
            serialCommInfo.Id,
            serialCommInfo.Name,
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
        _log4Queue.Enqueue(message);
        while (_log4Queue.Count > PublicConst.MaxLogLines)
        {
            _log4Queue.TryDequeue(out _);
        }
        if (Interlocked.Exchange(ref _pendingUpdate, 1) == 1)
        {
            return; // 已有更新等待，跳过本次
        }
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                _stringBuilder.Clear();
                foreach (var line in _log4Queue)
                {
                    _stringBuilder.AppendLine(line);
                }
                AscMessageText = _stringBuilder.ToString();
            }
            finally
            {
                Interlocked.Exchange(ref _pendingUpdate, 0);
            }
        }, DispatcherPriority.Normal);
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
}