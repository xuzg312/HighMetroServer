using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetro.Models;
using HighMetro.Parameters;
using HighMetro.Views.Controls;

namespace HighMetro.ViewModels;

public partial class SerialConfigViewModel : ObservableObject
{
    // 1. 接收外部传入的配置数据
    [ObservableProperty]
    private SerialPortOptions? _config;
    
    // 2. 接收外部传入的只读状态
    [ObservableProperty] 
    private bool _isReadOnly;

    // 3. 控件内部自己的数据（比如波特率下拉列表）
    [ObservableProperty] 
    private ObservableCollection<CodeNameModals> _portNameList= new ([]);

    [ObservableProperty] 
    private ObservableCollection<CodeNameModals> _baudRateList=new ([]);

    [ObservableProperty]
    private ObservableCollection<CodeNameModals> _dataBitsList=new ([]);

    [ObservableProperty] 
    private ObservableCollection<CodeNameModals> _parityList=new ([]);

    [ObservableProperty] 
    private ObservableCollection<CodeNameModals> _stopBitsList=new ([]);
    
    [ObservableProperty] 
    private CodeNameModals? _selectPortName;
    
    [ObservableProperty] 
    private CodeNameModals? _selectedBaudRate;
    
    [ObservableProperty] 
    private CodeNameModals? _selectedDataBits;
    
    [ObservableProperty] 
    private CodeNameModals? _selectedStopBits;
    
    [ObservableProperty] 
    private CodeNameModals? _selectedParity;

    [ObservableProperty] 
    private string? _messageText;

    private readonly bool _start;
    private int _serial;
    public SerialConfigViewModel(bool isReadOnly,int serial)
    {
        IsReadOnly = isReadOnly;
        _serial = serial;
        _start = false;
    }
    public void UpdateParams(bool isReadOnly, int serial, SerialPortOptions? options)
    {
        IsReadOnly = isReadOnly;
        _serial = serial;
        InitComboSource(options);
    }
    private CodeNameModals? BuildSingleItem(
        IEnumerable<CodeNameModals> sourcePool,
        Func<CodeNameModals, bool> matchRule,
        ObservableCollection<CodeNameModals> targetCollection)
    {
        var matched = sourcePool.FirstOrDefault(matchRule);
        if (matched == null)
            return null;

        var newItem = new CodeNameModals(matched.Value, matched.DisplayName);
        targetCollection.Add(newItem);
        return newItem;
    }
    private void InitComboSource(SerialPortOptions? value)
    {
        if (value is null) return;
        PortNameList.Clear();
        BaudRateList.Clear();
        DataBitsList.Clear();
        ParityList.Clear();
        StopBitsList.Clear();
        SelectPortName = BuildSingleItem(CommParameter.PortNameList, x => x.DisplayName == value.PortName, PortNameList);
        SelectedBaudRate = BuildSingleItem(CommParameter.BaudRateList, x => x.Value == value.BaudRate, BaudRateList);
        SelectedDataBits = BuildSingleItem(CommParameter.DataBitsList, x => x.Value == value.DataBits, DataBitsList);
        SelectedStopBits = BuildSingleItem(CommParameter.StopBitsList, x => x.Value == value.StopBits, StopBitsList);
        SelectedParity = BuildSingleItem(CommParameter.ParityList, x => x.Value == value.Parity, ParityList);
    }
    [RelayCommand(CanExecute = nameof(CanOpen))]
    private void Open()
    {
        var serialCommList = ParaSetupModules.SerialCommList;
        if (serialCommList!.Count < _serial)
        {
            MessageText = "主板内部处理逻辑有误，serial与主板列表不一致！";
            return;            
        }
        var serialCommInfo = serialCommList[_serial-1];
        if (!serialCommInfo.IsValid())
        {
            MessageText = "主板参数无效，如果已经配置过，请重新启动程序加载！";
            return;            
        }
        //连接尝试串口
        
    }

    [RelayCommand(CanExecute = nameof(CanClose))]
    private void Close()
    {
    }

    private bool CanOpen()
    {
        return !_start && _serial is > 0 and < 2; 
    }
    private bool CanClose()
    {
        return _start; 
    }
}