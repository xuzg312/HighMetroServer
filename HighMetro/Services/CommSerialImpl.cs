using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HighMetro.BaseModel;

namespace HighMetro.Services;

public class CommSerialImpl(int threadCount, SerialCommInfo serialCommInfo)
{
    private readonly SerialPort _serialPort=new SerialPort()
    {
        PortName = serialCommInfo.CommName,
        BaudRate = serialCommInfo.BaudRate,
        Parity = (Parity)serialCommInfo.Parity,
        DataBits = serialCommInfo.DataBits,
        StopBits = (StopBits)serialCommInfo.StopBits,
        Encoding = Encoding.ASCII,
        ReadTimeout = 500,
        WriteTimeout = 500
    };
    private DataBufferPoolImpl? _iDataBufferPool;
    private readonly List<IGetBufferData> _getBufferDataImplList=[];
    private readonly List<byte> _receiveBuffer = [];
    private readonly ConcurrentQueue<byte[]> _receiveQueue=[];
    private bool _start;
    private const byte PacketHead1 = 0xEB;
    private const byte PacketHead2 = 0xAA;
    private const byte PacketTail = 0xED;
    private const int ParseLoopDelayMs = 100;
    private const int TaskWaitTimeoutMs = 500;
    private int _receiveTotalCount;
    private int _parseTotalCount;
    private int _receiveTotalBytes;
    private int _parseTotalBytes;
    private Task? _parseBackgroundTask;
    private CancellationTokenSource? _parseCts;
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
            _parseBackgroundTask = Task.Run(() => ParseData(_parseCts.Token));
            ResetStatistics();
            return true;
        }
        catch (Exception)
        {
            _start = false;
            ClearResource();
            serialCommInfo.RaiseAscDataProdEvent("启动串口失败！");
            return false;
        }
    }
    private void OnSerialDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            var readBytesCount = _serialPort.BytesToRead;
            if (readBytesCount is < 1 or > PublicConst.SockDataMaxLength)
                return;
            var data = new byte[readBytesCount];
            var actualRead = _serialPort.Read(data, 0, readBytesCount);
            if (actualRead > 0)
            {
                Interlocked.Increment(ref _receiveTotalCount);
                Interlocked.Add(ref _receiveTotalBytes, actualRead);
                _receiveQueue.Enqueue(data);
            }
        }
        catch (Exception ex)
        {
            serialCommInfo.RaiseAscDataProdEvent("接收串口数据异常!"+ex.Message);
        }
    }
    #region 解析串口数据；
    private async Task ParseData(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_receiveQueue.TryDequeue(out var data) && data.Length > 0)
            {
                _receiveBuffer.AddRange(data);
                while (ParsePacket()) ;
            }
            else
            {
                // 没数据时稍作等待，降低CPU占用
                await Task.Delay(ParseLoopDelayMs, token);
            }
        }
    }
    #endregion
    private bool ParsePacket()
    {
        // 至少需要：包头2 + 长度1 + 包尾1 = 4字节
        if (_receiveBuffer.Count < 4)
            return false;

        // 1. 查找包头
        var headIndex = FindPacketHeaderIndex();
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
        var dataLen = _receiveBuffer[2];
        if (dataLen > PublicConst.PerSockDataMaxLength)
        {
            //每包数据长度，超过最大，则丢弃这个包头，继续找下一个
            _receiveBuffer.RemoveRange(0, 2);
            return true;
        }
        var totalFrameLength = 2 + 1 + dataLen + 1; // 总帧长

        // 缓存不足一帧，等待下次数据
        if (_receiveBuffer.Count < totalFrameLength)
            return false;

        // 3. 校验包尾
        var frameTail = _receiveBuffer[totalFrameLength - 1];
        if (frameTail != PacketTail)
        {
            // 包尾不对，丢弃这个包，继续找下一个
            _receiveBuffer.RemoveRange(0, 2);
            return true;
        }

        // 4. 提取完整一帧
        var frameBytes = _receiveBuffer.GetRange(0, totalFrameLength).ToArray();

        // 5. 从缓存移除这一帧
        _receiveBuffer.RemoveRange(0, totalFrameLength);

        // 统计计数原子更新
        Interlocked.Increment(ref _parseTotalCount);
        Interlocked.Add(ref _parseTotalBytes, totalFrameLength);

        
        // 6. 抛出完整包事件
        var socketDataBlock00 = new SocketDataBlock
        {
            Content = frameBytes,
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
            serialCommInfo.RaiseAscDataProdEvent("发送串口数据异常!"+ex.Message);
        }
    }
    private int FindPacketHeaderIndex()
    {
        for (var i = 0; i < _receiveBuffer.Count - 1; i++)
        {
            if (_receiveBuffer[i] == PacketHead1 && _receiveBuffer[i + 1] == PacketHead2)
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
    public bool IsOpen { get; private set; }
    public void Close()
    {
        if (!_start)
            return;
        ClearResource();
    }
    public void ClearResource()
    {
        _serialPort.DataReceived -= OnSerialDataReceived;
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
        var length = _getBufferDataImplList.Count;
        for (var i = 0; i < length; i++)
        {
            _getBufferDataImplList[i].DisConnect();
        }
        try
        {
            _parseCts?.Cancel();;
        }
        catch
        {
            //忽略；
        }
        _parseBackgroundTask?.Wait(TaskWaitTimeoutMs);
        _parseCts = null;
        _start = false;
    }
}