using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HighMetroServer.BaseModel;
using HighMetroServer.ClassLib;
using HighMetroServer.Event;
using HighMetroServer.Message;
using HighMetroServer.Models;
using HighMetroServer.Services;

namespace HighMetroServer.ViewModels;

public partial class HostConfigViewModel : ObservableObject, IRecipient<AppCleanupMessage>
{
    [ObservableProperty]
    private HostOptions? _config;
    
    [ObservableProperty] 
    private string _hostState;
    
    [ObservableProperty]
    private string _ip = string.Empty;

    [ObservableProperty]
    private int _port = 3000;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string _messageText = string.Empty;

    private bool _start;
    private readonly HostInfo _hostInfo;
    private TcpServerListenerImpl? _tcpServer;
    private bool _buildServer;
    
    public HostConfigViewModel()
    {
        _start = false;
        _buildServer = false;
        _hostInfo = ParaSetupModules.HostInfo!;
        HostState = "【 TCP端口监听状态：❌ 】";
        WeakReferenceMessenger.Default.RegisterAll(this);
    }
    partial void OnConfigChanged(HostOptions? value)
    {
        if (value is null)
            return;
        Ip = value.Ip;
        Port = value.Port;
        Code = value.Code;
        Name = value.Name;
    }
    public async Task Start()
    {
        if (PublicConst.SelfStart == 1)
        {
            if (!_start)
            {
                await Task.Delay(500); 
                Open();
            }
        }
    }
    [RelayCommand(CanExecute = nameof(CanOpen))]
    private void Open()
    {
        if (!_buildServer)
        {
            _hostInfo.BufferDataProdEvent += OnShowTcpServerDataProdEvent;
            _hostInfo.ClientConnEvent += OnClientConnEvent;
            _tcpServer = new TcpServerListenerImpl(_hostInfo, PublicConst.TcpDataParseTask); //建立2个消费者线程；
            _buildServer = true;
            _hostInfo.TcpServer= _tcpServer;
        }
        if (_tcpServer!.Start())
        {
            _start = true;
            HostState = "【 TCP端口监听状态：✅ 】";
            OpenCommand.NotifyCanExecuteChanged();
            CloseCommand.NotifyCanExecuteChanged();
        }
        else
        {
            _start = false;
            OpenCommand.NotifyCanExecuteChanged();
            CloseCommand.NotifyCanExecuteChanged();
            ParaSetupModules.RaiseAscDataProdEvent("启动Tcp-Server失败！");
        }
    }
    [RelayCommand(CanExecute= nameof(CanClose))]
    private void Close()
    {
        //关闭监听端口
        _tcpServer!.CloseServer();
        _start = false;
        HostState = "【 TCP端口监听状态：❌ 】";
        OpenCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();
    }
    // 执行条件：!_start （_start为false时按钮可用）
    private bool CanOpen()
    {
        return !_start; 
    }
    private bool CanClose()
    {
        return _start; 
    }
    //收到客户端连接；
    private void OnShowTcpServerDataProdEvent(object? obj, EventArgs arg)
    {
        if (arg is not SocketDataEventArgs socketDataEventArgs)
        {
            return;
        }
        var socketDataBlock = socketDataEventArgs.Data;
        //解析tcp-client消息，转发到对应的串口；
        var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        try
        {
            var tcpDataBean = ParseClientData.ParseTcpClientData(socketDataBlock);
            if (tcpDataBean == null)
            {
                //数据无效，显示到错误日志框；
                ParaSetupModules.RaiseHexDataProdEvent(socketDataBlock);
                return;
            }
            if (!tcpDataBean.TurnComm)
            {
                switch (tcpDataBean.Type)
                {
                    case PublicConst.IdentifyAll:
                    case PublicConst.IdentifyHeart:
                        //检测摄像机是否在线？
                        var camInfo = ParaSetupModules.CamInfo!;
                        var onLine = false;
                        var camRemoteLinkImpl = camInfo.CamRemoteLinkImpl;
                        if (camRemoteLinkImpl != null && camRemoteLinkImpl.GetUserId()>=0)
                        {
                            onLine = camRemoteLinkImpl.CheckOnLine();
                        }
                        //转发到TcpClient;
                        var iPosition = 7;
                        socketDataBlock.Content![iPosition] = (byte)(onLine ? 0XCE : 0XDE);
                        //发送摄像机状态到客户端；
                        _tcpServer!.IdentifyInfo(socketDataBlock, tcpDataBean);
                        _hostInfo.RaiseClientConnEvent($"发送摄像机连接状态到客户端！【{currentTime}】");
                        break;
                    case PublicConst.IdentifyPhoto:
                        var fileData = ParseClientData.GetPhotoFile(tcpDataBean);
                        if (fileData != null)
                        {
                            _tcpServer!.SendPhotoFile(socketDataBlock, tcpDataBean, fileData);
                            _hostInfo.RaiseClientConnEvent($"发送拍照图片到客户端！【{currentTime}】");
                        }
                        else
                        {
                            var value01 = $"文件【{{tcpDataBean.FileName}}】不存在！【{currentTime}】";
                            _hostInfo.RaiseClientConnEvent(value01);
                        }
                        break;
                    default:
                        var value00 = $"工控机HostBh【{tcpDataBean.HostBh}】,请求功能码无效！【{currentTime}】";
                        _hostInfo.RaiseClientConnEvent(value00);
                        break;
                }
            }
            else
            {
                //需要发送到串口；
                //协议中去掉hostId
                //接收到有效信息，转发到串口；
                var bFind = false;
                foreach (var item in ParaSetupModules.SerialCommList!)
                {
                    if (item.CommSerialImpl == null)
                    {
                        continue;
                    }
                    if (item.HostBh == tcpDataBean.HostBh && item.Id == tcpDataBean.Id)
                    {
                        //找到主板，向对应的串口发送数据；
                        item.CommSerialImpl.SendMessage(socketDataBlock.Content!, 0, socketDataBlock.Length);
                        _hostInfo.RaiseClientConnEvent($"主板ID【{tcpDataBean.Id}】：向对应的串口发送数据！【{currentTime}】");
                        bFind = true;
                    }
                }
                if (!bFind)
                {
                    //主板未找到，说明客户端关联的主板有误！
                    var value00 = $"工控机HostBh【{tcpDataBean.HostBh}】,主板ID【{tcpDataBean.Id}】未找到！【{currentTime}】";
                    _hostInfo.RaiseClientConnEvent(value00);
                }
            }
        }
        catch (Exception ex)
        {
            ParaSetupModules.RaiseAscDataProdEvent($"解析TCP数据异常：{ex.Message}【{currentTime}】");
        }
    }
    private void OnClientConnEvent(object? obj, EventArgs arg)
    {
        if (arg is not StringEventArgs stringEventArgs)
        {
            return;
        }
        var message = stringEventArgs.Message;
        Dispatcher.UIThread.Post(() => { MessageText = message; });
    }
    private void ClearResource()
    {
        _tcpServer?.CloseServer();
    }
    public void Receive(AppCleanupMessage message)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        ClearResource();
        Console.WriteLine("释放TCP资源！");
    }
}