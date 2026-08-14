using System;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetro.Attributes;
using HighMetro.BaseModel;
using HighMetro.HikVision;
using HighMetro.Models;
using HighMetro.Services;

namespace HighMetro.ViewModels;

public partial class EditCamConfigViewModel : ObservableValidator
{
    private readonly IDbService _dbService;
    public event Action? OnHardConfigSuccess;
    public event Action? OnHardConfigCancel;
    
    [ObservableProperty]
    [Required(ErrorMessage = "地址不能为空")]
    [IpAddress(ErrorMessage = "IP 地址格式不正确")]
    private string _ip;

    [ObservableProperty]
    [Required(ErrorMessage = "端口不能为空")]
    [Range(1001, 65535, ErrorMessage = "端口必须在 1001-65535 之间")]
    private int _port;
    
    [ObservableProperty]
    [Required(ErrorMessage = "用户名不能为空")]
    private string _userName;

    [ObservableProperty]
    [Required(ErrorMessage = "密码不能为空")]
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
        UserName = hardInfo.UserName;
        Password = hardInfo.PassWord;
        _hardInfo = hardInfo;
        _camRemoteLinkImpl = new CamRemoteLinkImpl();
    }

    [RelayCommand]
    private void TestConnection()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            return; 
        }
        if (HikPlatform.IsMac)
        {
            MessageText = "MAC环境，不支持此操作，请切换到：Windows/Linux环境测试！";
            return;        
        }
        //尝试连接摄像机；
        if (!InitDrive.InitSign)
        {
            //初始化；
            var loadCamResult00 = _camRemoteLinkImpl.Init();
            if (!loadCamResult00.Code.Equals(PublicConst.FlagYes))
            {
                MessageText = "摄像头初始化失败！";
                return;
            }
            InitDrive.InitSign = true;
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
        loadCamResult = _camRemoteLinkImpl.Logout(loadCamResult.Value);
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
        ValidateAllProperties();
        if (HasErrors)
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
        OnHardConfigSuccess?.Invoke();
    }
    [RelayCommand]
    private void Cancel()
    {
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
}