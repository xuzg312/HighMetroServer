using System;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetro.Attributes;
using HighMetro.BaseModel;
using HighMetro.Models;
using HighMetro.Parameters;
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
    private string _Password;

    [ObservableProperty]
    private string _messageText = "";
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
    }

    [RelayCommand]
    private void TestConnection()
    {
        var setting = BuildSetting();
        //ResultInfo resultInfo = _dbService.TestConnection(setting);
        //MessageText = resultInfo.Code.Equals(PublicConst.FlagYes) ? "✅ 数据库连接成功！" : "❌ 连接失败，请检查参数:"+resultInfo.Message;
    }
    [RelayCommand]
    private void Confirm()
    {
        // 清除旧的错误并验证所有属性
        ValidateAllProperties();
        if (HasErrors)
        {
            return; 
        }
        var setting = BuildSetting();
        // 新增：测试数据库连接
        /*ResultInfo resultInfo = _dbService.TestConnection(setting);
        if (!resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = "❌ 连接失败，请检查参数:"+resultInfo.Message;
            return;    
        }*/
        OnHardConfigSuccess?.Invoke();
    }

    [RelayCommand]
    private void Cancel(MainViewModel rootVm)
    {
        OnHardConfigCancel?.Invoke();
    }

    private HardSetting BuildSetting()
    {
        return new HardSetting
        {
            Ip = Ip,
            Port = Port,
            UserName = UserName,
            Password = Password
        };
    }
}