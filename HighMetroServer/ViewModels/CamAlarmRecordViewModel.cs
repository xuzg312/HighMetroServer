using System;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HighMetroServer.BaseModel;
using HighMetroServer.Models;
using HighMetroServer.Services;

namespace HighMetroServer.ViewModels;

public partial class CamAlarmRecordViewModel : ObservableObject
{
    private readonly IDbService _dbService;

    public event Action? OnClose;
    
    [ObservableProperty]
    private CameraBean? _selectedRow;
    
    [ObservableProperty]
    private string _messageText=string.Empty;
    
    [ObservableProperty]
    private DateTime _queryDate = DateTime.Now.Date;

    [ObservableProperty]
    private ObservableCollection<CameraBean> _recordList=[];

    [ObservableProperty]
    private bool _calendarPopupOpen;
    public CamAlarmRecordViewModel(IDbService dbService)
    {
        _dbService = dbService;
    }
    [RelayCommand]
    private void Query()
    {
        RecordList.Clear();
        MessageText = string.Empty;
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
    private void OpenCalendar()
    {
        CalendarPopupOpen = true;
    }
    [RelayCommand]
    private void PopupClosed()
    {
        CalendarPopupOpen = false;
    }
    [RelayCommand]
    private void Browse()
    {
        if (SelectedRow is not { } record)
        {
            MessageText = "请首先选择一条拍照记录！";
            return;
        }
        record.FilePath = "C:\\Users\\sunny\\Desktop\\share\\14-11-17-NO.mp4";
        record.Type = PublicConst.DoorStateCamera;
        var path = record.FilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            MessageText = "选中的记录对应的路径无效！";
            return;
        }
        if (record.Type.Equals(PublicConst.DoorStateCamera))
        {
            var dir = Path.GetDirectoryName(path)!;
            var nameWithoutExt = Path.GetFileNameWithoutExtension(path);
            var ext = Path.GetExtension(path);
            var newFileName = $"{nameWithoutExt}_0{ext}";
            path = Path.Combine(dir, newFileName);
        }
        if (!File.Exists(path))
        {
            MessageText = "选中的记录对应的文件不存在！";
            return;
        }
        // 触发事件，交给View层弹窗
        WeakReferenceMessenger.Default.Send(new PreviewImageMessage(path,record.Type));
    }
    [RelayCommand]
    private void Exit()
    {
        OnClose?.Invoke();
    }
}