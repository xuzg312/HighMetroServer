using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using HighMetro.BaseModel;
using HighMetro.Models;

namespace HighMetro.Services;

public class TcpServerChatImpl : IChildCommunication
{
    #region 私有数据；
    private readonly HostInfo _hostInfo;
    private readonly IDataBufferPool _iDataBufferPool;
    private readonly List<byte> _receiveBuffer;
    private readonly ConcurrentQueue<byte[]> _receiveQueue;
    private TcpClient _client;
    private readonly string _key;
    private readonly ConcurrentDictionary<string, IChildCommunication> _dictionary;
    private byte _clientType;
    private const int MaxPacket = 1025*500; //0.5M
    private readonly CancellationTokenSource _clientCts;
    private bool _start;
    private Task? _readTask;
    private readonly SemaphoreSlim _streamSemaphore = new SemaphoreSlim(1, 1);
    #endregion

    #region 构造函数；
    public TcpServerChatImpl(TcpClient client, HostInfo hostInfo, IDataBufferPool iDataBufferPool, 
        ConcurrentDictionary<string, IChildCommunication> dictionary,string key,CancellationToken serverToken)
    {
        _client = client;
        _hostInfo = hostInfo;
        _iDataBufferPool = iDataBufferPool;
        _dictionary = dictionary;
        _clientCts = CancellationTokenSource.CreateLinkedTokenSource(serverToken);
        _start = true;
        
        var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _key = key;
        _receiveQueue = [];
        _receiveBuffer = [];
        _clientType = PublicConst.IdentifyNone;//未验证；
        _hostInfo.RaiseClientConnEvent($"{_key}：客户端上线！【{currDateTime}】");
        _readTask = Task.Run(() => SafeHandleClientLoop(_clientCts.Token), _clientCts.Token);
    }
    #endregion
    private async Task SafeHandleClientLoop(CancellationToken token)
    {
        try
        {
            await HandleClientAsync(token);
        }
        catch (OperationCanceledException)
        {
            //主动取消监听，正常优雅关闭，不打错误日志
        }
        catch (Exception ex)
        {
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ParaSetupModules.RaiseAscDataProdEvent($"{_key}：接收循环顶层异常：{ex.Message}【{currDateTime}】");
        }
        finally
        {
            CloseClient();
        }
    }
    #region 单个客户端接收数据

    private async Task HandleClientAsync(CancellationToken token)
    {
        var stream = _client.GetStream();
        var data = new byte[PublicConst.ClientMaxLength];
        while (!token.IsCancellationRequested)
        {
            try
            {
                var bytesRead = await stream.ReadAsync(data, token);
                var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                if (bytesRead == 0)
                {
                    //client left;
                    _hostInfo.RaiseClientConnEvent($"{_key}：主动下线！【{currDateTime}】");
                    break;
                }
                var data00 = new byte[bytesRead];
                Array.Copy(data, data00, bytesRead);
                _receiveQueue.Enqueue(data00);
                _hostInfo.RaiseClientConnEvent($"{_key}：收到客户端数据！【{currDateTime}】");
            }
            catch (IOException)
            {
                var currDateTime = DateTime.Now.ToString("yyyy‑MM‑dd HH:mm:ss");
                _hostInfo.RaiseClientConnEvent($"{_key}：IO异常，强制下线！【{currDateTime}】");
                break;
            }
            catch (SocketException)
            {
                var currDateTime = DateTime.Now.ToString("yyyy‑MM‑dd HH:mm:ss");
                _hostInfo.RaiseClientConnEvent($"{_key}：Socket异常，强制下线！【{currDateTime}】");
                break;
            }
            catch (Exception ex)
            {
                var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                ParaSetupModules.RaiseAscDataProdEvent($"{_key}：异步接收异常 {ex.Message}，强制下线！【{currDateTime}】");                
                break;
            }
        }
    }
    #endregion
    
