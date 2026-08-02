using System;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetro.Attributes;
using HighMetro.Models;
using HighMetro.Services;

namespace HighMetro.ViewModels;

public partial class DbConfigViewModel : ObservableValidator
{
    private readonly IDbService _dbService;
    private readonly IConfigService _configService;

    public Action<DbSetting>? OnDbConfigSuccess;
    public Action? OnDbConfigCancel;
    
    [ObservableProperty]
    [Required(ErrorMessage = "地址不能为空")]
    [IpAddress(ErrorMessage = "IP 地址格式不正确")]
    private string _host;

    [ObservableProperty]
    [Required(ErrorMessage = "端口不能为空")]
    [Range(1001, 65535, ErrorMessage = "端口必须在 1001-65535 之间")]
    private int _port;
    
    [ObservableProperty]
    [Required(ErrorMessage = "用户名不能为空")]
    private string _dbUser;

    [ObservableProperty]
    [Required(ErrorMessage = "密码不能为空")]
    private string _dbPassword;

    [ObservableProperty]
    private string _messageText = "";
    public DbConfigViewModel(IConfigService configService, IDbService dbService)
    {
        _dbService = dbService;
        _configService = configService;
        var dbConfig = _configService.LoadDbConfig() ?? new DbSetting();
        Host = dbConfig.DbHost;
        Port = dbConfig.DbPort;
        DbUser = dbConfig.DbUser;
        DbPassword = dbConfig.DbPassword;
    }

    [RelayCommand]
    private void TestConnection()
    {
        var setting = BuildSetting();
        var ok = _dbService.TestConnection(setting);
        MessageText = ok ? "✅ 数据库连接成功！" : "❌ 连接失败，请检查参数";
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
        //bool connected = _dbService.TestConnection(setting);
        //if (!connected)
        //{
            //MessageText = "❌ 连接失败，请检查参数！";
            //return; 
        //}
        _configService.SaveDbConfig(setting);
        OnDbConfigSuccess?.Invoke(setting);
    }

    [RelayCommand]
    private void Cancel(MainViewModel rootVm)
    {
        OnDbConfigCancel?.Invoke();
    }

    private DbSetting BuildSetting()
    {
        return new DbSetting
        {
            DbHost = Host,
            DbPort = Port,
            DbUser = DbUser,
            DbPassword = DbPassword
        };
    }
}