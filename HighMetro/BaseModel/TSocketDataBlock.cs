using System;
using HighMetro.Event;

namespace HighMetro.BaseModel;

public class SocketDataBlock
{
    public int Length { get ; set; }
    public byte[]? Content { get ; set ; }
    public int Value1 { get; set; }
    public int Value2 { get; set; }
    public int Value1Length { get; set; }
    public int Value2Length { get; set; }
    public string? Key { get; set; }
    public EventHandler? BufferDataProdEvent;
}