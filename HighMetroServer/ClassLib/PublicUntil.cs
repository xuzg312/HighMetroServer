using System;
using System.Linq;
using System.Net;

namespace HighMetroServer.ClassLib;

public class PublicUntil
{
    public void GetUShort(int intData, byte[] commBuffer, int iPosition)
    {
        var netValue = IPAddress.HostToNetworkOrder(intData);
        var bytes = BitConverter.GetBytes(netValue);
        Array.Copy(bytes, 2, commBuffer, iPosition, 2);
    }
    public void GetInt(int intData, byte[] commBuffer, int iPosition)
    {
        Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(intData)), 0, commBuffer, iPosition, 4);
    }
    public ushort GetUshort(byte[] dataBuffer, int iPosition)
    {
        var revertByteList = new byte[2];
        Array.Copy(dataBuffer, iPosition, revertByteList, 0, 2);
        revertByteList = revertByteList.Reverse().ToArray();
        return BitConverter.ToUInt16(revertByteList, 0);
    }
    public short GetShort(byte[] dataBuffer, int iPosition)
    {
        var revertByteList = new byte[2];
        Array.Copy(dataBuffer, iPosition, revertByteList, 0, 2);
        revertByteList = revertByteList.Reverse().ToArray();
        return BitConverter.ToInt16(revertByteList, 0);
    }
    public uint GetUint(byte[] dataBuffer, int iPosition)
    {
        var revertByteList = new byte[4];
        Array.Copy(dataBuffer, iPosition, revertByteList, 0, 4);
        revertByteList = revertByteList.Reverse().ToArray();
        return BitConverter.ToUInt32(revertByteList, 0);
    }
    public int GetInt(byte[] dataBuffer, int iPosition)
    {
        var revertByteList = new byte[4];
        Array.Copy(dataBuffer, iPosition, revertByteList, 0, 4);
        revertByteList = revertByteList.Reverse().ToArray();
        return BitConverter.ToInt32(revertByteList, 0);
    }
}