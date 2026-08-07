using System;
using System.Threading;
using System.Threading.Tasks;
using HighMetro.BaseModel;
using HighMetro.ClassLib;
using HighMetro.Event;

namespace HighMetro.Services;

public class GetBufferDataImpl : IGetBufferData
{
    #region 私有数据；
    private readonly IDataBufferPool _iDataBufferPool;
    private Task? _workerTask;
    private CancellationTokenSource? _cts;
    private bool _disposed;
    #endregion

    #region 构造函数；
    public GetBufferDataImpl(IDataBufferPool dataBufferPool)
    {
        _iDataBufferPool = dataBufferPool;
        _cts = new CancellationTokenSource();
        _workerTask = Task.Run(() => GetBufferSocketData(_cts.Token));
    }
    #endregion

    #region 获取数据池中数据；
    private async Task GetBufferSocketData(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var socketDataBlock = _iDataBufferPool.DataDequeue();
                if (socketDataBlock != null)
                {
                    //解析数据；
                    socketDataBlock.BufferDataProdEvent?.Invoke(null, new SocketDataEventArgs(socketDataBlock));
                }
                else
                {
                    await Task.Delay(100, token);
                }
            }catch (OperationCanceledException)
            {
                // 正常取消，直接退出循环
                break;
            }
            catch (Exception ex)
            {
                WriteErrorToLog.WriteToErrorLog(ex, "解析消息异常，错误原因:GetBufferDataImpl.GetBufferSocketData");
            }
        }
    }
    #endregion

    #region IGetBufferData 成员
    public void DisConnect()
    {
        if (_disposed)
            return;
        _disposed = true;
        try
        {
            _cts?.Cancel();
        }
        catch (Exception ex)
        {
            WriteErrorToLog.WriteToErrorLog(ex, "GetBufferDataImpl.DisConnect");
        }
        try
        {
            _workerTask?.Wait(500);
        }
        catch (Exception ex)
        {
            WriteErrorToLog.WriteToErrorLog(ex, "GetBufferDataImpl.DisConnect");
        }
        try
        {
            _cts?.Dispose();
        }
        catch (Exception ex)
        {
            WriteErrorToLog.WriteToErrorLog(ex, "GetBufferDataImpl.DisConnect");
        }
        _cts = null;
        _workerTask = null;
    }
    #endregion
    
}