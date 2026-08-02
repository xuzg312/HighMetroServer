using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetro.Models;
using HighMetro.Parameters;
using HighMetro.Services;

namespace HighMetro.ViewModels;

public partial class HostSelectViewModel : ObservableValidator
{
    private readonly IDbService _dbService;
    private readonly IConfigService _configService;

    // 回调
    public Action<HostSetting>? OnConfirm;
    public Action? OnCancel;

    [ObservableProperty]
    private ObservableCollection<HostInfo> _hostList = [];
    
    private HostInfo? _selectedHost;

    [ObservableProperty]
    private string _messageText = "";
    public HostSelectViewModel(IConfigService configService, IDbService dbService)
    {
        _dbService = dbService;
        _configService = configService;
        HostList = new ObservableCollection<HostInfo>
        {
            new HostInfo("IPC-01", "一号工业计算机"),
            new HostInfo("IPC-02", "二号工业计算机"),
            new HostInfo("IPC-03", "三号工业计算机"),
            new HostInfo("IPC-04", "四号工业计算机"),
            new HostInfo("IPC-05", "五号工业计算机")
        };
    }
    public HostInfo? SelectedHost
    {
        get => _selectedHost;
        set
        {
            if (SetProperty(ref _selectedHost, value))
            {
                // ======选中变更触发逻辑======
                OnHostSelectedChanged(value);
            }
        }
    } 
    private void OnHostSelectedChanged(HostInfo? selectItem)
    {
        if (selectItem == null)
        {
            MessageText = "❌ 请选择工控机！";
            return;
        }

        MessageText = "";
    }
    [RelayCommand]
    private void Confirm()
    {
        if (SelectedHost != null)
        {
            var setting = BuildSetting();
            _configService.SaveHostConfig(setting);
            OnConfirm?.Invoke(setting);
        }
        else
        {
            MessageText = "❌ 请选择工控机！";
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        OnCancel?.Invoke();
    }
    private HostSetting BuildSetting()
    {
        return new HostSetting
        {
            SystemHost = SelectedHost.Code
        };
    }
}