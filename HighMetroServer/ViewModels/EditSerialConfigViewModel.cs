using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetroServer.BaseModel;
using HighMetroServer.Models;
using HighMetroServer.Parameters;
using HighMetroServer.Services;

namespace HighMetroServer.ViewModels;

public partial class EditSerialConfigViewModel : ViewModelBase
{
    private readonly IDbService _dbService;
    public event Action? OnExit;
    
    [ObservableProperty]
    public partial string MessageText1 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Id1 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name1 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<CodeNameModals> PortNameList1 { get; set; } = CommParameter.PortNameList;

    [ObservableProperty]
    public partial ObservableCollection<CodeNameModals> BaudRateList1 { get; set; } = CommParameter.BaudRateList;

    [ObservableProperty]
    public partial ObservableCollection<CodeNameModals> DataBitsList1 { get; set; } = CommParameter.DataBitsList;

    [ObservableProperty]
    public partial ObservableCollection<CodeNameModals> ParityList1 { get; set; } = CommParameter.ParityList;

    [ObservableProperty]
    public partial ObservableCollection<CodeNameModals> StopBitsList1 { get; set; } = CommParameter.StopBitsList;

    [ObservableProperty]
    public partial CodeNameModals? SelectPortName1 { get; set; }

    [ObservableProperty]
    public partial CodeNameModals? SelectedBaudRate1 { get; set; }

    [ObservableProperty]
    public partial CodeNameModals? SelectedDataBits1 { get; set; }

    [ObservableProperty]
    public partial CodeNameModals? SelectedStopBits1 { get; set; }

    [ObservableProperty]
    public partial CodeNameModals? SelectedParity1 { get; set; }

