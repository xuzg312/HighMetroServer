namespace HighMetroServer.HikVision;

public class LoadCamResult
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public byte[] ImageData { get; set; } = [];
}