using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using HighMetroServer.BaseModel;
using HighMetroServer.Models;

namespace HighMetroServer.Services;

public class CommSerialImpl(int threadCount, SerialCommInfo serialCommInfo)
{
    private readonly SerialPort _serialPort = new()
    {
        PortName = serialCommInfo.CommName,
        BaudRate = serialCommInfo.BaudRate,
        Parity = (Parity)serialCommInfo.Parity,
        DataBits = serialCommInfo.DataBits,
        StopBits = (StopBits)serialCommInfo.StopBits,
        ReadTimeout = 500,
        WriteTimeout = 500
    };
    private DataBufferPoolImpl? _iDataBufferPool;
    private readonly List<IGetBufferData> _getBufferDataImplList=[];
    private readonly Queue<byte> _receiveBuffer = [];
    private readonly ConcurrentQueue<byte[]> _receiveQueue=[];
    private bool _start;
    private const byte PacketHead1 = 0xEB;
    private const byte PacketHead2 = 0xAA;
    private const byte PacketTail = 0xED;
    private const int TaskWaitTimeoutMs = 500;
    private int _receiveTotalCount;
    private int _parseTotalCount;
    private int _receiveTotalBytes;
    private int _parseTotalBytes;
    private Task? _parseBackgroundTask;
    private CancellationTokenSource? _parseCts;
    private SemaphoreSlim? _semaphoreSlim;

