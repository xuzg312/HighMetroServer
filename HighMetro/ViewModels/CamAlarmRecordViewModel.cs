using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HighMetro.BaseModel;
using HighMetro.Models;
using HighMetro.Services;

namespace HighMetro.ViewModels;

public partial class CamAlarmRecordViewModel : ObservableObject
{
    private readonly IDbService _dbService;

    public event Action? OnClose;
    
    // 选中行（预留浏览使用）
    [ObservableProperty]
    private CameraBean? _selectedRow;
    
    [ObservableProperty]
    private string _messageText=string.Empty;
    
    // 查询日期（默认当天）
    [ObservableProperty]
    private string _queryDate = DateTime.Now.Date.ToShortDateString();

    // 表格数据源
    [ObservableProperty]
    private ObservableCollection<CameraBean> _recordList=[];

    public CamAlarmRecordViewModel(IDbService dbService)
    {
        _dbService = dbService;
        MessageText = "请查询！";
    }
    [RelayCommand]
    private void Query()
    {
        var cameraBean = new CameraBean
        {
            DateTime=QueryDate,
            HostBh = ParaSetupModules.HostInfo!.Bh,
        };
        var resultCamAlarmInfo=_dbService.QueryCamAlarm(cameraBean);
        if (!resultCamAlarmInfo.ReturnInfo.Code.Equals(PublicConst.FlagYes))
        {
            MessageText = resultCamAlarmInfo.ReturnInfo.Message;
            return;
        }
        foreach (var item in resultCamAlarmInfo.CameraList)
            RecordList.Add(item);
        if (resultCamAlarmInfo.CameraList.Count == 0)
        {
            MessageText = "未查询到拍照记录！";
        }
    }
    [RelayCommand]
    private void Browse()
    {
        if (SelectedRow is not { } record)
        {
            MessageText = "请首先选择一条拍照记录！";
            return;
        }
        var path = record.FilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            MessageText = "选中的拍照记录对应的路径无效！";
            return;
        }
        // 触发事件，交给View层弹窗
        WeakReferenceMessenger.Default.Send(new PreviewImageMessage(path));
    }
    [RelayCommand]
    private void Exit()
    {
        OnClose?.Invoke();
    }
}