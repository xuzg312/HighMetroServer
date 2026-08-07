using HighMetro.BaseModel;

namespace HighMetro.Services;

public interface IDataBufferPool
{
    //数据进入队列；
    void DataEnqueue(SocketDataBlock sockData);
    //数据离开队列；
    SocketDataBlock? DataDequeue();
}