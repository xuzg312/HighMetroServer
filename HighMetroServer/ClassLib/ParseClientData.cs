using System;
using System.IO;
using HighMetroServer.BaseModel;

namespace HighMetroServer.ClassLib;

public static class ParseClientData
{
    public static TcpDataBean? ParseTcpClientData(SocketDataBlock socketDataBlock)
    {
         if (socketDataBlock.Length < 10 ||
            socketDataBlock.Content![0] != 0XEB ||
            socketDataBlock.Content[1] == 0XAA)
        {
            return null;
        }
        TcpDataBean tcpDataBean = null!;
        var publicUntil = new PublicUntil();
        var iPosition = 7;
        var function = socketDataBlock.Content[iPosition];//功能；
        switch (function)
        {
            case 0X01://开门
            case 0X02://关门
            case 0X08://常开模式；
            case 0X09://常关模式；
            case 0X0E://紧急疏散；
            case 0X0F://禁用（全关）；
            case 0X10://锁定设备；
            case 0X11://解锁设备
            case 0X12://复位设备
            case 0X13://维修设备
            case 0X0C://临时常开
            case 0X0D://临时常关
            case 0X05://手动
            case 0X06://自动
                tcpDataBean = new TcpDataBean
                {
                    TurnComm = true
                };
                //hostBh；
                iPosition = 3;
                tcpDataBean.HostBh = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
                iPosition += 2;
                //id
                tcpDataBean.Id = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
                //协议中去掉hostId
                var data = new byte[socketDataBlock.Length-2];
                Array.Copy(socketDataBlock.Content, 0, data, 0, 3);
                Array.Copy(socketDataBlock.Content, 5, data, 3, socketDataBlock.Length - 5);
                Array.Copy(data, socketDataBlock.Content, data.Length);
                var length = socketDataBlock.Content[2];
                socketDataBlock.Content[2] = (byte)(length - 2);
                socketDataBlock.Length = data.Length;
                break;
            case 0XEC: //验证,实时监控数据；
                tcpDataBean = new TcpDataBean
                {
                    TurnComm = false,
                    Type = PublicConst.IdentifyAll
                };
                //hostBh；
                iPosition = 3;
                tcpDataBean.HostBh = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
                break;
            case 0XED: //验证,仅发送心跳；
                tcpDataBean = new TcpDataBean
                {
                    TurnComm = false,
                    Type = PublicConst.IdentifyHeart
                };
                //hostBh；
                iPosition = 3;
                tcpDataBean.HostBh = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
                break;
            case 0XEF: //客户端获取拍照的图片；
                tcpDataBean = new TcpDataBean
                {
                    TurnComm = false,
                    Type = PublicConst.IdentifyPhoto
                };
                //长度，1字节；
                var fileLength = socketDataBlock.Content[2] -5;
                tcpDataBean.FileName = System.Text.Encoding.UTF8.GetString(socketDataBlock.Content, 8, fileLength);
                //hostBh；
                iPosition = 3;
                tcpDataBean.HostBh = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
                break;
        }
        return tcpDataBean;
    }
    public static byte[]? GetPhotoFile(TcpDataBean tcpDataBean)
    {
        if (!File.Exists(tcpDataBean.FileName))
        {
            return null;
        }
        return File.ReadAllBytes(tcpDataBean.FileName);
    }
}