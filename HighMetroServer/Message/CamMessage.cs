using HighMetroServer.BaseModel;

namespace HighMetroServer.Message;

public class CamMessage(byte type,CameraBean cameraBean)
{
    public byte Type { get; private set; } = type;
    public CameraBean CameraBean { get; private set; } = cameraBean;
}