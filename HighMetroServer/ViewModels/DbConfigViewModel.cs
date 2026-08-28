using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetroServer.BaseModel;
using HighMetroServer.Models;
using HighMetroServer.Parameters;
using HighMetroServer.Services;

namespace HighMetroServer.ViewModels;

public partial class DbConfigViewModel : ViewModelBase
{
    private readonly IDbService _dbService;
    private readonly IConfigService _configService;

    public event Action<DbSetting>? OnDbConfigSuccess;
    public event Action? OnDbConfigCancel;
    
    [ObservableProperty]
    private string _host;

    [ObservableProperty]
    private string _portText;
    
    [ObservableProperty]
    private int _port;
    
    [ObservableProperty]
    private string _dbUser;

    [ObservableProperty]
    private string _dbPassword;

    [ObservableProperty]
    private string _messageText = "";
    public DbConfigViewModel(IConfigService configService, IDbService dbService,DbSetting setting,ResultInfo resultInfo)
    {
        _dbService = dbService;
        _configService = configService;
        Host = setting.DbHost;
        PortText = setting.DbPort.ToString();
        Port = setting.DbPort;
        DbUser = setting.DbUser;
        DbPassword = setting.DbPassword;
        if (resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = "❌ 连接失败，请检查参数:"+resultInfo.Message;
        }
    }
    [RelayCommand]
    private void TestConnection()
    {
        if (!ValidateProperty())
        {
            return; 
        }
        var setting = BuildSetting();
        ResultInfo resultInfo = _dbService.TestConnection(setting);
        MessageText = resultInfo.Code.Equals(PublicConst.FlagYes) ? "✅ 数据库连接成功！" : "❌ 连接失败，请检查参数:"+resultInfo.Message;
    }
    [RelayCommand]
    private void Confirm()
    {
        if (!ValidateProperty())
        {
            return; 
        }
        var setting = BuildSetting();
        // 新增：测试数据库连接
        ResultInfo resultInfo = _dbService.TestConnection(setting);
        if (!resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = "❌ 连接失败，请检查参数:"+resultInfo.Message;
            return;    
        }
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
    private bool ValidateProperty()
    {
        if (string.IsNullOrWhiteSpace(Host))
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
        if (string.IsNullOrWhiteSpace(DbUser))
        {
            MessageText = "用户名不能为空！";
            return false;
        }
        if (string.IsNullOrWhiteSpace(DbPassword))
        {
            MessageText = "密码不能为空！";
            return false;
        }
        return true;
    }
}