using HighMetroServer.BaseModel;

namespace HighMetroServer.Message;

public class TcpMessage(SocketDataBlock socketDataBlock)
{
    public SocketDataBlock SocketDataBlock { get; private set; } = socketDataBlock;
}