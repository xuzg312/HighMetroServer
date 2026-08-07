using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetro.BaseModel;
using HighMetro.ClassLib;
using HighMetro.Event;
using HighMetro.Models;
using HighMetro.Services;

namespace HighMetro.ViewModels;

public partial class HostConfigViewModel : ObservableObject
{
    // 1. 接收外部传入的配置数据
    [ObservableProperty]
    private HostOptions? _config;
    
    // 2. 接收外部传入的只读状态
    [ObservableProperty] 
    private bool _isReadOnly;

    [ObservableProperty] 
    private string _hostState;
    // UI绑定字段
    [ObservableProperty]
    private string _ip = string.Empty;

    [ObservableProperty]
    private int _port = 3000;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isInfoHit;
    
    [ObservableProperty]
    private string _messageText = string.Empty;

    private bool _start;
    private readonly HostInfo _hostInfo;
    private readonly TcpServerListenerImpl _tcpServer;
    private List<ISerialComm> _mainBordSerialCommList;
    private List<SerialComm> _serialList;
    public HostConfigViewModel(bool isReadOnly)
    {
        _isInfoHit = true;
        IsReadOnly = isReadOnly;
        _start = false;
        _hostInfo = ParaSetupModules.HostInfo;
        HostState = "【工控机，启动状态：❌ 】";
        _hostInfo.BufferDataProdEvent += OnShowTcpServerDataProdEvent;
        _hostInfo.ClientConnEvent += OnClientConnEvent;
        _tcpServer = new TcpServerListenerImpl(_hostInfo, 2);//建立2个消费者线程；
    }
    partial void OnConfigChanged(HostOptions? value)
    {
        if (value is null)
            return;
        // 将外部HostOptions模型映射拷贝到UI属性
        Ip = value.Ip;
        Port = value.Port;
        Code = value.Code;
        Name = value.Name;
    }
    public void AppendMessage(string msg)
    {
        MessageText = $"[{DateTime.Now:HH:mm:ss}] {msg}";
    }
    /*partial void OnIpChanged(string value)
    {
        if(Config is not null) Config.Ip = value;
    }

    partial void OnPortChanged(int value)
    {
        if(Config is not null) Config.Port = value;
    }
    partial void OnCodeChanged(string value)
    {
        if(Config is not null) Config.Code = value;
    }
    partial void OnNameChanged(string value)
    {
        if(Config is not null) Config.Name = value;
    }*/
    [RelayCommand(CanExecute= nameof(CanOpen))]
    private void Open()
    {
        HostInfo? hostInfo = ParaSetupModules.HostInfo;
        if (hostInfo is not null)
        {
            if (_tcpServer.Start())
            {
                _start = true;
                HostState = "【工控机，启动状态：✅ 】";
                OpenCommand.NotifyCanExecuteChanged();
                CloseCommand.NotifyCanExecuteChanged();
            }
            else
            {
                _start = false;
                OpenCommand.NotifyCanExecuteChanged();
                CloseCommand.NotifyCanExecuteChanged();
                ParaSetupModules.HostInfo!.RaiseAscDataProdEvent("启动Tcp-Server失败！");
            }
        }
    }
    [RelayCommand(CanExecute= nameof(CanClose))]
    private void Close()
    {
        //关闭监听端口
        _tcpServer.CloseServer();
        _start = false;
        HostState = "【工控机，启动状态：❌ 】";
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
    private void OnClientConnEvent(object? obj, EventArgs arg)
    {
        if (arg is StringEventArgs stringEventArgs)
        {
            var message = stringEventArgs.Message;
            // 将更新操作提交到 UI 线程队列
            Dispatcher.UIThread.Post(() => { MessageText = message; });
        }
    }
    private void OnShowTcpServerDataProdEvent(object? obj, EventArgs arg)
    {
        if (arg is SocketDataEventArgs socketDataEventArgs)
        {
            SocketDataBlock socketDataBlock = socketDataEventArgs.Data;
            //解析tcp-client消息，转发到对应的串口；
            TcpDataBean tcpDataBean = ParseClientData.ParseTcpClientData(socketDataBlock);
            var bFind = false;
            var bSend = false;
            if (tcpDataBean != null)
            {
                if (!tcpDataBean.TurnComm)
                {
                    switch (tcpDataBean.Type)
                    {
                        case PublicConst.IdentifyAll:
                        case PublicConst.IdentifyHeart:
                            //检测摄像机是否在线？
                            bool onLine = false;////////hardUserControl1.checkOnLine();
                            //转发到TcpClient;
                            byte iPosition = 7;
                            socketDataBlock.Content[iPosition] = (byte)(onLine ? 0XCE : 0XDE);
                            //发送摄像机状态到客户端；
                            _tcpServer.IdentifyInfo(socketDataBlock, tcpDataBean);
                            return;
                        case PublicConst.IdentifyPhoto:
                            byte[] filedata = ParseClientData.GetPhotoFile(tcpDataBean);
                            if (filedata != null)
                            {
                                _tcpServer.SendPhotoFile(socketDataBlock, tcpDataBean, filedata);
                            }
                            else
                            {
                                String value01 = "文件【" + tcpDataBean.FileName + "】不存在！\r\n";
                                ShowError(socketDataBlock, value01);
                            }

                            return;
                        default:
                            String value00 = "工控机Hostbh【" + tcpDataBean.HostBh + "】,请求功能码无效！\r\n";
                            ShowError(socketDataBlock, value00);
                            return;
                    }
                }
                else
                {
                    //需要发送到串口；
                    //协议中去掉hostid
                    //接收到有效信息，转发到串口；
                    if (_serialList != null)
                    {
                        SerialComm iserialComm;
                        if (_serialList.Count >= 1)
                        {
                            iserialComm = _serialList[0];
                            if (iserialComm.Hostbh == tcpDataBean.HostBh && iserialComm.Id == tcpDataBean.Id)
                            {
                                //找到主板，向对应的串口发送数据；
                                ////////bSend = commUserControl1.sendClientToComm(socketDataBlock);
                                bFind = true;
                            }
                        }

                        if (!bFind && _serialList.Count >= 2)
                        {
                            iserialComm = _serialList[1];
                            if (iserialComm.Hostbh == tcpDataBean.HostBh && iserialComm.Id == tcpDataBean.Id)
                            {
                                //找到主板，向对应的串口发送数据；
                                ////////bSend = commUserControl2.sendClientToComm(socketDataBlock);
                                bFind = true;
                            }
                        }
                    }

                    if (!bFind)
                    {
                        //主板未找到，说明客户端关联的主板有误！
                        String value00 = "工控机Hostbh【" + tcpDataBean.HostBh + "】,主板ID【" + tcpDataBean.Id + "】未找到！\r\n";
                        ShowError(socketDataBlock, value00);
                    }
                    else
                    {
                        if (!bSend)
                        {
                            //找到主板，向对应的串口发送数据失败；
                            String value00 = "工控机Hostbh【" + tcpDataBean.HostBh + "】,主板ID【" + tcpDataBean.Id +
                                             "】发送失败！\r\n";
                            ShowError(socketDataBlock, value00);
                        }
                    }
                }
            }
            else
            {
                //数据无效，显示到错误日志框；
                _hostInfo.RaiseHexDataProdEvent(socketDataBlock);
            }
        }
    }
    private void ShowError(SocketDataBlock socketDataBlock, string errorMessage)
    {
        _hostInfo.RaiseAscDataProdEvent(errorMessage);
    }
}