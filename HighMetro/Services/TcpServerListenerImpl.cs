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

namespace HighMetro.Services;

public class TcpServerListenerImpl(HostInfo hostInfo, int threadCount)
{
    #region 私有数据;
    private TcpListener? _listener;
    private IDataBufferPool? _iDataBufferPool;
    private readonly List<IGetBufferData> _getBufferDataImplList=[];
    private Task? _readTask;
    private readonly ConcurrentDictionary<string, IChildCommunication> _dictionary
                                   = new ConcurrentDictionary<string, IChildCommunication>();
    private bool _start;
    private CancellationTokenSource? _ctsServer;
    private Task? _acceptLoopTask;
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
            _acceptLoopTask = AcceptClientLoopAsync(_ctsServer.Token);
            //启动1个线程，进行数据包的拆分或合并；
            _readTask = Task.Run(() => ParseData(_ctsServer.Token));
            _start = true;
            return true;
        }
        catch (Exception)
        {
            _start = false;
            ClearResource();
            hostInfo.RaiseClientConnEvent("启动Server失败！");
            return false;
        }
    }
    #region 接收客户端连接事件；
    private async Task AcceptClientLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener != null)
        {
            try
            {
                TcpClient tcpClient = await _listener.AcceptTcpClientAsync(token);
                if (tcpClient.Client.RemoteEndPoint is not IPEndPoint endPoint)
                {
                    continue;
                }
                string key = endPoint.Address + "【" + hostInfo.Bh + "】";
                IChildCommunication newChild = new TcpServerChatImpl(
                    tcpClient, 
                    hostInfo, 
                    _iDataBufferPool!,
                    _dictionary,
                    key,
                    token);
                _dictionary.TryRemove(key, out IChildCommunication? oldChild);
                _dictionary.TryAdd(key, newChild);
                oldChild?.CloseClient();
            }
            catch (Exception)
            {
                hostInfo.RaiseAscDataProdEvent("接收客户端异常！");
            }
        }
    }
    #endregion
    #region 解析tcp数据；
    private async Task ParseData(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            int execCount = 0;
            // 复制一份集合快照，遍历副本，不在原字典上foreach
            var childList = _dictionary.Values.ToList();
            foreach (var child in childList)
            {
                if (child.ParseDatas())
                {
                    execCount++;
                }
            }
            if (execCount == 0)
            {
                await Task.Delay(100, token);
            }
        }
    }
    #endregion

    private void SendMessage(SocketDataBlock socketDataBlock)
    {
        if (!_start)
            return;
        var snapshotList = _dictionary.Values.ToList();
        foreach(var child in snapshotList)
        {
            try
            {
                if (child.GetClientType() == PublicConst.IdentifyAll)
                {
                    child.SendMessage(socketDataBlock.Content!, socketDataBlock.Length);
                }
            }
            catch(Exception ex)
            {
                hostInfo.RaiseAscDataProdEvent($"广播发送异常：{ex.Message}");
            }
        }
    }
    public void IdentifyInfo(SocketDataBlock socketDataBlock, TcpDataBean tcpDataBean)
    {
        if (!_start)
            return;
        //查找需要接收数据在客户端；
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
        if (hostBh != tcpDataBean.HostBh)
        {
            //强行下线，工控机编号无效！
            tempComm.CloseClient();
            hostInfo.RaiseAscDataProdEvent(socketDataBlock.Key + "，工控机编号无效，强制下线！");
        }
        else
        {
            tempComm.SetClientType((byte)tcpDataBean.Type);
            SendMessage(socketDataBlock);
        }
    }
    public void SendPhotoFile(SocketDataBlock socketDataBlock,TcpDataBean tcpDataBean, byte[] fileData)
    {
        if (!_start)
            return;
        //查找需要接收数据在客户端；
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
        if (hostBh != tcpDataBean.HostBh)
        {
            //强行下线，工控机编号无效！
            tempComm.CloseClient();
            hostInfo.RaiseAscDataProdEvent(socketDataBlock.Key + "，工控机编号无效，强制下线！");
        }
        else
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
    }
    #region 关闭服务；
    public void CloseServer()
    {
        if (!_start)
            return;
        _start = false;
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
            kv.Value.CloseClient();
        }
        _dictionary.Clear(); // 清除集合，不要 =null
        foreach (var item in _getBufferDataImplList)
        {
            item.DisConnect();
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
        catch
        {
            //忽略；
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
    }
}