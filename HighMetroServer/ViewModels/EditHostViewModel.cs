using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetroServer.BaseModel;
using HighMetroServer.Models;
using HighMetroServer.Parameters;
using HighMetroServer.Services;

namespace HighMetroServer.ViewModels;

public partial class EditHostViewModel : ViewModelBase
{
    private readonly IConfigService _configService;
    private readonly IDbService _dbService;
    public event Action? OnSuccess;
    public event Action? OnCancel;

    [ObservableProperty] 
    private string _ip = string.Empty;

    [ObservableProperty]
    private string _portText = string.Empty;
    
    [ObservableProperty]
    private int _port;
    
    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _messageText = "";

    private readonly HostInfo _hostInfo;
    private readonly DbSetting _dbSetting;
    
    public EditHostViewModel(
        IConfigService configService, 
        IDbService dbService,
        DbSetting dbSetting,
        HostInfo hostInfo, 
        ResultInfo resultInfo)
    {
        _dbService = dbService;
        _configService = configService;
        _dbSetting = dbSetting;
        _hostInfo = hostInfo;
        if (!resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = resultInfo.Message;
        }
        Ip = hostInfo.Ip;
        Port = hostInfo.Port;
        PortText= hostInfo.Port.ToString();
        Code = hostInfo.Code;
        Name = hostInfo.Name;
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
        _hostInfo.Ip = Ip;
        _hostInfo.Port = Port;
        _hostInfo.Code = Code;
        _hostInfo.Name = Name;
        var resultInfo = _hostInfo.Bh == -1 
            ? _dbService.AddHost(_hostInfo,_dbSetting) 
            : _dbService.EditHost(_hostInfo,_dbSetting);
        if (!resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = resultInfo.Message;
            return;
        }
        var setting = new HostSetting
        {
            Bh = _hostInfo.Bh
        };
        _configService.SaveHostConfig(setting);
        OnSuccess?.Invoke();
    }
    [RelayCommand]
    private void Cancel()
    {
        OnCancel?.Invoke();
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
        if (string.IsNullOrWhiteSpace(Code))
        {
            MessageText = "代码不能为空！";
            return false;
        }
        if (string.IsNullOrWhiteSpace(Name))
        {
            MessageText = "名称不能为空！";
            return false;
        }
        return true;
    }
}