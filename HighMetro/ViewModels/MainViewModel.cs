using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetro.BaseModel;
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
            resultInfo = new ResultInfo();
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "";
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
            LoginSetting loginSetting = _configService.LoadLoginConfig();
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
        LoginSetting loginSetting = _configService.LoadLoginConfig();
        var vm = new LoginViewModel(_configService,_dbService,loginSetting,_dbSetting);
        vm.OnLoginSuccess += OnLoginSuccess;
        vm.OnLoginCancel += ExitApplication;
        if (ActivePopupVm is DbConfigViewModel oldDbConfigVm)
        {
            oldDbConfigVm.OnDbConfigSuccess -= OnDbConfigSuccess;
            oldDbConfigVm.OnDbConfigCancel -= ExitApplication;
        }
        ActivePopupVm = vm;
    }

    private void OnLoginSuccess(LoginSetting setting)
    {
        HostSetting hostSetting = _configService.LoadHostConfig();
        ResultInfo resultInfo;
        if (hostSetting.IsValid())
        {
            //选择了工控机，确认是否正确？
            resultInfo = _dbService.VerifyHost(hostSetting,_dbSetting);
        }
        else
        {
            //工控机参数未配置，或者配置无效；
            resultInfo = new ResultInfo();
            resultInfo.Code = PublicConst.FlagNo;
            resultInfo.Message = "";
        }
        if (!resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            //未设置，或者异常，需要重新选择工控机；
            ResultHostInfo resultHostInfo = _dbService.GetHostList(_dbSetting);
            var vm = new HostSelectViewModel(_configService, resultHostInfo);
            vm.OnConfirm += OnHostSuccess;
            vm.OnCancel += ExitApplication;
            if (ActivePopupVm is LoginViewModel oldloginVm)
            {
                oldloginVm.OnLoginSuccess -= OnLoginSuccess;
                oldloginVm.OnLoginCancel -= ExitApplication;
            }
            ActivePopupVm = vm;
        }
        else
        {
            OnHostSuccess(hostSetting);
        }
    }
    
    private void OnHostSuccess(HostSetting setting)
    {
        if (ActivePopupVm is HostSelectViewModel oldHostVm)
        {
            oldHostVm.OnConfirm -= OnHostSuccess;
            oldHostVm.OnCancel -=  ExitApplication;
        }
        //保存数据库连接；
        DataBaseConnect dataBaseConnect = DataBaseConnect.Instance;
        dataBaseConnect.SetDataBaseConn(_dbSetting.GetConnectionString());
        //获取工控机；、主板1、主板2、硬盘摄像机；
        HostInfo hostInfo = new HostInfo();
        hostInfo.Bh = setting.Bh;
        ResultInfo resultInfo = _dbService.GetHostInfo(hostInfo);
        if (resultInfo.Code.Equals(PublicConst.FlagYes))
        {
            //获取摄像机；
            HardInfo hardInfo = new HardInfo();
            hardInfo.HostBh = hostInfo.Bh;
            hardInfo.Type = PublicConst.PhotoCamera;
            resultInfo=_dbService.GetHardCamera(hardInfo);
            if (resultInfo.Code.Equals(PublicConst.FlagYes))
            {
                //获取主板，最多2个主板；
                ResultSerialCommInfo resultSerialCommInfo = _dbService.GetCommInfoList(hostInfo,PublicConst.Mainboard);
                resultInfo = resultSerialCommInfo.ReturnInfo;
                if (resultInfo.Code.Equals(PublicConst.FlagYes))
                {
                    //所有参数都正常，打开主页面；
                    MainPageVm = new MainPageViewModel(hostInfo,hardInfo,resultSerialCommInfo.SerialCommList,_dbService);
                    ActivePopupVm = null;
                    ShowOverlay = false;
                    return;
                }
            }
        }
        //展示错误信息；
        var vm = new LoadParaViewModel(resultInfo);
        vm.OnCancel += ExitApplication;
        ActivePopupVm = vm; 
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
        Console.WriteLine("程序结束，释放资源！");
    }
}