using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetro.BaseModel;
using HighMetro.Models;
using HighMetro.Parameters;
using HighMetro.Services;

namespace HighMetro.ViewModels;

public partial class EditSerialConfigViewModel : ObservableValidator
{
    private readonly IDbService _dbService;
    public event Action? OnExit;
    
    [ObservableProperty] 
    private string _messageText1 = string.Empty;
    
    [ObservableProperty] 
    private ObservableCollection<CodeNameModals> _portNameList1 = CommParameter.PortNameList;

    [ObservableProperty] 
    private ObservableCollection<CodeNameModals> _baudRateList1 = CommParameter.BaudRateList;

    [ObservableProperty]
    private ObservableCollection<CodeNameModals> _dataBitsList1 = CommParameter.DataBitsList;

    [ObservableProperty] 
    private ObservableCollection<CodeNameModals> _parityList1 = CommParameter.ParityList;

    [ObservableProperty] 
    private ObservableCollection<CodeNameModals> _stopBitsList1 = CommParameter.StopBitsList;
    
    [ObservableProperty] 
    private CodeNameModals? _selectPortName1;
    
    [ObservableProperty] 
    private CodeNameModals? _selectedBaudRate1;
    
    [ObservableProperty] 
    private CodeNameModals? _selectedDataBits1;
    
    [ObservableProperty] 
    private CodeNameModals? _selectedStopBits1;
    
    [ObservableProperty] 
    private CodeNameModals? _selectedParity1;
    
    [ObservableProperty] 
    private string _messageText2 = string.Empty;
    
    [ObservableProperty] 
    private ObservableCollection<CodeNameModals> _portNameList2 = CommParameter.PortNameList;

    [ObservableProperty] 
    private ObservableCollection<CodeNameModals> _baudRateList2 = CommParameter.BaudRateList;

    [ObservableProperty]
    private ObservableCollection<CodeNameModals> _dataBitsList2 = CommParameter.DataBitsList;

    [ObservableProperty] 
    private ObservableCollection<CodeNameModals> _parityList2 = CommParameter.ParityList;

    [ObservableProperty] 
    private ObservableCollection<CodeNameModals> _stopBitsList2 = CommParameter.StopBitsList;
    
    [ObservableProperty] 
    private CodeNameModals? _selectPortName2;
    
    [ObservableProperty] 
    private CodeNameModals? _selectedBaudRate2;
    
    [ObservableProperty] 
    private CodeNameModals? _selectedDataBits2;
    
    [ObservableProperty] 
    private CodeNameModals? _selectedStopBits2;
    
    [ObservableProperty] 
    private CodeNameModals? _selectedParity2;

    public EditSerialConfigViewModel(IDbService dbService, ResultSerialCommInfo resultSerialCommInfo)
    {
        _dbService = dbService;
        var serialCommList = resultSerialCommInfo.SerialCommList;
        if (!resultSerialCommInfo.ReturnInfo.Code.Equals(PublicConst.FlagYes))
        {
            MessageText1 = resultSerialCommInfo.ReturnInfo.Message;
            return;
        }
        if (serialCommList.Count == 0)
        {
            return;
        }
        var serialCommInfo = serialCommList[0];
        SelectPortName1 = PortNameList1.FirstOrDefault(item => item.DisplayName == serialCommInfo.CommName);
        SelectedBaudRate1 = BaudRateList1.FirstOrDefault(item => item.Value == serialCommInfo.BaudRate);
        SelectedDataBits1 = DataBitsList1.FirstOrDefault(item => item.Value == serialCommInfo.DataBits);
        SelectedStopBits1 = StopBitsList1.FirstOrDefault(item => item.Value == serialCommInfo.StopBits);
        SelectedParity1 = ParityList1.FirstOrDefault(item => item.Value == serialCommInfo.Parity);
        if (serialCommList.Count == 1)
        {
            return;
        }
        serialCommInfo = serialCommList[1];
        SelectPortName2 = PortNameList2.FirstOrDefault(item => item.DisplayName == serialCommInfo.CommName);
        SelectedBaudRate2 = BaudRateList2.FirstOrDefault(item => item.Value == serialCommInfo.BaudRate);
        SelectedDataBits2 = DataBitsList2.FirstOrDefault(item => item.Value == serialCommInfo.DataBits);
        SelectedStopBits2 = StopBitsList2.FirstOrDefault(item => item.Value == serialCommInfo.StopBits);
        SelectedParity2 = ParityList2.FirstOrDefault(item => item.Value == serialCommInfo.Parity);
    }
    [RelayCommand]
    private void Exit()
    {
        OnExit?.Invoke();
    }
}