    public bool Open()
    {
        if (_start)
        {
            return true;
        }
        try
        {
            //open the serial port;
            _serialPort.Open();
            _serialPort.DataReceived += OnSerialDataReceived;
            _receiveQueue.Clear();
            _receiveBuffer.Clear();
            //接收生产者串口数据；
            _iDataBufferPool = new DataBufferPoolImpl();
            //数据消费者
            _getBufferDataImplList.Clear();
            for (var i = 0; i < threadCount; i++)
            {
                _getBufferDataImplList.Add(new GetBufferDataImpl(_iDataBufferPool));
            }
            _parseCts = new CancellationTokenSource();
            //启动1个线程，进行数据包的拆分或合并；
            _parseBackgroundTask = Task.Run(() => ParseDataLoop(_parseCts.Token), _parseCts.Token);
            ResetStatistics();
            _semaphoreSlim = new SemaphoreSlim(0);
            _start = true;
            return true;
        }
        catch (Exception ex)
        {
            _start = false;
            ClearResource();
            var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ParaSetupModules.RaiseAscDataProdEvent($"启动串口失败！{ex.Message}【{currentTime}】");
            return false;
        }
    }
    private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (sender is not SerialPort sp || !sp.IsOpen)
                return;
            var readBytesCount = sp.BytesToRead;
            if (readBytesCount <= 0)
                return;
            if (readBytesCount > PublicConst.SockDataMaxLength)
            {
                var currTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                ParaSetupModules.RaiseAscDataProdEvent($"串口接收数据包超长，长度{readBytesCount}已丢弃！【{currTime}】");
                return;
            }
            var validData = new byte[readBytesCount];
            var actualRead = sp.Read(validData, 0, readBytesCount);
            if (actualRead <= 0)
            {
                return;
            }
            if (actualRead != validData.Length)
            {
                var temp = new byte[actualRead];
                Array.Copy(validData, temp, actualRead);
                validData = temp;
            }
            Interlocked.Increment(ref _receiveTotalCount);
            Interlocked.Add(ref _receiveTotalBytes, actualRead);
            _receiveQueue.Enqueue(validData);
            _semaphoreSlim!.Release();
        }
        catch (Exception ex)
        {
            var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ParaSetupModules.RaiseAscDataProdEvent($"接收串口数据异常！{ex.Message}【{currentTime}】");
        }
    }
    private async Task ParseDataLoop(CancellationToken token)
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
            ParaSetupModules.RaiseAscDataProdEvent($"串口解析循环顶层异常：{ex.Message}【{currDateTime}】");
        }
    }
    #region 解析串口数据；
    private async Task ParseData(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await _semaphoreSlim!.WaitAsync(token);
                if (!_receiveQueue.TryDequeue(out var data))
                    continue;
                foreach (var b in data)
                {
                    _receiveBuffer.Enqueue(b);
                }
                while (!token.IsCancellationRequested
                       && TryParseOnePacket()) ;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                ParaSetupModules.RaiseAscDataProdEvent($"解析串口数据异常：{ex.Message}【{currDateTime}】");
            }
        }
    }
    #endregion
    private bool TryParseOnePacket()
    {
        // 至少需要：包头2 + 长度1 + 包尾1 = 4字节
        if (_receiveBuffer.Count < 4)
            return false;
        var byteCopy = _receiveBuffer.ToArray();
        // 1. 查找包头
        var startPosition = 0;
        var headIndex = FindPacketHeaderIndex(byteCopy);
        // 没找到包头，丢弃无用数据
        if (headIndex == -1)
        {
            _receiveBuffer.Clear();
            return false;
        }
        startPosition += headIndex;
        // 包头前的脏数据丢弃
        for (var i = 0; i < headIndex; i++)
        {
            _receiveBuffer.Dequeue();
        }
        // 再次判断最小长度
        if (_receiveBuffer.Count < 4)
            return false;
        startPosition += 2;
        // 2. 获取数据长度（第3字节）[2]
        var dataLen = byteCopy[startPosition];
        if (dataLen > PublicConst.PerSockDataMaxLength)
        {
            //每包数据长度，超过最大，则丢弃这个包头，继续找下一个
            _receiveBuffer.Dequeue();
            _receiveBuffer.Dequeue();
            return true;
        }
        var totalFrameLength = 2 + 1 + dataLen + 1; // 总帧长
        // 缓存不足一帧，等待下次数据
        if (_receiveBuffer.Count < totalFrameLength)
            return false;
        startPosition += (totalFrameLength - 3);
        var tailByte = byteCopy[startPosition];
        if (tailByte != PacketTail)
        {
            _receiveBuffer.Dequeue();
            _receiveBuffer.Dequeue();
            return true;
        }
        // 截取完整帧
        var frame = new byte[totalFrameLength];
        for (var i = 0; i < totalFrameLength; i++)
        {
            frame[i] = _receiveBuffer.Dequeue();
        }
        // 统计计数原子更新
        Interlocked.Increment(ref _parseTotalCount);
        Interlocked.Add(ref _parseTotalBytes, totalFrameLength);
        // 6. 抛出完整包事件
        var socketDataBlock00 = new SocketDataBlock
        {
            Content = frame,
            Length = totalFrameLength,
            Value1 = _receiveTotalCount,
            Value2 = _parseTotalCount,
            Value1Length = _receiveTotalBytes,
            Value2Length = _parseTotalBytes,
            BufferDataProdEvent = serialCommInfo.GetBufferDataProdEvent()
        };
        //放入数据队列中；
        _iDataBufferPool!.DataEnqueue(socketDataBlock00);
        return true;
    }
    private int FindPacketHeaderIndex(byte[] buffer)
    {
        for (var i = 0; i < buffer.Length - 1; i++)
        {
            if (buffer[i] == PacketHead1 && buffer[i + 1] == PacketHead2)
            {
                return i;
            }
        }
        return -1;
    }
    private void ResetStatistics()
    {
        _receiveTotalCount = 0;
        _parseTotalCount = 0;
        _receiveTotalBytes = 0;
        _parseTotalBytes = 0;
    }
    public void SendMessage(byte[] message, int start, int length)
    {
        if (!_start)
            return;
        try
        {
            _serialPort.Write(message, start, length);
        }
        catch (Exception ex)
        {
            var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ParaSetupModules.RaiseAscDataProdEvent($"发送串口数据异常！{ex.Message}【{currentTime}】");
        }
    }
    public bool TestComm()
    {
        try
        {
            _serialPort.Open();
            try
            {
                _serialPort.Close();
            }catch(Exception)
            {
                //忽略；
            }
            try
            {
                _serialPort.Dispose();
            }catch(Exception)
            {
                //忽略；
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    public void Close()
    {
        if (!_start)
            return;
        ClearResource();
    }
    private void ClearResource()
    {
        _serialPort.DataReceived -= OnSerialDataReceived;
        try
        {
            _parseCts?.Cancel();;
        }
        catch
        {
            //忽略；
        }
        try
        {
            _parseCts?.Dispose();
        }
        catch
        {
            //忽略；
        }
        try
        {
            _parseBackgroundTask?.Wait(TaskWaitTimeoutMs);
        }
        catch (Exception)
        {
            //忽略;
        }
        _parseBackgroundTask = null;
        _parseCts = null;
        try
        {
            _serialPort.Close();
        }
        catch (Exception)
        {
            //忽略异常；
        }
        try
        {
            _serialPort.Dispose();
        }
        catch (Exception)
        {
            //忽略异常；
        }
        foreach (var item in _getBufferDataImplList)
        {
            item.DisConnect();
        }
        _getBufferDataImplList.Clear();
        try
        {
            _semaphoreSlim?.Dispose();
        }
        catch (Exception)
        {
            //忽略异常；
        }
        _semaphoreSlim = null;
        _start = false;
    }
}