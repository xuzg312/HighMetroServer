using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using HighMetroServer.BaseModel;

namespace HighMetroServer.Services;

public class DataBufferPoolImpl : IDataBufferPool
{
    #region 私有数据；
    private readonly Channel<SocketDataBlock> _channel;
    #endregion
    public DataBufferPoolImpl()
    {
        _channel = Channel.CreateBounded<SocketDataBlock>(
            new BoundedChannelOptions(PublicConst.DataBufferPoolMaxLength)
            {
                SingleReader = true,                          // 单消费者 → 保序
                SingleWriter = false,                         // 多接收线程可入队
                FullMode = BoundedChannelFullMode.DropOldest  // 满了丢最旧，保证生产者不阻塞
            });
    }
    #region 数据进入队列；
    public void DataEnqueue(SocketDataBlock sockData)
    {
        _channel.Writer.TryWrite(sockData);
    }
    #endregion

    #region 数据离开队列；
    public async ValueTask<SocketDataBlock?> DataDequeueAsync(CancellationToken token)
    {
        try
        {
            return await _channel.Reader.ReadAsync(token);
        }
        catch (ChannelClosedException)
        {
            return null;
        }
    }
    #endregion
    #region 关闭队列；
    public void Complete()
    {
        _channel.Writer.TryComplete();
    }
    #endregion
}