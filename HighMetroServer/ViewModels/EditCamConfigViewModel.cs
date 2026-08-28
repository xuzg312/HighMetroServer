using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetroServer.BaseModel;
using HighMetroServer.HikVision;
using HighMetroServer.Models;
using HighMetroServer.Services;

namespace HighMetroServer.ViewModels;

public partial class EditCamConfigViewModel : ViewModelBase
{
    private readonly IDbService _dbService;
    public event Action? OnHardConfigSuccess;
    public event Action? OnHardConfigCancel;
    
    [ObservableProperty]
    private string _ip;

    [ObservableProperty]
    private string _portText;
    
    [ObservableProperty]
    private int _port;
    
    [ObservableProperty]
    private string _userName;

    [ObservableProperty]
    private string _password;

    [ObservableProperty]
    private string _messageText = "";

    private readonly HardInfo _hardInfo;
    private readonly CamRemoteLinkImpl _camRemoteLinkImpl;

    public EditCamConfigViewModel(IDbService dbService,HardInfo hardInfo,ResultInfo resultInfo)
    {
        _dbService = dbService;
        if (!resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = resultInfo.Message;
        }
        Ip = hardInfo.Ip;
        Port = hardInfo.Port;
        PortText= hardInfo.Port.ToString();
        UserName = hardInfo.UserName;
        Password = hardInfo.PassWord;
        _hardInfo = hardInfo;
        _camRemoteLinkImpl = new CamRemoteLinkImpl();
    }
    [RelayCommand]
    private void TestConnection()
    {
        if (!ValidateProperty())
        {
            return; 
        }
        if (HikPlatform.IsMac)
        {
            MessageText = "MAC环境，不支持此操作，请切换到：Windows/Linux环境测试！";
            return;        
        }
        //尝试连接摄像机；
        //初始化；
        var loadCamResult00 = _camRemoteLinkImpl.Init();
        if (!loadCamResult00.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = "摄像头初始化失败！";
            return;
        }
        var setting = BuildSetting();
        //尝试登录;
        var loadCamResult = _camRemoteLinkImpl.Login(setting);
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = loadCamResult.Message;
            return;
        }
        //连接成功，退出登录；
        loadCamResult = _camRemoteLinkImpl.Logout();
        if (!loadCamResult.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = "断开摄像头失败！";
            return;
        }
        MessageText = "连接摄像头正常 ✅ ！";
    }
    [RelayCommand]
    private void Confirm()
    {
        MessageText = "";
        if (!ValidateProperty())
        {
            return; 
        }
        //保存到数据库；
        _hardInfo.Ip = Ip;
        _hardInfo.Port = Port;
        _hardInfo.UserName = UserName;
        _hardInfo.PassWord = Password;
        var resultInfo = _hardInfo.Bh == 0 
            ? _dbService.AddHardCamera(_hardInfo) 
            : _dbService.EditHardCamera(_hardInfo);
        if (!resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = resultInfo.Message;
            return;
        }

        _camRemoteLinkImpl.Close();
        OnHardConfigSuccess?.Invoke();
    }
    [RelayCommand]
    private void Cancel()
    {
        _camRemoteLinkImpl.Clear();
        OnHardConfigCancel?.Invoke();
    }
    private HardInfo BuildSetting()
    {
        return new HardInfo
        {
            Ip = Ip,
            Port = Port,
            UserName = UserName,
            PassWord = Password
        };
    }
    private bool ValidateProperty()
    {
        if (string.IsNullOrWhiteSpace(Ip))
        {
            MessageText = "地址不能为空！";
            return false;
        }
        if (string.IsNullOrWhiteSpace(PortText))
        {
            MessageText = "端口无效！";
            return false;
        }
        if (!int.TryParse(PortText, out int portNumber))
        {
            MessageText = "端口号格式不正确";
            return false;
        }
        if (portNumber < 1000 || portNumber > 65535)
        {
            MessageText = $"端口 {portNumber} 【1000-65535】";
            return false;
        }
        Port= portNumber;
        if (string.IsNullOrWhiteSpace(UserName))
        {
            MessageText = "用户名不能为空！";
            return false;
        }
        if (string.IsNullOrWhiteSpace(Password))
        {
            MessageText = "密码不能为空！";
            return false;
        }
        return true;
    }
}