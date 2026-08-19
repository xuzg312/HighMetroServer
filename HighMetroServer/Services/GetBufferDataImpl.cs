using System;
using System.Threading;
using System.Threading.Tasks;
using HighMetroServer.Event;
using HighMetroServer.Models;

namespace HighMetroServer.Services;

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
        _workerTask = Task.Run(() => GetData(_cts.Token), _cts.Token);
    }
    #endregion

    private async Task GetData(CancellationToken token)
    {
        try
        {
            await GetBufferSocketData(token);
        }
        catch (OperationCanceledException)
        {
            //主动取消监听，正常优雅关闭，不打错误日志
        }
        catch (Exception ex)
        {
            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ParaSetupModules.RaiseAscDataProdEvent($"数据池获取数据顶层异常：{ex.Message}【{currDateTime}】");
        }
        finally
        {
            DisConnect();
        }
    }

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
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                ParaSetupModules.RaiseAscDataProdEvent($"消息池中监听消息异常：{ex.Message}【{currDateTime}】");
            }
        }
    }
    #endregion

    #region IGetBufferData 成员
    public void DisConnect()
    {
        if (_disposed)
            return;
        try
        {
            _cts?.Cancel();
        }
        catch (Exception)
        {
            //忽略;
        }
        try
        {
            _workerTask?.Wait(500);
        }
        catch (Exception)
        {
            //忽略;
        }
        _workerTask = null;
        try
        {
            _cts?.Dispose();
        }
        catch (Exception)
        {
            //忽略;
        }
        _cts = null;
        _disposed = true;
    }
    #endregion
}