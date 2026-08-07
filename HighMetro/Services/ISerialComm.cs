using System;

namespace HighMetro.Services;

public interface ISerialComm
{
    bool Open();
    void SendMessage(byte[] message, int start, int length);
    bool IsOpen { get; }
    void Close();
    void Destory();
    EventHandler BufferDataProdEvent { get; set; }
    EventHandler MainThreadDataProdEvent { get; set; }
    EventHandler SourBufferDataProdEvent { get; set; }
    EventHandler ErrorBufferDataProdEvent { get; set; }
}