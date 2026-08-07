using System;
using System.Linq;
using System.Net;

namespace HighMetro.ClassLib;

public class PublicUntil
{
    public void GetShort(ushort intPort, byte[] commbuffer, int iPosition)
    {
        short netValue = IPAddress.HostToNetworkOrder((short)intPort);
        byte[] bytes = BitConverter.GetBytes(netValue);
        Array.Copy(bytes, 0, commbuffer, iPosition, 2);
    }
    public void GetInt(int intdata, byte[] commbuffer, int iPosition)
    {
        Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(intdata)), 0, commbuffer, iPosition, 4);
    }
    public ushort GetUshort(byte[] databuffer, int iPosition)
    {
        var revertByteList = new byte[2];
        Array.Copy(databuffer, iPosition, revertByteList, 0, 2);
        revertByteList = revertByteList.Reverse().ToArray();
        return BitConverter.ToUInt16(revertByteList, 0);
    }
    public short GetShort(byte[] databuffer, int iPosition)
    {
        var revertByteList = new byte[2];
        Array.Copy(databuffer, iPosition, revertByteList, 0, 2);
        revertByteList = revertByteList.Reverse().ToArray();
        return BitConverter.ToInt16(revertByteList, 0);
    }
    public uint GetUint(byte[] databuffer, int iPosition)
    {
        var revertByteList = new byte[4];
        Array.Copy(databuffer, iPosition, revertByteList, 0, 4);
        revertByteList = revertByteList.Reverse().ToArray();
        return BitConverter.ToUInt32(revertByteList, 0);
    }
    public int GetInt(byte[] databuffer, int iPosition)
    {
        var revertByteList = new byte[4];
        Array.Copy(databuffer, iPosition, revertByteList, 0, 4);
        revertByteList = revertByteList.Reverse().ToArray();
        return BitConverter.ToInt32(revertByteList, 0);
    }
}