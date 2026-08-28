using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetroServer.BaseModel;
using HighMetroServer.Models;
using HighMetroServer.Parameters;
using HighMetroServer.Services;

namespace HighMetroServer.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IDbService _dbService;
    private readonly IConfigService _configService;
    private readonly DbSetting _dbSetting;
    
    public event Action<LoginSetting>? OnLoginSuccess;
    public event Action? OnLoginCancel;

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _msg = "";

    public LoginViewModel(IConfigService configService, IDbService dbService,LoginSetting loginSetting,DbSetting dbSetting)
    {
        _dbService = dbService;
        _configService = configService;
        Username = loginSetting.LoginUser;
        _dbSetting = dbSetting;
    }
    [RelayCommand]
    private void Login()
    {
        // 清除旧的错误并验证所有属性
        if (!ValidateProperty())
        {
            return; 
        }
        var setting = BuildSetting();
        ResultInfo resultInfo = _dbService.VerifyUser(setting,_dbSetting);
        if (resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            _configService.SaveLoginConfig(setting);
            Msg = "";
            UserInfo userInfo = new UserInfo()
            {
                Username = setting.LoginUser,
                Password=setting.LoginPassword
            };
            ParaSetupModules.UserInfo = userInfo;
            OnLoginSuccess?.Invoke(setting);
        }
        else
        {
            Msg = resultInfo.Message;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        OnLoginCancel?.Invoke();
    }
    private LoginSetting BuildSetting()
    {
        return new LoginSetting
        {
            LoginUser = Username,
            LoginPassword = Password
        };
    }
    private bool ValidateProperty()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            Msg = "用户名不能为空！";
            return false;
        }
        if (string.IsNullOrWhiteSpace(Password))
        {
            Msg = "密码不能为空！";
            return false;
        }
        return true;
    }
}