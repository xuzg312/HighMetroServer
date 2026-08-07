using CommunityToolkit.Mvvm.ComponentModel;
using HighMetro.Models;

namespace HighMetro.ViewModels;

public partial class CamConfigViewModel : ObservableObject
{
    // 1. 接收外部传入的配置数据
    [ObservableProperty]
    private CamOptions? _config;
    
    // 2. 接收外部传入的只读状态
    [ObservableProperty] 
    private bool _isReadOnly;

    [ObservableProperty] 
    private string _camState;
    // UI绑定字段
    [ObservableProperty]
    private string _ip = string.Empty;

    [ObservableProperty]
    private int _port = 3000;
    
    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private bool _isInfoHit;
    
    [ObservableProperty]
    private string _messageText = string.Empty;

    private bool _start;
    public CamConfigViewModel(bool isReadOnly)
    {
        _isInfoHit = true;
        IsReadOnly = isReadOnly;
        _start = false;
        CamState = "【摄像机，启动状态：❌ 】";
    }
    partial void OnConfigChanged(CamOptions? value)
    {
        if (value is null)
            return;
        // 将外部HostOptions模型映射拷贝到UI属性
        Ip = value.Ip;
        Port = value.Port;
        UserName = value.UserName;
    }
}