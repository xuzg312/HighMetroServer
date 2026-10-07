using System;
using System.Threading;
using System.Threading.Tasks;
using HighMetroServer.BaseModel;
using HighMetroServer.Models;

namespace HighMetroServer.Services;

public class GetBufferDataImpl : IGetBufferData
{
    #region 私有数据；
    private readonly IDataBufferPool _iDataBufferPool;
    private Task? _workerTask;
    private CancellationTokenSource? _cts;
    private int _disposed;
    #endregion

    #region 构造函数；
    public GetBufferDataImpl(IDataBufferPool dataBufferPool, byte messageType)
    {
        _iDataBufferPool = dataBufferPool;
        _cts = new CancellationTokenSource();
        _workerTask = Task.Run(() => ConsumeAsync(_cts.Token));
    }
    #endregion
    #region 获取数据池中数据；
    private async Task  ConsumeAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var socketDataBlock = await _iDataBufferPool.DataDequeueAsync(token);
                if (socketDataBlock == null)
                    break;  
                try
                {
                    //解析数据；
                    switch (socketDataBlock.MessageType)
                    {
                        case PublicConst.TcpMessage:
                            ParaSetupModules.RaiseTcpServerBufferDataProdEvent(socketDataBlock);
                            break;
                        case PublicConst.CommMessage:
                            ParaSetupModules.RaiseCommBufferDataProdEvent(socketDataBlock);
                            break;
                        default:
                            var currDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            ParaSetupModules.RaiseAscDataProdEvent($"待处理的数据类型未定义：【{socketDataBlock.MessageType}】【{currDateTime}】");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    ParaSetupModules.RaiseAscDataProdEvent($"单条消息处理异常：{ex.Message}【{now}】");
                }
            }
            catch (OperationCanceledException)
            {
                break;
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
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;
        try
        {
            _cts?.Cancel();
        }
        catch (Exception)
        {
            //忽略;
        }
        _iDataBufferPool.Complete(); 
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
    }
    #endregion
}