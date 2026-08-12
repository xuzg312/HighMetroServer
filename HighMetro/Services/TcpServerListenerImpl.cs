using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using HighMetro.BaseModel;
using HighMetro.ClassLib;
using HighMetro.Models;

namespace HighMetro.Services;

public class TcpServerListenerImpl(HostInfo hostInfo, int threadCount)
{
    #region 私有数据;
    private TcpListener? _listener;
    private IDataBufferPool? _iDataBufferPool;
    private readonly List<IGetBufferData> _getBufferDataImplList=[];
    private readonly ConcurrentDictionary<string, IChildCommunication> _dictionary=[];
    private bool _start;
    private CancellationTokenSource? _ctsServer;
    private Task? _acceptLoopTask;
    private Task? _readTask;

    #endregion
    
    public bool Start()
    {
        if (_start)
        {
            return true;
        }
        try
        {
            _dictionary.Clear();
            _ctsServer = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, hostInfo.Port);
            _listener.Start();
            //接收生产者串口数据；
            _iDataBufferPool = new DataBufferPoolImpl();
            //数据消费者
            _getBufferDataImplList.Clear();
            for (var i = 0; i < threadCount; i++)
            {
                _getBufferDataImplList.Add(new GetBufferDataImpl(_iDataBufferPool));
            }
            // 后台接受客户端循环
            _acceptLoopTask = Task.Run(() => AcceptClient(_ctsServer.Token), _ctsServer.Token);
            //启动1个线程，进行数据包的拆分或合并；
            _readTask = Task.Run(() => ParseClient(_ctsServer.Token), _ctsServer.Token);
            _start = true;
            return true;
        }
        catch (Exception)
        {
            ClearResource();
            hostInfo.RaiseClientConnEvent("启动Server失败！");
            return false;
        }
    }
    private async Task AcceptClient(CancellationToken token)
    {
        try
        {
            await AcceptClientLoopAsync(token);
        }
        catch (OperationCanceledException)
        {
            //主动取消监听，正常优雅关闭，不打错误日志
        }
        catch (Exception ex)
        {
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ParaSetupModules.RaiseAscDataProdEvent($"TCP监听顶层异常：{ex.Message}【{currDateTime}】");
        }
        finally
        {
            CloseServer();
        }
    }
    #region 接收客户端连接事件；
    private async Task AcceptClientLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener != null)
        {
            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(token);
                if (tcpClient.Client.RemoteEndPoint is not IPEndPoint endPoint)
                {
                    tcpClient.Close();
                    continue;
                }
                var key = endPoint.Address + "【" + hostInfo.Bh + "】";
                IChildCommunication newChild = new TcpServerChatImpl(
                    tcpClient, 
                    hostInfo, 
                    _iDataBufferPool!,
                    _dictionary,
                    key,
                    token);
                _dictionary.TryRemove(key, out var oldChild);
                _dictionary.TryAdd(key, newChild);
                oldChild?.CloseClient();
            }
            catch (Exception ex)
            {
                var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                ParaSetupModules.RaiseAscDataProdEvent($"接收客户端异常：{ex.Message}【{currDateTime}】");
            }
        }
    }
    #endregion
    private async Task ParseClient(CancellationToken token)
    {
        try
        {
            await ParseData(token);
        }
        catch (OperationCanceledException)
        {
            //正常关闭；
        }
        catch (Exception ex)
        {
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ParaSetupModules.RaiseAscDataProdEvent($"解析循环顶层异常：{ex.Message}【{currDateTime}】");
        }
    }
    #region 解析tcp数据；
    private async Task ParseData(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var execCount = 0;
            var childList = _dictionary.Values.ToList();
            foreach (var child in childList)
            {
                if (!child.IsStart())
                    continue;
                try
                {
                    var hasProcessData = child.ParseDatas();
                    if (hasProcessData)
                    {
                        execCount++;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // 单个客户端解析异常隔离，不中断整体轮询
                    var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    ParaSetupModules.RaiseAscDataProdEvent($"解析客户端数据异常：{ex.Message}【{currentTime}】");
                }
            }
            if (execCount > 0)
            {
                continue;
            }
            await Task.Delay(100, token);
        }
    }
    #endregion

    public void SendMessage(SocketDataBlock socketDataBlock)
    {
        if (!_start)
            return;
        var snapshotList = _dictionary.Values.ToList();
        foreach(var child in snapshotList)
        {
            if (!child.IsStart())
                continue;
            try
            {
                if (child.GetClientType() == PublicConst.IdentifyAll)
                {
                    child.SendMessage(socketDataBlock.Content!, socketDataBlock.Length);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch(Exception ex)
            {
                var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                ParaSetupModules.RaiseAscDataProdEvent($"广播发送异常：{ex.Message}【{currDateTime}】");
            }
        }
    }
    public void IdentifyInfo(SocketDataBlock socketDataBlock, TcpDataBean tcpDataBean)
    {
        if (!_start)
            return;
        IChildCommunication? tempComm = null;
        var hostBh = -1;
        var exist = false;
        if (_dictionary.TryGetValue(socketDataBlock.Key!, out var comm))
        {
            exist = true;
            tempComm = comm;
            hostBh = comm.GetHostBh();
        }
        if (!exist || tempComm == null)
        {
            return;
        }
        if (hostBh == tcpDataBean.HostBh)
        {
            tempComm.SetClientType((byte)tcpDataBean.Type);
            SendMessage(socketDataBlock);
        }
        else
        {
            tempComm.CloseClient();
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            hostInfo.RaiseClientConnEvent($"{socketDataBlock.Key}，工控机编号无效，强制下线！【{currDateTime}】");
        }
    }
    public void SendPhotoFile(SocketDataBlock socketDataBlock,TcpDataBean tcpDataBean, byte[] fileData)
    {
        if (!_start)
            return;
        IChildCommunication? tempComm = null;
        var hostBh = -1;
        var exist = false;
        if (_dictionary.TryGetValue(socketDataBlock.Key!, out var comm))
        {
            exist = true;
            tempComm = comm;
            hostBh = comm.GetHostBh();
        }
        if (!exist || tempComm == null)
        {
            return;
        }
        if (hostBh == tcpDataBean.HostBh)
        {
            //循环发送，每次8*1024字节；
            var data = new byte[fileData.Length + 10];
            var iPosition = 0;
            data[iPosition++] = 0XEC;
            data[iPosition++] = 0XAB;
            //文件大小，占用4表字节；
            PublicUntil publicUntil = new PublicUntil();
            publicUntil.GetInt(fileData.Length + 3, data, iPosition);
            iPosition += 4;
            //设备id
            publicUntil.GetShort((ushort)tcpDataBean.Id, data, iPosition);
            iPosition += 2;
            //功能码；
            data[iPosition++] = 0XAC;
            //文件内容；
            Array.Copy(fileData, 0, data, iPosition, fileData.Length);
            iPosition += fileData.Length;
            //结尾；
            data[iPosition] = 0XED;
            socketDataBlock.Content = data;
            socketDataBlock.Length = data.Length;
            tempComm.SendMessage(socketDataBlock.Content, socketDataBlock.Length);
        }
        else
        {
            tempComm.CloseClient();
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            hostInfo.RaiseClientConnEvent($"{socketDataBlock.Key}，工控机编号无效，强制下线！【{currDateTime}】");
        }
    }
    #region 关闭服务；
    public void CloseServer()
    {
        if (!_start)
            return;
        ClearResource();
    }
    #endregion

    private void ClearResource()
    {
        if (_listener != null)
        {
            try
            {
                _listener.Stop();
            }
            catch (Exception)
            {
                //忽略；
            }
            _listener = null;
        }
        try
        {
            _ctsServer?.Cancel();
        }
        catch
        {
            //忽略；
        }
        //断开与客户端的连接；
        foreach (var kv in _dictionary)
        {
            try
            {
                kv.Value.CloseClient();
            }
            catch (Exception)
            {
                //忽略；
            }
        }
        _dictionary.Clear(); 
        foreach (var item in _getBufferDataImplList)
        {
            try
            {
                item.DisConnect();
            }
            catch (Exception)
            {
                //忽略；
            }
        }
        _getBufferDataImplList.Clear();
        try
        {
            _acceptLoopTask?.Wait(500);
        }
        catch
        {
            //忽略；
        }
        _acceptLoopTask = null;
        try
        {
            _readTask?.Wait(500);
        }
        catch (Exception)
        {
            //忽略;
        }
        _readTask = null;
        try
        {
            _ctsServer?.Dispose();
        }
        catch
        {
            //忽略；
        }
        _ctsServer = null;
        _start = false;
    }
}