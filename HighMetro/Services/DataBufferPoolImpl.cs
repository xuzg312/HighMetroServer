using System.Collections.Concurrent;
using HighMetro.BaseModel;

namespace HighMetro.Services;

public class DataBufferPoolImpl : IDataBufferPool
{
    #region 私有数据；
    private readonly ConcurrentQueue<SocketDataBlock> _receiveQueue = new ConcurrentQueue<SocketDataBlock>();
    #endregion

    #region 数据进入队列；
    public void DataEnqueue(SocketDataBlock sockData)
    {
        _receiveQueue.Enqueue(sockData);
    }
    #endregion

    #region 数据离开队列；
    public SocketDataBlock? DataDequeue()
    {
        return _receiveQueue.TryDequeue(out var data) ? data : null;
    }
    #endregion
}