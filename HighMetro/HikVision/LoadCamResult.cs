namespace HighMetro.HikVision;

public class LoadCamResult
{
    public string Code { get; set; } = string.Empty;
    public int Value{ get; set; }
    public string Message { get; set; } = string.Empty;
    public int Tag{ get; set; }
    public byte[] ImageData { get; set; } = [];
}