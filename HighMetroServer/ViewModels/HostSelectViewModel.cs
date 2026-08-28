using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetroServer.BaseModel;
using HighMetroServer.Models;
using HighMetroServer.Parameters;
using HighMetroServer.Services;

namespace HighMetroServer.ViewModels;

public partial class HostSelectViewModel : ViewModelBase
{
    private readonly IConfigService _configService;

    // 回调
    public event Action<HostSetting>? OnConfirm;
    public event Action? OnCancel;

    [ObservableProperty]
    private ObservableCollection<HostModals> _hostModalsList = [];
    
    private HostModals? _selectedHost;

    [ObservableProperty]
    private string _messageText = "";
    public HostSelectViewModel(IConfigService configService, ResultHostInfo resultHostInfo)
    {
        _configService = configService;
        if (resultHostInfo.ReturnInfo.Code.Equals(PublicConst.FlagYes))
        {
            List<HostInfo> hostList = resultHostInfo.HostList;
            if (hostList.Count > 0)
            {
                int length = hostList.Count;
                HostModalsList = new ObservableCollection<HostModals>();
                for (var i = 0; i < length; i++)
                {
                    HostInfo hostInfo = hostList[i];
                    HostModals hostModals = new HostModals(hostInfo.Bh,"("+hostInfo.Code+")"+hostInfo.Name);
                    HostModalsList.Add(hostModals);
                }
            }
        }
        else
        {
            MessageText = resultHostInfo.ReturnInfo.Message;
        }
    }
    public HostModals? SelectedHost
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
    private void OnHostSelectedChanged(HostModals? selectItem)
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
            Bh = SelectedHost!.Bh
        };
    }
}