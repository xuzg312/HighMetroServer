using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HighMetroServer.BaseModel;
using HighMetroServer.Message;
using HighMetroServer.Models;
using HighMetroServer.Services;

namespace HighMetroServer.ViewModels;

public partial class CamAlarmRecordViewModel(IDbService dbService) : ObservableObject
{
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

    [ObservableProperty]
    private string _pageInfo = "第1页 / 共0页";
    
    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPage = 1;
    
    [RelayCommand]
    private void Query()
    {
        CurrentPage = 1;
        BeginQuery();
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
        //record.FilePath = "/Users/xu_zg/Desktop/JetBrainsRider/bak/9-11-52-26.jpg";
        //record.Type = PublicConst.DoorStateCapture;
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
    [RelayCommand]
    private void PrevPage()
    {
        if (CurrentPage <=1) return;
        CurrentPage--;
        BeginQuery();
    }
    [RelayCommand]
    private void NextPage()
    {
        if (CurrentPage >= TotalPage) return;
        CurrentPage++;
        BeginQuery();
    }
    private void BeginQuery()
    {
        RecordList.Clear();
        MessageText = string.Empty;
        var cameraBean = new CameraBean
        {
            DateTime=QueryDate,
            HostBh = ParaSetupModules.HostInfo!.Bh,
        };
        var resultInfo=dbService.QueryCamAlarmCount(cameraBean);
        if (!resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            MessageText=resultInfo.Message;
            return;
        }
        if (resultInfo.Tag > 0)
        {
            TotalPage = (int)Math.Ceiling(resultInfo.Tag * 1.0 / PublicConst.PageSize);
            var page = new DataBaseQueryPage(PublicConst.PageSize, CurrentPage);
            var resultCamAlarmInfo = dbService.QueryCamAlarm(cameraBean,page);
            if (!resultCamAlarmInfo.ReturnInfo.Code.Equals(PublicConst.FlagYes))
            {
                MessageText = resultCamAlarmInfo.ReturnInfo.Message;
                return;
            }
            foreach (var item in resultCamAlarmInfo.CameraList)
                RecordList.Add(item);
            if (resultCamAlarmInfo.CameraList.Count == 0)
            {
                MessageText = "未查询到报警记录！";
            }        
        }
        else
        {
            TotalPage = 1;
            MessageText = "未查询到报警记录！";
        }
        PageInfo = $"第{CurrentPage}页 / 共{TotalPage}页，共{resultInfo.Tag}条";
    }
}