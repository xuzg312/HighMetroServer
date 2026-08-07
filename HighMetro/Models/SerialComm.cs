namespace HighMetro.Models;

public class SerialComm
{
    public int Bh { get; set ; }
    public int Hostbh { get; set ; }
    public string Name { get ; set ; }
    public string CommName { get; set; }
    public int BaudRate { get; set; }
    public int Parity { get; set; }
    public int DataBits { get; set; }
    public int StopBits { get ; set ; }
    public int Id { get; set ; }
    public int Sign { get; set ; }
    public string CommType { get; set ; }
    public bool Open { get; set; }
}