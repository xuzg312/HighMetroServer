namespace HighMetro.BaseModel;

public class SerialCommInfo
{
    public int Bh { get; set; }
    public int HostBh { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CommName { get; set; } = string.Empty;
    public int BaudRate { get; set; }
    public int Parity { get; set; }
    public int DataBits { get; set; }
    public int StopBits { get; set; }
    public int Id { get; set; }
    public int Sign { get; set; }
    public string CommType { get; set; } = string.Empty;
    public bool Open { get; set; }
}