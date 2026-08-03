using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetro.Models;
using HighMetro.Parameters;
using HighMetro.Services;

namespace HighMetro.ViewModels;
public partial class MainViewModel : ViewModelBase
{
    private readonly IConfigService _configService;
    private readonly IDbService _dbService;
    private readonly Window _mainWindow;
    // 弹窗遮罩是否显示
    [ObservableProperty]
    private bool _showOverlay = false;

    // 当前激活弹窗VM
    [ObservableProperty]
    private object? _activePopupVm;
    
    [ObservableProperty]
    private MainPageViewModel? _mainPageVm;
    public MainViewModel(Window mainWindow)
    {
        _mainWindow = mainWindow;
        _configService = new ConfigService();
        _dbService = new DbService();
        InitializeStartup();
    }

    private void InitializeStartup()
    {
        // 读取本地数据库配置
        var vm = new DbConfigViewModel(_configService, _dbService);
        // 注册回调：数据库配置确认成功后打开登录窗口
        vm.OnDbConfigSuccess += OnDbConfigSuccess;
        vm.OnDbConfigCancel += ExitApplication;
        ShowOverlay = true;
        ActivePopupVm = vm;
    }

    private void OnDbConfigSuccess(DbSetting setting)
    {
        var vm = new LoginViewModel(_configService,_dbService);
        vm.OnLoginSuccess += OnLoginSuccess;
        vm.OnLoginCancel += ExitApplication;
        if (ActivePopupVm is DbConfigViewModel oldLoginVm)
        {
            oldLoginVm.OnDbConfigSuccess = null;
            oldLoginVm.OnDbConfigCancel = null;
        }
        ActivePopupVm = vm;
    }

    private void OnLoginSuccess(LoginSetting setting)
    {
        var vm = new HostSelectViewModel(_configService,_dbService);
        vm.OnConfirm += OnHostSuccess;
        vm.OnCancel += ExitApplication;
        if (ActivePopupVm is HostSelectViewModel oldHostVm)
        {
            oldHostVm.OnConfirm = null;
            oldHostVm.OnCancel = null;
        }
        ActivePopupVm = vm;
    }
    
    private void OnHostSuccess(HostSetting setting)
    {
        if (ActivePopupVm is HostSelectViewModel oldHostVm)
        {
            oldHostVm.OnConfirm = null;
            oldHostVm.OnCancel = null;
        }
        MainPageVm = new MainPageViewModel();
        ActivePopupVm = null;
        ShowOverlay = false;
    }
    private void ExitApplication()
    {
        _mainWindow.Close();
    }
    public void CleanResources()
    {
        // 1.停止海康摄像头预览、释放SDK
        // 2.关闭数据库连接
        // 3.停止后台定时器、异步任务
        // 4.释放视频解码、图像资源
        Console.WriteLine("开始释放系统资源!");
    }
}