using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using HighMetroServer.BaseModel;
using HighMetroServer.ClassLib;
using HighMetroServer.Event;
using HighMetroServer.Models;

namespace HighMetroServer.ViewModels;

public partial class MainPageViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial HostOptions HostConfig { get; set; }

    [ObservableProperty]
    public partial CamOptions CamConfig { get; set; }

    [ObservableProperty]
    public partial SerialPortOptions? SerialConfig1 { get; set; }

    [ObservableProperty]
    public partial SerialPortOptions? SerialConfig2 { get; set; }

    [ObservableProperty]
    public partial string? AscMessageText { get; set; }

    [ObservableProperty]
    public partial string? HexMessageText { get; set; }

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
        if (ParaSetupModules.SerialCommList.Count == 1)
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
        _ = OnShowAscDataProdEventAsync();
    }
    private async Task OnShowAscDataProdEventAsync()
    {
        await Task.Delay(5).ConfigureAwait(false);
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
        _= OnShowHexDataProdEventAsync(socketDataBlock);
    }
    private async Task OnShowHexDataProdEventAsync(SocketDataBlock socketDataBlock)
    {
        await Task.Delay(5).ConfigureAwait(false);
        var parseMessage = new ParseMessage();
        var message = parseMessage.ParseHexMessage(socketDataBlock);
        // 将更新操作提交到 UI 线程队列
        Dispatcher.UIThread.Post(() => { HexMessageText = message; });
    }
}