using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HighMetro.BaseModel;
using HighMetro.HikVision;
using HighMetro.Message;
using HighMetro.Models;
using HighMetro.Services;

namespace HighMetro.ViewModels;

public partial class CamConfigViewModel : ObservableObject,IRecipient<AppCleanupMessage>
{
    // 1. 接收外部传入的配置数据
    [ObservableProperty]
    private CamOptions? _config;
    
    // 2. 接收外部传入的只读状态
    [ObservableProperty] 
    private bool _isReadOnly;

    [ObservableProperty] 
    private string _camState;
    // UI绑定字段
    [ObservableProperty]
    private string _ip = string.Empty;

    [ObservableProperty]
    private int _port = 3000;
    
    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private bool _isInfoHit;
    
    [ObservableProperty]
    private string _messageText = string.Empty;

    private bool _start;
    public CamConfigViewModel(bool isReadOnly)
    {
        _isInfoHit = true;
        IsReadOnly = isReadOnly;
        _start = false;
        CamState = "【 摄像头连接状态：❌ 】";
    }
    partial void OnConfigChanged(CamOptions? value)
    {
        if (value is null)
            return;
        // 将外部HostOptions模型映射拷贝到UI属性
        Ip = value.Ip;
        Port = value.Port;
        UserName = value.UserName;
    }
    [RelayCommand(CanExecute = nameof(CanOpen))]
    private void Open()
    {
        var camInfo = ParaSetupModules.CamInfo;
        if (camInfo == null)
        {
            MessageText = "摄像头参数未配置，如果已经配置过，请重新启动程序加载！";
            return;
        }
        if (!camInfo.IsValid())
        {
            MessageText = "摄像头参数配置不正确，如果已经配置过，请重新启动程序加载！";
            return;
        }
        if (HikPlatform.IsMac)
        {
            MessageText = "MAC环境，不支持此操作，请切换到：Windows/Linux环境测试！";
            return;        
        }
        if (!InitDrive.InitSign)
        {
            //初始化；
            var loadCamResult00 = CamRemoteLinkImpl.Init();
            if (!loadCamResult00.Code.Equals(PublicConst.FlagYes))
            {
                MessageText = "摄像头初始化失败！";
                return;
            }

            InitDrive.InitSign = true;
        }
        //尝试登录;
        var loadCamResult = CamRemoteLinkImpl.Login(camInfo);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = loadCamResult.Message;
            return;
        }
        camInfo.UserId = loadCamResult.Value;
        _start = true;
        CamState = "【 摄像头连接状态：✅ 】";
        OpenCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();        
    }
    [RelayCommand(CanExecute= nameof(CanClose))]
    private void Close()
    {
        //退出登录；
        var camInfo = ParaSetupModules.CamInfo;
        var loadCamResult = CamRemoteLinkImpl.Logout(camInfo!.UserId);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = "退出登录失败！";
        }
        _start = false;
        CamState = "【 摄像头连接状态：❌ 】";
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
    private void ClearResource()
    {
        Console.WriteLine("释放摄像机资源！");
        if (!_start)
        {
            return;
        }
        var camInfo = ParaSetupModules.CamInfo;
        CamRemoteLinkImpl.Logout(camInfo!.UserId);
        //释放摄像机资源；
        CamRemoteLinkImpl.Clear();
    }
    public void Receive(AppCleanupMessage message)
    {
        ClearResource();
    }
    public void Unsubscribe()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}