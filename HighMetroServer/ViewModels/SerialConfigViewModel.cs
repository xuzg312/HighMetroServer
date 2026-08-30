using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HighMetroServer.BaseModel;
using HighMetroServer.ClassLib;
using HighMetroServer.Event;
using HighMetroServer.Message;
using HighMetroServer.Models;
using HighMetroServer.Parameters;
using HighMetroServer.Services;

namespace HighMetroServer.ViewModels;

public partial class SerialConfigViewModel : ObservableObject,IRecipient<AppCleanupMessage>
{
    [ObservableProperty]
    private SerialPortOptions? _config;

    [ObservableProperty] 
    private string _commState;
    
    [ObservableProperty] 
    private int? _id;
    
    [ObservableProperty] 
    private string? _name;

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

    [ObservableProperty] 
    private string? _messageText1;
    
    [ObservableProperty] 
    private string? _messageText2;
    
    [ObservableProperty] 
    private string? _messageText3;
    
    [ObservableProperty] 
    private string? _messageText4;
    
    private bool _start;
    private int _serial;
    private CommSerialImpl? _commSerialImpl;
    private bool _buildServer;
    private static readonly SemaphoreSlim AsyncLock = new (1, 1);

    public SerialConfigViewModel(int serial)
    {
        _serial = serial;
        _start = false;
        _buildServer = false;
        foreach (var item in ParaSetupModules.SerialCommList!)
        {
            item.BufferDataProdEvent += OnBufferDataProdEvent;
        }
        CommState = "【 串口连接状态：❌ 】";
        WeakReferenceMessenger.Default.Register(this);
    }
    public void UpdateParams(int serial, SerialPortOptions? options)
    {
        _serial = serial;
        InitComboSource(options);
    }
    private static CodeNameModals? BuildSingleItem(
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
        Id = value.Id;
        Name=value.Name;
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
    //收到串口数据；
    private void OnBufferDataProdEvent(object? obj, EventArgs arg)
    {
        if (arg is not SocketDataEventArgs socketDataEventArgs)
        {
            return;
        }
        var socketDataBlock = socketDataEventArgs.Data;
        var valid = false;
        if (socketDataBlock.Length >= 11)
        {
            if (socketDataBlock.Content![0] == 0XEB &&
                    socketDataBlock.Content[1] == 0XAA)
            {
                if (socketDataBlock.Content[5] == 0X80 && socketDataBlock.Content[6] == 0X0F)
                {
                    //心跳；
                    if (socketDataBlock.Length >= 62)
                    {
                        //转发到TcpClient;
                        var tcpServer = ParaSetupModules.HostInfo!.TcpServer;
                        tcpServer?.SendMessage(socketDataBlock);
                        //保存心跳;
                        _= ReplyHeartInfo(socketDataBlock);
                        valid = true;
                    }
                }
                else if (socketDataBlock.Content[5] == 0X84)
                {
                    //拍照、录像；                         
                    var cameraBean = new CameraBean();
                    byte iPosition = 6;
                    var dire = socketDataBlock.Content[iPosition];
                    if (dire == 0X0F)
                    {
                        cameraBean.Door = PublicConst.DireDoor;
                        iPosition = 10;
                        var state = socketDataBlock.Content[iPosition];
                        if (state == 0X00)
                        {
                            //动作；拍照；
                            //转发到TcpClient;
                            var tcpServer = ParaSetupModules.HostInfo!.TcpServer;
                            tcpServer?.SendMessage(socketDataBlock);
                            cameraBean.Type = PublicConst.DoorStateCapture;
                            _= ReplyCaptureInfo(socketDataBlock, cameraBean);
                            valid = true;
                        }
                        else if (state == 0X01)
                        {
                            //录像;
                            cameraBean.Type = PublicConst.DoorStateCamera;
                            _= ReplyCameraInfo(socketDataBlock, cameraBean);
                            valid = true;
                        } 
                    }
                }
            }
        }
        if (!valid)
        {
            //协议数据无效；
            ParaSetupModules.RaiseHexDataProdEvent(socketDataBlock);
        }
    }
    //心跳；
    private async Task ReplyHeartInfo(SocketDataBlock socketDataBlock)
    { 
        await ReplyHeart(socketDataBlock);
    }
    private async Task ReplyHeart(SocketDataBlock socketDataBlock)
    { 
        await AsyncLock.WaitAsync();
        try
        {
            var mainInfoBean = ParseMainBordData.ReplyHeartInfo(socketDataBlock);
            if (mainInfoBean != null)
            {
                mainInfoBean.HostBh = ParaSetupModules.CamInfo!.HostBh;
                var data = ParseMainBordData.ParsePack(mainInfoBean);
                ResultInfo resultInfo;
                if (mainInfoBean.A1gzm > 0 || mainInfoBean.A2gzm > 0 || mainInfoBean.B1gzm > 0 ||
                    mainInfoBean.B2gzm > 0)
                {
                    //异常的心跳，保存到数据库;
                    resultInfo = ParaSetupModules.DbService!.AddHeart(mainInfoBean);
                }
                else
                {
                    //正常心跳，数据库更新次数，每天一条记录；
                    mainInfoBean.Datetime = DateTime.Now.Date.ToString("yyyy-MM-dd HH:mm:ss");
                    resultInfo = ParaSetupModules.DbService!.SavePersonDay(mainInfoBean);
                }

                if (!resultInfo.Code.Equals(PublicConst.FlagYes))
                {
                    var currDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    ParaSetupModules.RaiseAscDataProdEvent($"{resultInfo.Message}【{currDate}】");
                }

                Dispatcher.UIThread.Post(() =>
                {
                    MessageText1 = data[0];
                    MessageText2 = data[1];
                    MessageText3 = data[2];
                    MessageText4 = data[3];
                    var currDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    MessageText = $"收到主板【{_serial}】的心跳数据！【{currDate}】";
                });
            }
            else
            {
                ParaSetupModules.RaiseHexDataProdEvent(socketDataBlock);
            }
        }
        finally
        {
            AsyncLock.Release();
        }
    }
    //拍照
    private async Task ReplyCaptureInfo(SocketDataBlock socketDataBlock, CameraBean cameraBean)
    { 
        await ReplyCapture(socketDataBlock,cameraBean);
    }
    private async Task ReplyCapture(SocketDataBlock socketDataBlock, CameraBean cameraBean)
    {
        cameraBean.HostBh = ParaSetupModules.HostInfo!.Bh;
        var publicUntil = new PublicUntil();
        byte iPosition = 3;
        //设备id
        cameraBean.Id = publicUntil.GetUshort(socketDataBlock.Content!, iPosition);
        cameraBean.DateTime = DateTime.Now;//.ToString("yyyy-MM-dd HH:mm:ss");
        //次数；
        iPosition = 8;
        cameraBean.Serial = publicUntil.GetUshort(socketDataBlock.Content!, iPosition);
        var camInfo = ParaSetupModules.CamInfo;
        var camRemoteLinkImpl = camInfo!.CamRemoteLinkImpl;
        if (camRemoteLinkImpl!=null && camRemoteLinkImpl.GetUserId()>=0)
        {
            //动作：拍照；
            var value = await camRemoteLinkImpl.CaptureJpegPicture(cameraBean,SystemInfo.PhotoDir); 
            if (value.Code.Equals(PublicConst.FlagYes))
            {
                cameraBean.Message = "拍照执行成功！";
                Dispatcher.UIThread.Post(() => { MessageText = $"{cameraBean.Message}【{cameraBean.DateTime}】";});
                var resultInfo = ParaSetupModules.DbService!.AddAlarm(cameraBean);
                if (!resultInfo.Code.Equals(PublicConst.FlagYes))
                {
                    ParaSetupModules.RaiseAscDataProdEvent($"{resultInfo.Message}【{cameraBean.DateTime}】");
                }
            }
            else
            {
                cameraBean.Message = value.Message;
                ParaSetupModules.RaiseAscDataProdEvent($"{value.Message}【{cameraBean.DateTime}】");
                var resultInfo = ParaSetupModules.DbService!.AddError(cameraBean);
                if (!resultInfo.Code.Equals(PublicConst.FlagYes))
                {
                    ParaSetupModules.RaiseAscDataProdEvent($"{resultInfo.Message}【{cameraBean.DateTime}】");
                }
            }
        }
        else
        {
            cameraBean.Message = "触发拍照，但未连接摄像头！";
            Dispatcher.UIThread.Post(() => { MessageText = $"{cameraBean.Message}【{cameraBean.DateTime}】";});
            var resultInfo = ParaSetupModules.DbService!.AddError(cameraBean);
            if (!resultInfo.Code.Equals(PublicConst.FlagYes))
            {
                ParaSetupModules.RaiseAscDataProdEvent(resultInfo.Message);
            }
        }
    }
    //录像
    private async Task ReplyCameraInfo(SocketDataBlock socketDataBlock, CameraBean cameraBean)
    { 
        await ReplyCamera(socketDataBlock,cameraBean);
    }
    private async Task ReplyCamera(SocketDataBlock socketDataBlock, CameraBean cameraBean)
    {
        cameraBean.HostBh = ParaSetupModules.HostInfo!.Bh;
        var publicUntil = new PublicUntil();
        byte iPosition = 3;
        //设备id
        cameraBean.Id = publicUntil.GetUshort(socketDataBlock.Content!, iPosition);
        cameraBean.DateTime = DateTime.Now; //.ToString("yyyy-MM-dd HH:mm:ss");
        //次数；
        iPosition = 8;
        cameraBean.Serial = publicUntil.GetUshort(socketDataBlock.Content!, iPosition);
        var camInfo = ParaSetupModules.CamInfo;
        var camRemoteLinkImpl = camInfo!.CamRemoteLinkImpl;
        if (camRemoteLinkImpl != null && camRemoteLinkImpl.GetUserId()>=0)
        {
            //动作：录像；
            var value = await camRemoteLinkImpl.PlayCam(cameraBean,SystemInfo.PhotoDir); 
            if (value.Code.Equals(PublicConst.FlagYes))
            {
                cameraBean.Message = "录像执行成功！";
                Dispatcher.UIThread.Post(() => { MessageText = $"{cameraBean.Message}【{cameraBean.DateTime}】";});
                var resultInfo = ParaSetupModules.DbService!.AddAlarm(cameraBean);
                if (!resultInfo.Code.Equals(PublicConst.FlagYes))
                {
                    ParaSetupModules.RaiseAscDataProdEvent($"{resultInfo.Message}【{cameraBean.DateTime}】");
                }
            }
            else
            {
                cameraBean.Message = value.Message;
                ParaSetupModules.RaiseAscDataProdEvent($"{value.Message}【{cameraBean.DateTime}】");
                var resultInfo = ParaSetupModules.DbService!.AddError(cameraBean);
                if (!resultInfo.Code.Equals(PublicConst.FlagYes))
                {
                    ParaSetupModules.RaiseAscDataProdEvent($"{resultInfo.Message}【{cameraBean.DateTime}】");
                }
            }
        }
        else
        {
            cameraBean.Message = "触发录像，但未连接摄像头！";
            Dispatcher.UIThread.Post(() => { MessageText = $"{cameraBean.Message}【{cameraBean.DateTime}】";});
            var resultInfo = ParaSetupModules.DbService!.AddError(cameraBean);
            if (!resultInfo.Code.Equals(PublicConst.FlagYes))
            {
                ParaSetupModules.RaiseAscDataProdEvent(resultInfo.Message);
            }
        }
    }
    public async Task Start()
    {
        if (PublicConst.SelfStart == 1)
        {
            if (!_start)
            {
                if (SelectPortName is null || SelectedBaudRate is null || SelectedDataBits is null ||
                    SelectedStopBits is null || SelectedParity is null)
                {
                    return;
                }
                await Task.Delay(2000); 
                Open();
            }
        }
    }
    [RelayCommand(CanExecute = nameof(CanOpen))]
    private void Open()
    {
        if (!_buildServer)
        {
            if (_serial == 0)
            {
                MessageText = "主板参数处理逻辑有误，请联系开发人员检查！";
                return;
            }
            if (SelectPortName is null || SelectedBaudRate is null || SelectedDataBits is null ||
                SelectedStopBits is null || SelectedParity is null)
            {
                MessageText = "主板参数未配置，请点击菜单【设备管理--主板维护】进行设置，设置后需要重新启动程序！";
                return;
            }
            var serialCommList = ParaSetupModules.SerialCommList;
            if (serialCommList!.Count < _serial)
            {
                MessageText = "主板内部处理逻辑有误，serial与主板列表不一致！";
                return;
            }
            var serialCommInfo = serialCommList[_serial - 1];
            if (!serialCommInfo.IsValid())
            {
                MessageText = "主板参数无效，如果已经配置过，请重新启动程序加载！";
                return;
            }
            //连接尝试串口
            _commSerialImpl = new CommSerialImpl(PublicConst.CommDataParseTask, serialCommInfo);
            serialCommInfo.CommSerialImpl = _commSerialImpl;
            _buildServer = true;
        }
        if (_commSerialImpl!.Open())
        {
            CommState = "【 串口连接状态：✅ 】";
            _start = true;
        }
        else
        {
            CommState = "【 串口连接状态：❌ 】";
        }
        OpenCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();    
    }
    [RelayCommand(CanExecute = nameof(CanClose))]
    private void Close()
    {
        _commSerialImpl!.Close();
        _start = false;
        CommState = "【 串口连接状态：❌ 】";
        OpenCommand.NotifyCanExecuteChanged();
        CloseCommand.NotifyCanExecuteChanged();    
    }
    private bool CanOpen()
    {
        return !_start; 
    }
    private bool CanClose()
    {
        return _start; 
    }
    private void ClearResource()
    {
        if(!_start)
            return;
        _commSerialImpl!.Close();
        _start = false;
    }
    public void Receive(AppCleanupMessage message)
    {
        Console.WriteLine("释放串口资源-----Receive！");
        WeakReferenceMessenger.Default.UnregisterAll(this);
        ClearResource();
    }
}