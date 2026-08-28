using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HighMetroServer.BaseModel;
using HighMetroServer.HikVision;
using HighMetroServer.Message;
using HighMetroServer.Models;
using HighMetroServer.Services;

namespace HighMetroServer.ViewModels;

public partial class CamConfigViewModel : ObservableObject,IRecipient<AppCleanupMessage>
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
    public CamConfigViewModel()
    {
        _start = false;
        CamState = "【 摄像头连接状态：❌ 】";
        _camRemoteLinkImpl = new CamRemoteLinkImpl();
        ParaSetupModules.CamInfo!.CamRemoteLinkImpl = _camRemoteLinkImpl;
        WeakReferenceMessenger.Default.Register<AppCleanupMessage>(this);
    }
    partial void OnConfigChanged(CamOptions? value)
    {
        if (value is null)
            return;
        Ip = value.Ip;
        Port = value.Port;
        UserName = value.UserName;
    }
    public async Task Start()
    {
        if (PublicConst.SelfStart == 1)
        {
            if (!_start)
            {
                await Task.Delay(1000); 
                Open();
            }
        }
    }
    [RelayCommand(CanExecute = nameof(CanOpen))]
    private void Open()
    {
        var camInfo = ParaSetupModules.CamInfo!;
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
        //初始化；
        var loadCamResult00 = _camRemoteLinkImpl.Init();
        if (!loadCamResult00.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = loadCamResult00.Message;
            return;
        }
        //尝试登录;
        var loadCamResult = _camRemoteLinkImpl.Login(camInfo);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = loadCamResult.Message;
            return;
        }
        _start = true;
        CamState = "【 摄像头连接状态：✅ 】";
        OpenCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();        
    }
    [RelayCommand(CanExecute= nameof(CanClose))]
    private void Close()
    {
        //退出登录；
        var loadCamResult = _camRemoteLinkImpl.Logout();
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
        if (_start)
        {
            _camRemoteLinkImpl.Logout();
        }
        //释放摄像机资源；
        _camRemoteLinkImpl.Clear();
        _start = false;
    }
    public void Receive(AppCleanupMessage message)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        ClearResource();
        Console.WriteLine("释放摄像头资源----Receive！");
    }
}