using System.Threading;
using System.Threading.Tasks;
using HighMetroServer.BaseModel;

namespace HighMetroServer.Services;

public interface IDataBufferPool
{
    //数据进入队列；
    void DataEnqueue(SocketDataBlock sockData);
    //数据离开队列；
    ValueTask<SocketDataBlock?> DataDequeueAsync(CancellationToken token);
    void Complete();
}