    [ObservableProperty]
    public partial string MessageText2 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Id2 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name2 { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<CodeNameModals> PortNameList2 { get; set; } = CommParameter.PortNameList;

    [ObservableProperty]
    public partial ObservableCollection<CodeNameModals> BaudRateList2 { get; set; } = CommParameter.BaudRateList;

    [ObservableProperty]
    public partial ObservableCollection<CodeNameModals> DataBitsList2 { get; set; } = CommParameter.DataBitsList;

    [ObservableProperty]
    public partial ObservableCollection<CodeNameModals> ParityList2 { get; set; } = CommParameter.ParityList;

    [ObservableProperty]
    public partial ObservableCollection<CodeNameModals> StopBitsList2 { get; set; } = CommParameter.StopBitsList;

    [ObservableProperty]
    public partial CodeNameModals? SelectPortName2 { get; set; }

    [ObservableProperty]
    public partial CodeNameModals? SelectedBaudRate2 { get; set; }

    [ObservableProperty]
    public partial CodeNameModals? SelectedDataBits2 { get; set; }

    [ObservableProperty]
    public partial CodeNameModals? SelectedStopBits2 { get; set; }

    [ObservableProperty]
    public partial CodeNameModals? SelectedParity2 { get; set; }

    private readonly List<SerialCommInfo> _serialCommList;

    public EditSerialConfigViewModel(IDbService dbService, ResultSerialCommInfo resultSerialCommInfo)
    {
        _dbService = dbService;
        _serialCommList = resultSerialCommInfo.SerialCommList;
        if (!resultSerialCommInfo.ReturnInfo.Code.Equals(PublicConst.FlagYes))
        {
            MessageText1 = resultSerialCommInfo.ReturnInfo.Message;
            return;
        }
        if (_serialCommList.Count == 0)
        {
            return;
        }
        var serialCommInfo = _serialCommList[0];
        Id1 = serialCommInfo.Id.ToString();
        Name1 = serialCommInfo.Name;
        SelectPortName1 = PortNameList1.FirstOrDefault(item => item.DisplayName == serialCommInfo.CommName);
        SelectedBaudRate1 = BaudRateList1.FirstOrDefault(item => item.Value == serialCommInfo.BaudRate);
        SelectedDataBits1 = DataBitsList1.FirstOrDefault(item => item.Value == serialCommInfo.DataBits);
        SelectedStopBits1 = StopBitsList1.FirstOrDefault(item => item.Value == serialCommInfo.StopBits);
        SelectedParity1 = ParityList1.FirstOrDefault(item => item.Value == serialCommInfo.Parity);
        if (_serialCommList.Count == 1)
        {
            return;
        }
        serialCommInfo = _serialCommList[1];
        Id2 = serialCommInfo.Id.ToString();
        Name2 = serialCommInfo.Name;
        SelectPortName2 = PortNameList2.FirstOrDefault(item => item.DisplayName == serialCommInfo.CommName);
        SelectedBaudRate2 = BaudRateList2.FirstOrDefault(item => item.Value == serialCommInfo.BaudRate);
        SelectedDataBits2 = DataBitsList2.FirstOrDefault(item => item.Value == serialCommInfo.DataBits);
        SelectedStopBits2 = StopBitsList2.FirstOrDefault(item => item.Value == serialCommInfo.StopBits);
        SelectedParity2 = ParityList2.FirstOrDefault(item => item.Value == serialCommInfo.Parity);
    }
    [RelayCommand]
    private void Open1()
    {
        if(!CheckComm1())
            return;
        var serialCommInfo = new SerialCommInfo
        {
            CommName=SelectPortName1!.DisplayName,
            BaudRate=SelectedBaudRate1!.Value,
            Parity=SelectedParity1!.Value,
            DataBits=SelectedDataBits1!.Value,
            StopBits=SelectedStopBits1!.Value
        };
        var commSerialImpl=new CommSerialImpl(0,serialCommInfo);
        var resultInfo = commSerialImpl.TestComm();
        if (resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            MessageText1 = "串口打开正常！";
            return;
        }
        MessageText1 = $"串口打开失败：{resultInfo.Message}";
    }
    [RelayCommand]
    private async Task Save1()
    {
        if(!CheckComm1())
            return;
 
        ResultInfo resultInfo;
        var serialComm = GetValue1();
        if (_serialCommList.Count == 0)
        {
            resultInfo = await _dbService.AddCommInfo(serialComm);
        }
        else
        {
            serialComm.Bh = _serialCommList[0].Bh;
            resultInfo = await _dbService.EditCommInfo(serialComm);
        }
        if (resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            Exit();
        }
        else
        {
            MessageText1 = resultInfo.Message;
        }
    }
    [RelayCommand]
    private void Open2()
    {
        if(!CheckComm2())
            return;
        var serialCommInfo = new SerialCommInfo
        {
            CommName=SelectPortName2!.DisplayName,
            BaudRate=SelectedBaudRate2!.Value,
            Parity=SelectedParity2!.Value,
            DataBits=SelectedDataBits2!.Value,
            StopBits=SelectedStopBits2!.Value
        };
        var commSerialImpl=new CommSerialImpl(0,serialCommInfo);
        var resultInfo = commSerialImpl.TestComm();
        if (resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            MessageText2 = "串口打开正常！";
            return;
        }
        MessageText2 = $"串口打开失败：{resultInfo.Message}";
    }
    [RelayCommand]
    private async Task Save2()
    {
        if(!CheckComm2())
            return;
        var serialComm = GetValue2();
        ResultInfo resultInfo;
        if (_serialCommList.Count <= 1)
        {
            resultInfo = await _dbService.AddCommInfo(serialComm);
        }
        else
        {
            serialComm.Bh = _serialCommList[1].Bh;
            resultInfo = await _dbService.EditCommInfo(serialComm);
        }
        if (resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            Exit();
        }
        else
        {
            MessageText2 = resultInfo.Message;
        }
    }
    [RelayCommand]
    private void Exit()
    {
        OnExit?.Invoke();
    }
    private bool CheckComm1()
    {
        if (!int.TryParse(Id1, out var intVal) || intVal is <= 0 or > 255)
        {
            MessageText1 = "ID号无效，有效范围：【1-255】！";
            return false;
        }
        if (SelectPortName1 is null || 
            SelectedBaudRate1 is null || 
            SelectedDataBits1 is null ||
            SelectedStopBits1 is null ||
            SelectedParity1 is null ||
            string.IsNullOrWhiteSpace(Name1))
        {
            MessageText1 = "请完善串口参数！";
            return false;
        }
        return true;
    }
    private bool CheckComm2()
    {
        if (!int.TryParse(Id2, out var intVal) || intVal is <= 0 or > 255)
        {
            MessageText2 = "ID号无效，有效范围：【1-255】！";
            return false;
        }
        if (SelectPortName2 is null || 
            SelectedBaudRate2 is null || 
            SelectedDataBits2 is null ||
            SelectedStopBits2 is null ||
            SelectedParity2 is null ||
            string.IsNullOrWhiteSpace(Name2))
        {
            MessageText2 = "请完善串口参数！";
            return false;
        }
        return true;
    }
    private SerialComm GetValue1()
    {
        var hostInfo = ParaSetupModules.HostInfo!;
        int.TryParse(Id1, out var intVal);
        return new SerialComm
        {
            HostBh = hostInfo.Bh,
            Id = intVal,
            Name = Name1,
            CommName = SelectPortName1!.DisplayName,
            BaudRate = SelectedBaudRate1!.Value,
            Parity = SelectedParity1!.Value,
            DataBits = SelectedDataBits1!.Value,
            StopBits=SelectedStopBits1!.Value,
            CommType = PublicConst.Mainboard
        };
    }
    private SerialComm GetValue2()
    {
        var hostInfo = ParaSetupModules.HostInfo!;
        int.TryParse(Id2, out var intVal);
        return new SerialComm
        {
            HostBh = hostInfo.Bh,
            Id = intVal,
            Name = Name2,
            CommName = SelectPortName2!.DisplayName,
            BaudRate = SelectedBaudRate2!.Value,
            Parity = SelectedParity2!.Value,
            DataBits = SelectedDataBits2!.Value,
            StopBits=SelectedStopBits2!.Value,
            CommType = PublicConst.Mainboard
        };
    }
}