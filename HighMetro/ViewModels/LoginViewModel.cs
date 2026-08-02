using System;
using System.ComponentModel.DataAnnotations;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetro.Parameters;
using HighMetro.Services;

namespace HighMetro.ViewModels;

public partial class LoginViewModel : ObservableValidator
{
    private readonly IDbService _dbService;
    private readonly IConfigService _configService;

    public Action<LoginSetting>? OnLoginSuccess;
    public Action? OnLoginCancel;

    [ObservableProperty]
    [Required(ErrorMessage = "用户名不能为空")]
    private string _username = "";

    [ObservableProperty]
    [Required(ErrorMessage = "密码不能为空")]
    private string _password = "";

    [ObservableProperty]
    private string _msg = "";

    public LoginViewModel(IConfigService configService, IDbService dbService)
    {
        _dbService = dbService;
        _configService = configService;
        var loginConfig = _configService.LoadLoginConfig() ?? new LoginSetting();
        Username = loginConfig.LoginUser;
        Password = loginConfig.LoginPassword;
    }
    [RelayCommand]
    private void Login()
    {
        // 清除旧的错误并验证所有属性
        ValidateAllProperties();
        if (HasErrors)
        {
            return; 
        }
        if (_dbService.VerifyUser(Username, Password))
        {
            var setting = BuildSetting();
            _configService.SaveLoginConfig(setting);
            Msg = "";
            OnLoginSuccess?.Invoke(setting);
        }
        else
        {
            Msg = "用户名或密码错误";
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
}