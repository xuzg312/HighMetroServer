namespace HighMetroServer.BaseModel;

public class SocketDataBlock
{
    public int Length { get ; set; }
    public byte[]? Content { get ; set ; }
    public long Value1 { get; set; }
    public long Value2 { get; set; }
    public long Value1Length { get; set; }
    public long Value2Length { get; set; }
    public string? Key { get; set; }
    public byte MessageType { get; set; }

}