    #region 发送消息；
    public async Task<bool> SendMessage(byte[] content,int length)
    {
        if (length <= 0 || length > content.Length) return false;
        // 防御：TcpClient已经关闭
        if (_client is not { Connected: true } || !_start)
        {
            CloseClient();
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _hostInfo.RaiseClientConnEvent($"{_key}：发送消息，客户端连接已断开！【{currDateTime}】");
            return false;
        }
        // 异步锁，保证同一时刻只有一处进入写逻辑，解决并发WriteAsync错乱
        try
        {
            await _streamSemaphore.WaitAsync(_clientCts.Token);
        }
        catch (Exception)
        {
            CloseClient();
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _hostInfo.RaiseClientConnEvent($"{_key}：发送时会话取消！【{currDateTime}】");
            return false;
        }
        try
        {
            var ns = _client.GetStream();
            var offset = 0;
            var remaining = length;
            while (remaining > 0 && !_clientCts.Token.IsCancellationRequested)
            {
                var sendSize = Math.Min(MaxPacket, remaining);
                // 使用Memory<T> 高性能异步写入
                await ns.WriteAsync(content.AsMemory(offset, sendSize), _clientCts.Token);
                // 推进指针
                offset += sendSize;
                remaining -= sendSize;
            }
            return true;
        }
        catch (OperationCanceledException)
        {
            CloseClient();
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _hostInfo.RaiseClientConnEvent($"{_key}：发送消息会话取消！【{currDateTime}】");
            return false;
        }
        catch (IOException)
        {
            CloseClient();
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _hostInfo.RaiseClientConnEvent($"{_key}：发送消息IO异常，强制下线！【{currDateTime}】");
            return false;
        }
        catch (SocketException)
        {
            CloseClient();
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _hostInfo.RaiseClientConnEvent($"{_key}：发送消息Socket异常，强制下线！【{currDateTime}】");
            return false;
        }
        catch (Exception ex)
        {
            CloseClient();
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ParaSetupModules.RaiseAscDataProdEvent($"{_key}：发送消息异常，强制下线：{ex.Message}！【{currDateTime}】");                
            return false;
        }finally
        {
            _streamSemaphore.Release();
        }
    }
    #endregion

    public bool ParseDatas()
    {
        try
        {
            if (!_receiveQueue.TryDequeue(out var data))
            {
                return false;
            }
            _receiveBuffer.AddRange(data);
            while (ParsePacket()) ;
            return true;
        }
        catch (Exception ex)
        {
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ParaSetupModules.RaiseAscDataProdEvent($"{_key}：解析消息异常：{ex.Message}！【{currDateTime}】");                
            return false;
        }
    }
    private bool ParsePacket()
    {
        // 至少需要：包头2 + 长度1 + 包尾1 = 4字节
        if (_receiveBuffer.Count < 4)
            return false;
        // 1. 查找包头
        var headIndex = -1;
        for (var i = 0; i < _receiveBuffer.Count - 1; i++)
        {
            if (!(_receiveBuffer[i] == 0XEB && _receiveBuffer[i + 1] == 0XAA))
            {
                continue;
            }
            headIndex = i;
            break;
        }
        // 没找到包头，丢弃无用数据
        if (headIndex == -1)
        {
            _receiveBuffer.Clear();
            return false;
        }
        // 包头前的脏数据丢弃
        if (headIndex > 0)
        {
            _receiveBuffer.RemoveRange(0, headIndex);
        }
        // 再次判断最小长度
        if (_receiveBuffer.Count < 4)
            return false;
        // 2. 获取数据长度（第3字节）
        byte dataLen = _receiveBuffer[2];
        if (dataLen > PublicConst.ClientMaxLength)
        {
            //每包数据长度，超过最大，则丢弃这个包头，继续找下一个
            _receiveBuffer.RemoveRange(0, 2);
            return true;
        }
        var totalFrameLen = 2 + 1 + dataLen + 1; // 总帧长
        // 缓存不足一帧，等待下次数据
        if (_receiveBuffer.Count < totalFrameLen)
            return false;
        // 3. 校验包尾
        var tail = _receiveBuffer[totalFrameLen - 1];
        if (tail != 0XED)
        {
            // 包尾不对，丢弃这个包，继续找下一个
            _receiveBuffer.RemoveRange(0, 2);
            return true;
        }
        // 4. 提取完整一帧
        var frame = _receiveBuffer.GetRange(0, totalFrameLen).ToArray();
        // 5. 从缓存移除这一帧
        _receiveBuffer.RemoveRange(0, totalFrameLen);
        // 6. 抛出完整包事件
        var socketDataBlock00 = new SocketDataBlock
        {
            Content = frame,
            Length = totalFrameLen,
            Key = _key,
            BufferDataProdEvent = _hostInfo.GetBufferDataProdEvent()
        };
        //放入数据队列中；
        _iDataBufferPool.DataEnqueue(socketDataBlock00);
        return true;
    }
    #region 断开连接；
    public void CloseClient()
    {
        if (!_start)
            return;
        try
        {
            _clientCts.Cancel();
        }
        catch
        {
            //忽略；
        }
        try
        {
            _clientCts.Dispose();
        }
        catch
        {
            //忽略；
        }
        _dictionary.TryRemove(_key, out var _);
        try
        {
            _client.Close();
        }
        catch (Exception)
        {
            //忽略；
        }
        try
        {
            _client.Dispose();
        }
        catch (Exception)
        {
            //忽略；
        }
        _client = null!;
        try
        {
            _streamSemaphore.Dispose();
        }
        catch
        {
            //忽略；
        }
        _start = false;
    }
    #endregion
    public bool IsStart() { return _start;  }
    public byte GetClientType() { return _clientType; }
    public int GetHostBh() { return _hostInfo.Bh; }
    public void SetClientType(byte clientType) { _clientType = clientType; }
}