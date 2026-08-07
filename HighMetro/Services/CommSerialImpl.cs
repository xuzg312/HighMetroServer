using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using HighMetro.BaseModel;
using HighMetro.ClassLib;
using HighMetro.Models;

namespace HighMetro.Services;

public class CommSerialImpl : ISerialComm
{
    #region 私有数据；
    private SerialPort _serialPort;
    private IDataBufferPool _iDataBufferPool;
    private EventHandler _bufferDataProdEvent;
    private EventHandler _mainThreadDataProdEvent;
    private EventHandler _sourBufferDataProdEvent;
    private EventHandler _errorBufferDataProdEvent;
    private ArrayList _getBufferDataImplList;
    private List<byte> _recvBuffer = new List<byte>();
    private readonly ConcurrentQueue<byte[]> _receiveQueue;
    private SerialComm _serialComm;
    private Thread _readThread;
    private int _value1 = 0;
    private int _value2 = 0;
    private int _value1Length = 0;
    private int _value2Length = 0;
    #endregion

    #region 构造函数；
    public CommSerialImpl(int threadCount, SerialComm serialComm)
    {
        _serialComm = serialComm;
        //设置串口；
        _serialPort = new SerialPort();
        _serialPort.Encoding = System.Text.Encoding.Unicode;
        _serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(DataReceived);
        //接收生产者串口数据；
        _iDataBufferPool = new DataBufferPoolImpl();
        //数据消费者
        _getBufferDataImplList = new ArrayList();
        for (int i = 0; i < threadCount; i++)
        {
            _getBufferDataImplList.Add(new GetBufferDataImpl(_iDataBufferPool));
        }
        //启动1个线程，进行数据包的拆分或合并；
        _receiveQueue = new ConcurrentQueue<byte[]>();
        _readThread = new Thread(new ThreadStart(parseCommDatas));
        _readThread.Start();
    }
    #endregion
    public bool Open()
    {
        try
        {
            if (!_serialPort.IsOpen)
            {
                //configure the various parameters of the serial port;
                _serialPort.PortName = _serialComm.CommName;
                _serialPort.BaudRate = _serialComm.BaudRate;
                _serialPort.Parity = (System.IO.Ports.Parity)_serialComm.Parity;
                _serialPort.DataBits = _serialComm.DataBits;
                _serialPort.StopBits = (System.IO.Ports.StopBits)_serialComm.StopBits;
                //open the serial port;
                _serialPort.Open();
                _value1 = 0;
                _value2 = 0;
                _value1Length = 0;
                _value2Length = 0;
            }
            return true;
        }
        catch (Exception ex)
        {
            WriteErrorToLog.WriteToErrorLog(ex, "SerialCommImpl.open");
            showError("打开串口失败！");
        }
        return false;
    }
    private void DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
    {
        try
        {
            int byteToRead = _serialPort.BytesToRead;
            if (byteToRead >= 1 && byteToRead <= PublicConst.SockDataMaxLength)
            {
                byte[] data = new byte[byteToRead];
                byteToRead = _serialPort.Read(data, 0, byteToRead);
                _value1++;
                _value1Length += byteToRead;
                _receiveQueue.Enqueue(data);
            }
        }
        catch (Exception ex)
        {
            WriteErrorToLog.WriteToErrorLog(ex, "接收串口数据异常,错误原因:SerialCommImpl.DataReceived");
            showError("接收串口数据异常!");
        }
    }
    #region 解析串口数据；
    private void parseCommDatas()
    {
        while (true)
        {
            if (_receiveQueue.TryDequeue(out byte[] data))
            {
                _recvBuffer.AddRange(data);
                while (ParsePacket()) ;
            }
            else
            {
                // 没数据时稍作等待，降低CPU占用
                Thread.Sleep(100);
            }
        }
    }
    #endregion
    /// <summary>
    /// 核心分包逻辑
    /// </summary>
    /// <returns>true=解析到一帧，继续循环；false=退出</returns>
    private bool ParsePacket()
    {
        // 至少需要：包头2 + 长度1 + 包尾1 = 4字节
        if (_recvBuffer.Count < 4)
            return false;

        // 1. 查找包头
        int headIndex = -1;
        for (int i = 0; i < _recvBuffer.Count - 1; i++)
        {
            if (_recvBuffer[i] == 0XEB && _recvBuffer[i + 1] == 0XAA)
            {
                headIndex = i;
                break;
            }
        }
        // 没找到包头，丢弃无用数据
        if (headIndex == -1)
        {
            _recvBuffer.Clear();
            return false;
        }

        // 包头前的脏数据丢弃
        if (headIndex > 0)
        {
            _recvBuffer.RemoveRange(0, headIndex);
        }

        // 再次判断最小长度
        if (_recvBuffer.Count < 4)
            return false;

        // 2. 获取数据长度（第3字节）
        byte dataLen = _recvBuffer[2];
        if (dataLen > PublicConst.PerSockDataMaxLength)
        {
            //每包数据长度，超过最大，则丢弃这个包头，继续找下一个
            _recvBuffer.RemoveRange(0, 1);
            return true;
        }
        int totalFrameLen = 2 + 1 + dataLen + 1; // 总帧长

        // 缓存不足一帧，等待下次数据
        if (_recvBuffer.Count < totalFrameLen)
            return false;

        // 3. 校验包尾
        byte tail = _recvBuffer[totalFrameLen - 1];
        if (tail != 0XED)
        {
            // 包尾不对，丢弃这个包，继续找下一个
            _recvBuffer.RemoveRange(0, 1);
            return true;
        }

        // 4. 提取完整一帧
        byte[] frame = _recvBuffer.GetRange(0, totalFrameLen).ToArray();

        // 5. 从缓存移除这一帧
        _recvBuffer.RemoveRange(0, totalFrameLen);

        _value2++;
        _value2Length += totalFrameLen;
        
        // 6. 抛出完整包事件
        SocketDataBlock socketDataBlock00 = new SocketDataBlock();
        socketDataBlock00.Content = frame;
        socketDataBlock00.Length = totalFrameLen;
        socketDataBlock00.Value1 = _value1;
        socketDataBlock00.Value2 = _value2;
        socketDataBlock00.Value1Length = _value1Length;
        socketDataBlock00.Value2Length = _value2Length;
        socketDataBlock00.BufferDataProdEvent = BufferDataProdEvent;
        //放入数据队列中；
        _iDataBufferPool.DataEnqueue(socketDataBlock00);

        return true;
    }
    public void Close()
    {
        try
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }
        catch (Exception ex)
        {
            WriteErrorToLog.WriteToErrorLog(ex, "SerialCommImpl.Close");
            showError("关闭串口失败！");
        }
    }
    public void Destory()
    {
        try
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }
        catch (Exception ex)
        {
            WriteErrorToLog.WriteToErrorLog(ex, "SerialCommImpl.Close");
        }
        int length = _getBufferDataImplList.Count;
        for (int i = 0; i < length; i++)
        {
            ((IGetBufferData)_getBufferDataImplList[i]).DisConnect();
        }
        try
        {
            _readThread.Abort();
        }
        catch (Exception)
        {
        }
    }
    public void SendMessage(byte[] message, int start, int length)
    {
        if (_serialPort.IsOpen)
        {
            try
            {
                _serialPort.Write(message, start, length);
            }
            catch (Exception ex)
            {
                WriteErrorToLog.WriteToErrorLog(ex, "发送串口数据异常,错误原因:SerialCommImpl.SendMessage");
                showError("发送串口数据异常!");
            }
        }
    }
    public bool IsOpen
    {
        get
        {
            return _serialPort.IsOpen;
        }
    }
    private void showError(string errorMessage)
    {
        if (_errorBufferDataProdEvent != null)
        {
            byte[] buffer00 = Encoding.Default.GetBytes(errorMessage);
            SocketDataBlock socketDataBlock00 = new SocketDataBlock();
            socketDataBlock00.Content = buffer00;
            socketDataBlock00.Length = buffer00.Length;
            _errorBufferDataProdEvent(socketDataBlock00,null);
        }
    }
    public EventHandler BufferDataProdEvent { get => _bufferDataProdEvent; set => _bufferDataProdEvent = value; }
    public EventHandler MainThreadDataProdEvent { get => _mainThreadDataProdEvent; set => _mainThreadDataProdEvent = value; }
    public EventHandler SourBufferDataProdEvent { get => _sourBufferDataProdEvent; set => _sourBufferDataProdEvent = value; }
    public EventHandler ErrorBufferDataProdEvent { get => _errorBufferDataProdEvent; set => _errorBufferDataProdEvent = value; }
}