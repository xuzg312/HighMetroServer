using System;
using System.Net.Sockets;
using System.Threading;
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

    private Task? _heartTask;
    private CancellationTokenSource? _ctsHeart;
    private readonly SemaphoreSlim _sem = new (1,1);
    private bool _manClose;
    public HostConfigViewModel()
    {
        _start = false;
        _buildServer = false;
        _manClose = false;
        _hostInfo = ParaSetupModules.HostInfo!;
        HostState = "【 TCP端口监听状态：❌ 】";
        WeakReferenceMessenger.Default.Register(this);
        _ctsHeart = new CancellationTokenSource();
        _heartTask = Task.Run(() => HeartLoop(_ctsHeart.Token), _ctsHeart.Token);
    }
    private async Task HeartLoop(CancellationToken token)
    {
        try
        {
            await HeartLoopAsync(token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ParaSetupModules.RaiseAscDataProdEvent($"摄像头在线监听顶层异常：{ex.Message}【{currDateTime}】");
        }
    }
    private async Task HeartLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(PublicConst.HeartTcp, token);
            await _sem.WaitAsync(token);
            try
            {
                if (_manClose)
                    continue;
                if (_start)
                {
                    var isOnLine = await CheckIsLine(token);
                    if (!isOnLine)
                    {
                        await CloseAsync();
                        await OpenAsync(); 
                    }
                    continue;
                }
                await OpenAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                ParaSetupModules.RaiseAscDataProdEvent($"摄像头在线监听异常：{ex.Message}【{currDateTime}】");
            }
            finally
            {
                _sem.Release(); 
            }
        }
    }
    private async Task<bool> CheckIsLine(CancellationToken token)
    {
        using var client = new TcpClient();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(20000);
        try
        {
            await client.ConnectAsync(_hostInfo.Ip, _hostInfo.Port, cts.Token);
            await using var ns = client.GetStream();
            var publicUntil = new PublicUntil();
            var iPosition = 0;
            var data = new byte[11];
            //帧头，2字节；
            data[iPosition++] = 0XEB;
            data[iPosition++] = 0XAA;
            //长度，1字节；
            data[iPosition++] = 0X07;
            //工控机编号，2字节；
            var id = (ushort)_hostInfo.Bh;
            publicUntil.GetShort(id, data, iPosition);
            iPosition += 2;
            //主板ID，2字节；
            data[iPosition++] = 0X00;
            data[iPosition++] = 0X00;
            //功能码，1字节；
            data[iPosition++] = 0X55;
            //备用；
            data[iPosition++] = 0X99;
            data[iPosition++] = 0X99;
            //帧尾，1字节；
            data[iPosition] = 0XED;
            await ns.WriteAsync(data, cts.Token);
            await ns.FlushAsync(cts.Token);
            var resp = new byte[64];
            var read = await ns.ReadAsync(resp, cts.Token);
            if (read == 11 && resp[0] == 0XEB && resp[1] == 0XAA && resp[7] == 0X55)
            {
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ParaSetupModules.RaiseAscDataProdEvent($"检测TCPServer服务异常：{ex.Message}【{currDateTime}】");
            return false;
        }
        finally
        {
            try
            {
                await cts.CancelAsync();
            }
            catch (Exception)
            {
                //忽略
            }
            try
            {
                client.Close();
            }
            catch (Exception)
            {
                //忽略
            }
        }
    }
    private async Task OpenAsync()
    {
        await OnOpen();
    }
    private async Task OnOpen()
    {
        if (!_buildServer)
        {
            ParaSetupModules.TcpServerBufferDataProdEvent += OnShowTcpServerDataProdEvent;
            ParaSetupModules.TcpClientConnEvent += OnClientConnEvent;
            _tcpServer = new TcpServerListenerImpl(_hostInfo, PublicConst.TcpDataParseTask); //建立2个消费者线程；
            _buildServer = true;
            _hostInfo.TcpServer = _tcpServer;
        }
        if (_tcpServer!.Start())
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _start = true;
                HostState = "【 TCP端口监听状态：✅ 】";
                OpenCommand.NotifyCanExecuteChanged();
                CloseCommand.NotifyCanExecuteChanged();
            });
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _start = false;
                OpenCommand.NotifyCanExecuteChanged();
                CloseCommand.NotifyCanExecuteChanged();
            });
            ParaSetupModules.RaiseAscDataProdEvent("启动Tcp-Server失败！");
        }
    }
    private async Task CloseAsync()
    {
        await OnClose();
    }
    private async Task OnClose()
    {
        _tcpServer!.CloseServer();
        _start = false;
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            HostState = "【 TCP端口监听状态：❌ 】";
            OpenCommand.NotifyCanExecuteChanged();
            CloseCommand.NotifyCanExecuteChanged();
        });
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
    public void Start()
    {
        if (PublicConst.SelfStart != 1 || _start)
            return;
        _= StartOpen();
    }
    private async Task StartOpen()
    {
        await Task.Delay(1000).ConfigureAwait(false); 
        await Open();
    }
    [RelayCommand(CanExecute = nameof(CanOpen))]
    private async Task Open()
    {
        await _sem.WaitAsync(_ctsHeart!.Token);
        try
        {
            _manClose = false;
            await OnOpen();
        }
        finally
        {
            _sem.Release();
        }
    }
    [RelayCommand(CanExecute= nameof(CanClose))]
    private async Task Close()
    {
        await _sem.WaitAsync(_ctsHeart!.Token);
        try
        {
            _manClose = true;
            await OnClose();
        }        
        finally
        {
            _sem.Release();
        }
    }
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
        _ = ReceiveTcpData(socketDataBlock);
    }
    private async Task ReceiveTcpData(SocketDataBlock socketDataBlock)
    {
        try
        {
            await ParseData(socketDataBlock);
        }
        catch (Exception ex)
        {
            var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ParaSetupModules.RaiseAscDataProdEvent($"解析TCP数据异常：{ex.Message}，【{currentTime}】");
        }
    }
    private async Task ParseData(SocketDataBlock socketDataBlock)
    {
        await Task.Delay(10).ConfigureAwait(false); 
        //解析tcp-client消息，转发到对应的串口；
        var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var tcpDataBean = ParseClientData.ParseTcpClientData(socketDataBlock);
        if (tcpDataBean == null)
        {
            //数据无效，显示到错误日志框；
            ParaSetupModules.RaiseHexDataProdEvent(socketDataBlock);
            return;
        }
        if (!tcpDataBean.TurnComm)
        {
            //无需发送到串口；
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
                        onLine = await camRemoteLinkImpl.CheckOnLine();
                    }
                    //转发到TcpClient;
                    var iPosition = 7;
                    socketDataBlock.Content![iPosition] = (byte)(onLine ? 0XCE : 0XDE);
                    //发送摄像机状态到客户端；
                    _tcpServer!.IdentifyInfo(socketDataBlock, tcpDataBean);
                    ParaSetupModules.RaiseTcpClientConnEvent($"发送摄像机连接状态到客户端！【{currentTime}】");
                    break;
                case PublicConst.IdentifyPhoto:
                    var fileData = ParseClientData.GetPhotoFile(tcpDataBean);
                    if (fileData != null)
                    {
                        _tcpServer!.SendPhotoFile(socketDataBlock, tcpDataBean, fileData);
                        ParaSetupModules.RaiseTcpClientConnEvent($"发送拍照图片到客户端！【{currentTime}】");
                    }
                    else
                    {
                        var value01 = $"文件【{{tcpDataBean.FileName}}】不存在！【{currentTime}】";
                        ParaSetupModules.RaiseTcpClientConnEvent(value01);
                    }
                    break;
                case PublicConst.IdentifySelfCheck:
                    _tcpServer!.IdentifyInfo(socketDataBlock, tcpDataBean);
                    break;
                default:
                    var value00 = $"工控机HostBh【{tcpDataBean.HostBh}】,请求功能码无效！【{currentTime}】";
                    ParaSetupModules.RaiseTcpClientConnEvent(value00);
                    break;
            }
            return;
        }
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
                ParaSetupModules.RaiseTcpClientConnEvent($"主板ID【{tcpDataBean.Id}】：向对应的串口发送数据！【{currentTime}】");
                bFind = true;
            }
        }
        if (!bFind)
        {
            //主板未找到，说明客户端关联的主板有误！
            var value00 = $"工控机HostBh【{tcpDataBean.HostBh}】,主板ID【{tcpDataBean.Id}】未找到！【{currentTime}】";
            ParaSetupModules.RaiseTcpClientConnEvent(value00);
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
    private async Task ClearResource()
    {
        await _sem.WaitAsync(_ctsHeart!.Token);
        try
        {
            if (_start)
            {
                _tcpServer?.CloseServer();
            }
            try
            {
                await _ctsHeart.CancelAsync();
            }
            catch
            {
                //忽略；
            }
            try
            {
                _ctsHeart.Dispose();
            }
            catch
            {
                //忽略；
            }
            try
            {
                await _heartTask!;
            }
            catch
            {
                //忽略；
            }
            _ctsHeart = null;
            _heartTask = null;
            _start = false;
        }
        finally
        {
            _sem.Release();
        }
    }
    public void Receive(AppCleanupMessage message)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        Console.WriteLine("释放TCP资源！");
        _= ClearResource();
    }
}