using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using HighMetro.Models;
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
    public ObservableCollection<CodeNameModals> PortNameList { get; }
    public ObservableCollection<CodeNameModals> BaudRateList { get; }
    public ObservableCollection<CodeNameModals> DataBitsList { get; }
    public ObservableCollection<CodeNameModals> ParityList { get; }
    public ObservableCollection<CodeNameModals> StopBitsList { get; } 
    
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

    public SerialConfigViewModel(bool isReadOnly)
    {
        IsReadOnly = isReadOnly;
        if (IsReadOnly)
        {
            PortNameList = new ObservableCollection<CodeNameModals>([]);
            BaudRateList = new ObservableCollection<CodeNameModals>([]);
            DataBitsList = new ObservableCollection<CodeNameModals>([]);
            ParityList = new ObservableCollection<CodeNameModals>([]);
            StopBitsList = new ObservableCollection<CodeNameModals>([]);
        }
        else
        {

            PortNameList = new ObservableCollection<CodeNameModals>(
            [
                new CodeNameModals(1, "COM1"),
                new CodeNameModals(2, "COM2")
            ]);
            BaudRateList = new ObservableCollection<CodeNameModals>(
            [
                new CodeNameModals(9600, "9600"),
                new CodeNameModals(19200, "19200"),
                new CodeNameModals(38400, "38400"),
                new CodeNameModals(57600, "57600"),
                new CodeNameModals(115200, "115200")
            ]);
            DataBitsList = new ObservableCollection<CodeNameModals>(
            [
                new CodeNameModals(7, "7:位数据"),
                new CodeNameModals(8, "8:位数据")
            ]);
            ParityList = new ObservableCollection<CodeNameModals>(
            [
                new CodeNameModals(0, "0:无校验"),
                new CodeNameModals(1, "1:奇校验"),
                new CodeNameModals(2, "2:偶校验")
            ]);
            StopBitsList = new ObservableCollection<CodeNameModals>(
            [
                new CodeNameModals(1, "1:停止位"),
                new CodeNameModals(2, "2:停止位")
            ]);
        }
    }
    // 5. 当外部传入 Config 时，自动同步给 ComboBox 的选中项
    partial void OnConfigChanged(SerialPortOptions? value)
    {
        if (value is null)
            return;
        SelectedBaudRate = BaudRateList.FirstOrDefault(x => x.Value == value.BaudRate);
        SelectedDataBits = DataBitsList.FirstOrDefault(x => x.Value == value.DataBits);
        SelectedStopBits = StopBitsList.FirstOrDefault(x => x.Value == value.StopBits);
        SelectedParity = ParityList.FirstOrDefault(x => x.Value == value.Parity);
        if (IsReadOnly)
        {
            if (SelectedBaudRate is not null) BaudRateList.Add(SelectedBaudRate);
            if (SelectedDataBits is not null) DataBitsList.Add(SelectedDataBits);
            if (SelectedParity is not null) ParityList.Add(SelectedParity);
            if (SelectedStopBits is not null) StopBitsList.Add(SelectedStopBits);
        }
    }
    
    // 6. 当用户在界面上选择了新的波特率，同步回 Config
    partial void OnSelectedBaudRateChanged(CodeNameModals? value)
    {
        if (Config != null && value != null)
            Config.BaudRate = value.Value;
    }

    partial void OnSelectedDataBitsChanged(CodeNameModals? value)
    {
        if (Config != null && value != null)
            Config.DataBits = value.Value;
    }
    partial void OnSelectedStopBitsChanged(CodeNameModals? value)
    {
        if (Config != null && value != null )
            Config.StopBits = value.Value;
    }

    partial void OnSelectedParityChanged(CodeNameModals? value)
    {
        if (Config != null && value != null )
            Config.Parity = value.Value;
    }
}