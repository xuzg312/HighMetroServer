using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HighMetroServer.BaseModel;
using HighMetroServer.ClassLib;
using HighMetroServer.HikVision;
using HighMetroServer.Message;
using HighMetroServer.Models;
using HighMetroServer.Services;

namespace HighMetroServer.ViewModels;

public partial class CamConfigViewModel : ObservableObject,
    IRecipient<AppCleanupMessage>,
    IRecipient<CamMessage>
{
    [ObservableProperty]
    private CamOptions? _config;

    [ObservableProperty] 
    private string _camState;
    
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _ip = string.Empty;

    [ObservableProperty]
    private int _port;
    
    [ObservableProperty]
    private string _userName = string.Empty;
    
    [ObservableProperty]
    private string _messageText = string.Empty;

    private bool _start;
    private readonly CamRemoteLinkImpl _camRemoteLinkImpl;
    private Task? _heartTask;
    private CancellationTokenSource? _ctsHeart;
    private readonly SemaphoreSlim _sem = new (1,1);
    private bool _manClose;
    private bool _check;
    private bool _isValid;
    private readonly MsgCallBack _callBackForPerson;
    private readonly ConcurrentQueue<ChcNetSdk.NetDvrPdcAlramInfo> _receiveQueue = [];
    private readonly SemaphoreSlim _semaphoreSlim;
    private Task? _personTask;
    private readonly uint _recInfoSize = (uint)Marshal.SizeOf<ChcNetSdk.NetDvrPdcAlramInfo>();
    public CamConfigViewModel()
    {
        _start = false;
        _manClose = false;
        _check = false;
        _isValid = false;
        CamState = "【 摄像头连接状态：✘ 】";
        ParaSetupModules.PersonInfo = new PersonInfo();
        _semaphoreSlim = new SemaphoreSlim(0);
        _camRemoteLinkImpl = new CamRemoteLinkImpl();
        _callBackForPerson = OnCallBackForPerson;
        ParaSetupModules.CamInfo!.CamRemoteLinkImpl = _camRemoteLinkImpl;
        WeakReferenceMessenger.Default.Register<AppCleanupMessage>(this);
        WeakReferenceMessenger.Default.Register<CamMessage>(this);
        _ctsHeart = new CancellationTokenSource();
        _heartTask = Task.Run(() => HeartLoop(_ctsHeart.Token), _ctsHeart.Token);
        _personTask = Task.Run(() => PersonLoop(_ctsHeart.Token), _ctsHeart.Token);
    }
    //布防回调；
    private void OnCallBackForPerson(
        int lCommand, 
        ref ChcNetSdk.NetDvrAlarmer pAlarmer, 
        IntPtr pAlarmInfo, 
        uint dwBufLen, 
        IntPtr pUser)
    {
        if (lCommand != PublicConst.CommAlarmPdc)
            return;
        if (pAlarmInfo == IntPtr.Zero) 
            return;
        if (dwBufLen <_recInfoSize)
            return;
        var recInfo = Marshal.PtrToStructure<ChcNetSdk.NetDvrPdcAlramInfo>(pAlarmInfo);
        if(recInfo.byMode != 0)
            return;
        _receiveQueue.Enqueue(recInfo);
        _semaphoreSlim.Release();
    }
    private async Task ReplyCapture(CameraBean cameraBean)
    {
        var camRemoteLinkImpl =_camRemoteLinkImpl;
        if (camRemoteLinkImpl.GetUserId()>=0)
        {
            //动作：拍照；
            var value = await camRemoteLinkImpl.CaptureJpegPicture(cameraBean,SystemInfo.PhotoDir); 
            if (value.Code.Equals(PublicConst.FlagYes))
            {
                cameraBean.Message = "拍照执行成功！";
                Dispatcher.UIThread.Post(() => { MessageText = $"{cameraBean.Message}【{cameraBean.DateTime}】";});
                var resultInfo = await ParaSetupModules.DbService!.AddAlarm(cameraBean);
                if (!resultInfo.Code.Equals(PublicConst.FlagYes))
                {
                    ParaSetupModules.RaiseAscDataProdEvent($"{resultInfo.Message}【{cameraBean.DateTime}】");
                }
            }
            else
            {
                cameraBean.Message = value.Message;
                ParaSetupModules.RaiseAscDataProdEvent($"{value.Message}【{cameraBean.DateTime}】");
                var resultInfo = await ParaSetupModules.DbService!.AddError(cameraBean);
                if (!resultInfo.Code.Equals(PublicConst.FlagYes))
                {
                    ParaSetupModules.RaiseAscDataProdEvent($"{resultInfo.Message}【{cameraBean.DateTime}】");
                }
            }
        }
        else
        {
            cameraBean.Message = "触发拍照，但未连接摄像头！";
            Dispatcher.UIThread.Post(() => { MessageText = $"{cameraBean.Message}【{cameraBean.DateTime}】";});
            var resultInfo = await ParaSetupModules.DbService!.AddError(cameraBean);
            if (!resultInfo.Code.Equals(PublicConst.FlagYes))
            {
                ParaSetupModules.RaiseAscDataProdEvent(resultInfo.Message);
            }
        }
    }
    private async Task ReplyCamera(CameraBean cameraBean)
    {
        var camRemoteLinkImpl =_camRemoteLinkImpl;
        if (camRemoteLinkImpl.GetUserId()>=0)
        {
            //动作：录像；
            var value = await camRemoteLinkImpl.PlayCam(cameraBean,SystemInfo.PhotoDir); 
            if (value.Code.Equals(PublicConst.FlagYes))
            {
                cameraBean.Message = "录像执行成功！";
                Dispatcher.UIThread.Post(() => { MessageText = $"{cameraBean.Message}【{cameraBean.DateTime}】";});
                var resultInfo = await ParaSetupModules.DbService!.AddAlarm(cameraBean);
                if (!resultInfo.Code.Equals(PublicConst.FlagYes))
                {
                    ParaSetupModules.RaiseAscDataProdEvent($"{resultInfo.Message}【{cameraBean.DateTime}】");
                }
            }
            else
            {
                cameraBean.Message = value.Message;
                ParaSetupModules.RaiseAscDataProdEvent($"{value.Message}【{cameraBean.DateTime}】");
                var resultInfo = await ParaSetupModules.DbService!.AddError(cameraBean);
                if (!resultInfo.Code.Equals(PublicConst.FlagYes))
                {
                    ParaSetupModules.RaiseAscDataProdEvent($"{resultInfo.Message}【{cameraBean.DateTime}】");
                }
            }
        }
        else
        {
            cameraBean.Message = "触发录像，但未连接摄像头！";
            Dispatcher.UIThread.Post(() => { MessageText = $"{cameraBean.Message}【{cameraBean.DateTime}】";});
            var resultInfo = await ParaSetupModules.DbService!.AddError(cameraBean);
            if (!resultInfo.Code.Equals(PublicConst.FlagYes))
            {
                ParaSetupModules.RaiseAscDataProdEvent(resultInfo.Message);
            }
        }
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
            await Task.Delay(PublicConst.HeartCame, token);
            await _sem.WaitAsync(token);
            try
            {
                if (_manClose)
                    continue;
                if (_start)
                {
                    //已经远程登录，校验是否在线？
                    var onLine = await CheckIsLine();
                    if (!onLine)
                    {
                        //不在线，尝试重连；
                        await CloseAsync();
                        await OpenAsync();
                    }
                    continue;
                }
                if (CheckIsValid())
                {
                    await OpenAsync();
                }
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
                _sem.Release(); // 释放信号量锁
            }
        }
    }
    private async Task<bool> CheckIsLine()
    {
        return await _camRemoteLinkImpl.CheckOnLine();
    }
    private bool CheckIsValid()
    {
        if (_check)
            return _isValid;
        _check = true;
        var camInfo = ParaSetupModules.CamInfo!;
        _isValid = camInfo.IsValid() && !HikPlatform.IsMac;
        return _isValid;
    }
    private async Task OpenAsync()
    {
        await OnOpen();
    }
    private async Task OnOpen()
    {
        var camInfo = ParaSetupModules.CamInfo!;
        if (!camInfo.IsValid())
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MessageText = "摄像头参数配置不正确，如果已经配置过，请重新启动程序加载！";
            });
            return;
        }
        if (HikPlatform.IsMac)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MessageText = "MAC环境，不支持此操作，请切换到：Windows/Linux环境测试！";
            });
            return;        
        }
        //初始化；
        var loadCamResult00 = await CamRemoteManager.SdkInitialize();
        if (loadCamResult00<0)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MessageText = "摄像头初始化失败！";
            });
            return;
        }
        //尝试登录;
        var loadCamResult = await _camRemoteLinkImpl.Login(camInfo);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MessageText = loadCamResult.Message;
            });
            return;
        }
        //布防；
        loadCamResult = await _camRemoteLinkImpl.SetupAlarm(_callBackForPerson);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MessageText = loadCamResult.Message;
            });
            return;
        }
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _start = true;
            CamState = "【 摄像头连接状态：✔ 】";
            OpenCommand.NotifyCanExecuteChanged();
            CloseCommand.NotifyCanExecuteChanged();
        });
    }
    private async Task CloseAsync()
    {
        await OnClose();
    }
    private async Task OnClose()
    {
        var loadCamResult = _camRemoteLinkImpl.Logout();
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
            {
                MessageText = "退出登录失败！";
            }
            CamState = "【 摄像头连接状态：✘ 】";
            OpenCommand.NotifyCanExecuteChanged();
            CloseCommand.NotifyCanExecuteChanged();
        });
        _start = false;
    }
    partial void OnConfigChanged(CamOptions? value)
    {
        if (value is null)
            return;
        Ip = value.Ip;
        Port = value.Port;
        UserName = value.UserName;
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
    private async Task PersonLoop(CancellationToken token)
    {
        try
        {
            await PersonLoopAsync(token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ParaSetupModules.RaiseAscDataProdEvent($"人数实时监听顶层异常：{ex.Message}【{currDateTime}】");
        }
    }
    private async Task PersonLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await _semaphoreSlim.WaitAsync(token);
            if (!_receiveQueue.TryDequeue(out var data))
            {
                var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                ParaSetupModules.RaiseAscDataProdEvent($"获取布防消息失败！【{currDateTime}】");
                continue;
            }
            ParaSetupModules.PersonInfo!.Enter = data.dwEnterNum;
            ParaSetupModules.PersonInfo.Leave = data.dwLeaveNum;
            ParaSetupModules.PersonInfo.Pass = data.dwPassingNum;
            //var dwUnionSize = (uint)Marshal.SizeOf(data.uStatModeParam);
            //var ptrPdcUnion = Marshal.AllocHGlobal((Int32)dwUnionSize);
            //Marshal.StructureToPtr(data.uStatModeParam, ptrPdcUnion, false);
            //var recStatFrame = Marshal.PtrToStructure<ChcNetSdk.UnionStatFrame>(ptrPdcUnion);
            //_personInfo.RelativeTime = recStatFrame.dwRelativeTime;
            //_personInfo.AbsTime = recStatFrame.dwAbsTime;
        }
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
    private async Task ClearResource()
    {
        await _sem.WaitAsync(_ctsHeart!.Token);
        try
        {
            if (_start)
            {
                _camRemoteLinkImpl.Logout();
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
            try
            {
                await _personTask!;
            }
            catch
            {
                //忽略；
            }
            _ctsHeart = null;
            _heartTask = null;
            _personTask = null;
            CamRemoteManager.SdkCleanUp();
            _start = false;
        }
        finally
        {
            _sem.Release();
        }
    }
    public void Receive(CamMessage message)
    {
        switch (message.Type)
        {
            case PublicConst.CamPhoto:
                _ = ReplyCapture(message.CameraBean);
                break;
            case PublicConst.CamCamera:
                _ = ReplyCamera(message.CameraBean);
                break;   
        }
    }
    public void Receive(AppCleanupMessage message)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        Console.WriteLine("释放摄像头资源(CamConfigViewModel)----Receive！");
        _= ClearResource();
    }
}