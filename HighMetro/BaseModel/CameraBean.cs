using System;

namespace HighMetro.BaseModel;

public class CameraBean
{
    public int HostBh { get ; set ; }
    public string Door { get; set; } = string.Empty;
    public string Type { get; set ; }= string.Empty;
    public DateTime DateTime { get ; set ; }
    public string Upload { get ; set; }= string.Empty;
    public DateTime? UploadDateTime { get; set ; }
    public string FilePath { get ; set ; }= string.Empty;
    public string Message { get ; set ; }= string.Empty;
    public uint LastErr { get ; set ; }
    public int Id { get; set; }
    public int Serial { get; set ; }
    public int Bh { get ; set; }
}