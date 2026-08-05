using System;
using CommunityToolkit.Mvvm.ComponentModel;
using HighMetro.Models;

namespace HighMetro.ViewModels;

public partial class HostConfigViewModel : ObservableObject
{
    // 1. 接收外部传入的配置数据
    [ObservableProperty]
    private HostOptions? _config;
    
    // 2. 接收外部传入的只读状态
    [ObservableProperty] 
    private bool _isReadOnly;

    // UI绑定字段
    [ObservableProperty]
    private string _ip = string.Empty;

    [ObservableProperty]
    private int _port = 3000;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _hasUnappliedConfig=true;
    
    [ObservableProperty]
    private string _messageText = string.Empty;
    public HostConfigViewModel(bool isReadOnly)
    {
        HasUnappliedConfig = true;
        IsReadOnly = isReadOnly;
    }
    partial void OnConfigChanged(HostOptions? value)
    {
        if (value is null)
            return;
        // 将外部HostOptions模型映射拷贝到UI属性
        Ip = value.Ip;
        Port = value.Port;
        Code = value.Code;
        Name = value.Name;
    }
    public void AppendMessage(string msg)
    {
        MessageText = $"[{DateTime.Now:HH:mm:ss}] {msg}";
    }
    partial void OnIpChanged(string value)
    {
        if(Config is not null) Config.Ip = value;
    }

    partial void OnPortChanged(int value)
    {
        if(Config is not null) Config.Port = value;
    }
    partial void OnCodeChanged(string value)
    {
        if(Config is not null) Config.Code = value;
    }
    partial void OnNameChanged(string value)
    {
        if(Config is not null) Config.Name = value;
    }
}