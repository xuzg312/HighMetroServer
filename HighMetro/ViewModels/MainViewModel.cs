using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HighMetro.BaseModel;
using HighMetro.Message;
using HighMetro.Models;
using HighMetro.Parameters;
using HighMetro.Services;

namespace HighMetro.ViewModels;
public partial class MainViewModel : ViewModelBase
{
    private readonly IConfigService _configService;
    private readonly IDbService _dbService;
    private readonly Window _mainWindow;

    private DbSetting? _dbSetting;
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
        // 绑定命令与执行方法
        CameraMaintainCmd = new RelayCommand(CameraMaintain);
        BoardMaintainCmd = new RelayCommand(BoardMaintain);
        OpenPhotoQueryCmd = new RelayCommand(OpenPhotoQuery);
        OpenFaultQueryCmd = new RelayCommand(OpenFaultQuery);
        IsMenuEnabled = false;
        InitializeStartup();
    }
    private void InitializeStartup()
    {
        // 读取本地数据库配置
        _dbSetting = _configService.LoadDbConfig();
        ResultInfo resultInfo;
        if (_dbSetting.IsValid())
        {
            //数据库参数已配置，校验是否正确？
            resultInfo = _dbService.TestConnection(_dbSetting);
        }
        else
        {
            //数据库参数未配置，或者配置无效；
            resultInfo = new ResultInfo
            {
                Code = PublicConst.FlagNo,
                Message = ""
            };
        }
        if (!resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            //未设置，或者异常，需要重新配置数据库参数；
            var vm = new DbConfigViewModel(_configService, _dbService,_dbSetting,resultInfo);
            // 注册回调：数据库配置确认成功后打开登录窗口
            vm.OnDbConfigSuccess += OnDbConfigSuccess;
            vm.OnDbConfigCancel += ExitApplication;
            ShowOverlay = true;
            ActivePopupVm = vm;
        }
        else
        {
            var loginSetting = _configService.LoadLoginConfig();
            var vm = new LoginViewModel(_configService,_dbService,loginSetting,_dbSetting);
            vm.OnLoginSuccess += OnLoginSuccess;
            vm.OnLoginCancel += ExitApplication;            
            ShowOverlay = true;
            ActivePopupVm = vm;
        }
    }

    private void OnDbConfigSuccess(DbSetting setting)
    {
        _dbSetting = setting;
        var loginSetting = _configService.LoadLoginConfig();
        Dispatcher.UIThread.Post(() =>
        {
            var vm = new LoginViewModel(_configService, _dbService, loginSetting, _dbSetting);
            vm.OnLoginSuccess += OnLoginSuccess;
            vm.OnLoginCancel += ExitApplication;
            if (ActivePopupVm is DbConfigViewModel oldDbConfigVm)
            {
                oldDbConfigVm.OnDbConfigSuccess -= OnDbConfigSuccess;
                oldDbConfigVm.OnDbConfigCancel -= ExitApplication;
            }
            ActivePopupVm = vm;
        });
    }

    private void OnLoginSuccess(LoginSetting setting)
    {
        var hostSetting = _configService.LoadHostConfig();
        ResultInfo resultInfo;
        if (hostSetting.IsValid())
        {
            //选择了工控机，确认是否正确？
            resultInfo = _dbService.VerifyHost(hostSetting,_dbSetting!);
        }
        else
        {
            //工控机参数未配置，或者配置无效；
            resultInfo = new ResultInfo
            {
                Code = PublicConst.FlagNo,
                Message = ""
            };
        }
        if (!resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            //未设置，或者异常，需要重新选择工控机；
            var resultHostInfo = _dbService.GetHostList(_dbSetting);
            Dispatcher.UIThread.Post(() =>
            {
                var vm = new HostSelectViewModel(_configService, resultHostInfo);
                vm.OnConfirm += OnHostSuccess;
                vm.OnCancel += ExitApplication;
                if (ActivePopupVm is LoginViewModel oldloginVm)
                {
                    oldloginVm.OnLoginSuccess -= OnLoginSuccess;
                    oldloginVm.OnLoginCancel -= ExitApplication;
                }
                ActivePopupVm = vm;
            });
        }
        else
        {
            OnHostSuccess(hostSetting);
        }
    }
    
    private void OnHostSuccess(HostSetting setting)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (ActivePopupVm is HostSelectViewModel oldHostVm)
            {
                oldHostVm.OnConfirm -= OnHostSuccess;
                oldHostVm.OnCancel -= ExitApplication;
            }
        });
        //保存数据库连接；
        var dataBaseConnect = DataBaseConnect.Instance;
        dataBaseConnect.SetDataBaseConn(_dbSetting!.GetConnectionString());
        //获取工控机；、主板1、主板2、硬盘摄像机；
        var hostInfo = new HostInfo
        {
            Bh = setting.Bh
        };
        var resultInfo = _dbService.GetHostInfo(hostInfo);
        if (resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            //获取摄像机；
            var hardInfo = new HardInfo
            {
                HostBh = hostInfo.Bh,
                Type = PublicConst.PhotoCamera
            };
            resultInfo = _dbService.GetHardCamera(hardInfo);
            if (resultInfo.Code.Equals(PublicConst.FlagYes))
            {
                //获取主板，最多2个主板；
                var resultSerialCommInfo = _dbService.GetCommInfoList(hostInfo,PublicConst.Mainboard);
                resultInfo = resultSerialCommInfo.ReturnInfo;
                if (resultInfo.Code.Equals(PublicConst.FlagYes))
                {
                    Dispatcher.UIThread.Post(() => 
                    {
                       //所有参数都正常，打开主页面；
                        ParaSetupModules.HostInfo = hostInfo;
                        ParaSetupModules.CamInfo = hardInfo;
                        ParaSetupModules.SerialCommList = resultSerialCommInfo.SerialCommList;
                        ParaSetupModules.DbService = _dbService;
                        MainPageVm = new MainPageViewModel();
                        ActivePopupVm = null;
                        ShowOverlay = false;
                        IsMenuEnabled = true;
                    });
                    return;
                }
            }
        }
        //展示错误信息；
        Dispatcher.UIThread.Post(() =>
        {
            var vm = new LoadParaViewModel(resultInfo);
            vm.OnCancel += ExitApplication;
            ActivePopupVm = vm;
        });
    }
    public ICommand CameraMaintainCmd { get; }
    public ICommand BoardMaintainCmd { get; }
    public ICommand OpenPhotoQueryCmd { get; }
    public ICommand OpenFaultQueryCmd { get; }

    private bool _isMenuEnabled;
    public bool IsMenuEnabled
    {
        get => _isMenuEnabled;
        set => SetProperty(ref _isMenuEnabled, value);
    }
    //===== 菜单点击命令 =====
    private void CameraMaintain()
    {
        //获取摄像机；
        var hardInfo = new HardInfo
        {
            HostBh = ParaSetupModules.HostInfo!.Bh,
            Type = PublicConst.PhotoCamera
        };
        var resultInfo = _dbService.GetHardCamera(hardInfo);
        var vm = new EditCamConfigViewModel(_dbService, hardInfo,resultInfo);
        vm.OnHardConfigSuccess += OnHardEnd;
        vm.OnHardConfigCancel += OnHardEnd;
        IsMenuEnabled = false;
        ActivePopupVm = vm;
        ShowOverlay = true;
    }

    private void OnHardEnd()
    {
        if (ActivePopupVm is EditCamConfigViewModel oldHardVm)
        {
            oldHardVm.OnHardConfigSuccess -= OnHardEnd;
            oldHardVm.OnHardConfigCancel -= OnHardEnd;
        }
        ActivePopupVm = null;
        ShowOverlay = false;
        IsMenuEnabled = true;
    }

    private void BoardMaintain()
    {
        // ActivePopupVm = new BoardMaintainViewModel();
        // ShowOverlay = true;
    }
    private void OpenPhotoQuery()
    {
        // ActivePopupVm = new PhotoQueryViewModel();
        // ShowOverlay = true;
    }
    private void OpenFaultQuery()
    {
        // ActivePopupVm = new FaultQueryViewModel();
        // ShowOverlay = true;
    }
    private void ExitApplication()
    {
        _mainWindow.Close();
    }
    public void CleanResources()
    {
        WeakReferenceMessenger.Default.Send(new AppCleanupMessage());
        Console.WriteLine("程序结束，释放资源！");
    }
}