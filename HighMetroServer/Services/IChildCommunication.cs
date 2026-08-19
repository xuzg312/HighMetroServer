using System.Threading.Tasks;

namespace HighMetroServer.Services;

public interface IChildCommunication
{
    Task<bool> SendMessage(byte[] content,int length);
    void CloseClient();
    bool IsStart();
    bool ParseDatas();
    byte GetClientType();
    int GetHostBh();
    void SetClientType(byte clientType);
}