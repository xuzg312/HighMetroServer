using System;
using System.Collections.Generic;
using System.Text;
using HighMetroServer.BaseModel;

namespace HighMetroServer.ClassLib;

public static class ParseMainBordData
{
    public static MainInfoBean? ReplyHeartInfo(SocketDataBlock socketDataBlock)
    {
        var mainInfoBean00 = new MainInfoBean();
        //总长度62字节；
        if (socketDataBlock.Length < 62)
        {
            return null;
        }
        byte iPosition = 3;
        //设备id
        PublicUntil publicUntil = new PublicUntil();
        mainInfoBean00.Id = publicUntil.GetUshort(socketDataBlock.Content!, iPosition);
        mainInfoBean00.Datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        //长度；---1字节
        iPosition = 7;
        mainInfoBean00.Length = socketDataBlock.Content![iPosition++];
        //通行指示---1字节
        mainInfoBean00.Txzs = socketDataBlock.Content[iPosition++];
        //开门次数---2字节;
        mainInfoBean00.Kmcs = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
        iPosition += 2;
        //运行模式---1字节;
        mainInfoBean00.Yxms = socketDataBlock.Content[iPosition++];
        //A门工作模式；
        mainInfoBean00.Agzms = socketDataBlock.Content[iPosition++];
        //A门状态---1字节
        mainInfoBean00.Astate = socketDataBlock.Content[iPosition++];
        //A1故障码---1字节；
        mainInfoBean00.A1gzm = socketDataBlock.Content[iPosition++];
        //A2故障码---1字节；
        mainInfoBean00.A2gzm = socketDataBlock.Content[iPosition++];
        //A1转速----2字节；
        mainInfoBean00.A1zs = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
        iPosition += 2;
        //A2转速----2字节；
        mainInfoBean00.A2zs = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
        iPosition += 2;
        //A1电流----1字节
        mainInfoBean00.A1dl = socketDataBlock.Content[iPosition++];
        //A2电流----1字节
        mainInfoBean00.A2dl = socketDataBlock.Content[iPosition++];
        //A1位置---2字节；
        mainInfoBean00.A1wz = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
        iPosition += 2;
        //A2位置---2字节；
        mainInfoBean00.A2wz = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
        iPosition += 2;

        //预留---4字节；
        iPosition += 4;

        //B门状态---1字节
        mainInfoBean00.Bstate = socketDataBlock.Content[iPosition++];
        //B1故障码---1字节；
        mainInfoBean00.B1gzm = socketDataBlock.Content[iPosition++];
        //B2故障码---1字节；
        mainInfoBean00.B2gzm = socketDataBlock.Content[iPosition++];
        //B1转速----2字节；
        mainInfoBean00.B1zs = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
        iPosition += 2;
        //B2转速----2字节；
        mainInfoBean00.B2zs = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
        iPosition += 2;
        //B1电流----1字节
        mainInfoBean00.B1dl = socketDataBlock.Content[iPosition++];
        //B2电流----1字节
        mainInfoBean00.B2dl = socketDataBlock.Content[iPosition++];
        //B1位置---2字节；
        mainInfoBean00.B1wz = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
        iPosition += 2;
        //B2位置---2字节；
        mainInfoBean00.B2Wz = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
        iPosition += 2;
        //DL传感器状态----2字节;
        mainInfoBean00.Dlcgqzt = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
        iPosition += 2;
        //DO1-4状态---2字节；
        mainInfoBean00.Dostate = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
        iPosition += 2;
        //扩展DI DO---2字节；
        mainInfoBean00.Kzdldo = publicUntil.GetUshort(socketDataBlock.Content, iPosition);
        iPosition += 2;

        //预留---8字节；
        iPosition += 8;

        //累加和；---1字节
        mainInfoBean00.Total = socketDataBlock.Content[iPosition];
        mainInfoBean00.Value1 = socketDataBlock.Value1;
        mainInfoBean00.Value2 = socketDataBlock.Value2;
        mainInfoBean00.Value1Length = socketDataBlock.Value1Length;
        mainInfoBean00.Value2Length = socketDataBlock.Value2Length;
        return mainInfoBean00;
    }
    public static List<string> ParsePack(MainInfoBean mainInfoBean)
    {
        var data = new List<string>();
        var sb = new StringBuilder();
        sb.Append("主板ID：").Append(mainInfoBean.Id);
        sb.Append("\r\n类型：心跳");
        sb.Append("\r\n数据长度：").Append(mainInfoBean.Length);
        sb.Append("\r\n通行指示：").Append(GetTxzs(mainInfoBean.Txzs));
        sb.Append("\r\n开门次数：").Append(mainInfoBean.Kmcs);
        sb.Append("\r\n运行模式：").Append(GetYxms(mainInfoBean.Yxms));
        sb.Append("\r\n工作模式：").Append(GetAgzms(mainInfoBean.Agzms, mainInfoBean.Yxms));
        sb.Append("\r\n交互时间：").Append(mainInfoBean.Datetime);
        data.Add(sb.ToString());

        sb = new StringBuilder();
        sb.Append("A门状态：").Append(GetAstate(mainInfoBean.Astate));
        sb.Append("\r\nA1故障码：").Append(GetFault(mainInfoBean.A1gzm));
        sb.Append("\r\nA2故障码：").Append(GetFault(mainInfoBean.A2gzm));
        sb.Append("\r\nA1转速：").Append(mainInfoBean.A1zs);
        sb.Append("\r\nA2转速：").Append(mainInfoBean.A2zs);
        sb.Append("\r\nA1电流：").Append((mainInfoBean.A1dl * 0.1).ToString("0.00"));
        sb.Append("\r\nA2电流：").Append((mainInfoBean.A2dl * 0.1).ToString("0.00"));
        sb.Append("\r\nA1位置：").Append(mainInfoBean.A1wz);
        sb.Append("\r\nA2位置：").Append(mainInfoBean.A2wz);
        data.Add(sb.ToString());

        sb = new StringBuilder();
        sb.Append("B门状态：").Append(GetAstate(mainInfoBean.Bstate));
        sb.Append("\r\nB1故障码：").Append(GetFault(mainInfoBean.B1gzm));
        sb.Append("\r\nB2故障码：").Append(GetFault(mainInfoBean.B2gzm));
        sb.Append("\r\nB1转速：").Append(mainInfoBean.B1zs);
        sb.Append("\r\nB2转速：").Append(mainInfoBean.B2zs);
        sb.Append("\r\nB1电流：").Append((mainInfoBean.B1dl * 0.1).ToString("0.00"));
        sb.Append("\r\nB2电流：").Append((mainInfoBean.B2dl * 0.1).ToString("0.00"));
        sb.Append("\r\nB1位置：").Append(mainInfoBean.B1wz);
        sb.Append("\r\nB2位置：").Append(mainInfoBean.B2Wz);
        data.Add(sb.ToString());

        var value = Convert.ToString(mainInfoBean.Dlcgqzt, 16).ToUpper();
        if (value.Length < 2)
        {
            value = "0" + value;
        }
        sb = new StringBuilder();
        sb.Append("DI传感器状态：").Append(value);
        sb.Append("\r\nDO1-4状态：").Append(GetDostate(mainInfoBean.Dostate));
        sb.Append("\r\n扩展DIDO：").Append(mainInfoBean.Kzdldo);
        sb.Append("\r\n累加和：").Append(mainInfoBean.Total);
        sb.Append("\r\n收数据帧数：").Append(mainInfoBean.Value1).Append("，长度：").Append(mainInfoBean.Value1Length);
        sb.Append("\r\n有效帧数：").Append(mainInfoBean.Value2).Append("，长度：").Append(mainInfoBean.Value2Length);
        data.Add(sb.ToString());

        return data;
    }
    private static string GetTxzs(int txzs)
    {
        switch (txzs)
        {
            case 0XD1:
                return "正常";
            case 0XD2:
                return "正常";
            case 0XD3:
                return "紧急通行";
            case 0XD4:
                return "设备维修";
            case 0XD5:
                return "禁止使用";
            case 0XD6:
                return "设备上锁";
            default:
                return "--";
        }
    }
    private static String GetYxms(int Yxms)
    {
        switch (Yxms)
        {
            case 0X00:
                return "手动";
            case 0X01:
                return "自动";
            default:
                return "--";
        }
    }
    private static String GetAgzms(int Agzms,int Yxms)
    {
        switch (Yxms)
        {
            case 0X00://"手动" 4=AB 常开，5=AB 常关，6=AB 紧急全开，7=AB 禁用，8=AB 锁门，9=AB 维修
                switch (Agzms)
                {
                    case 0X04:
                        return "AB 常开";
                    case 0X05:
                        return "AB 常闭";
                    case 0X06:
                        return "AB 紧急全开";
                    case 0X07:
                        return "AB 禁用";
                    case 0X08:
                        return "AB 锁门";
                    case 0X09:
                        return "AB 维修";
                    default:
                        return "--";
                }
            case 0X01://自动;双闸机（0=A 常关，1=A 常开），单闸机（2=A 单闸机，3=B 单闸机）
                switch (Agzms)
                {
                    case 0X00:
                        return "A 常闭";
                    case 0X01:
                        return "A 常开";
                    case 0X02:
                        return "A 单闸机";
                    case 0X03:
                        return "B 单闸机";
                    default:
                        return "--";
                }
            default:
                return "--";
        }
    }
    private static String GetAstate(int Astate)
    {
        switch (Astate)
        {
            case 0X00:
                return "停止";
            case 0X01:
                return "开门中";
            case 0X02:
                return "开到位";
            case 0X03:
                return "关门中";
            case 0X04:
                return "关到位";
            case 0X05:
                return "开关门超时";
            default:
                return "--";
        }
    }
    private static String GetFault(int value)
    {
        switch (value)
        {
            case 0X00:
                return "0-无错误";
            case 0X01:
                return "1-未学习";
            case 0X02:
                return "2-堵转停止";
            case 0X03:
                return "3-霍尔错误";
            case 0X04:
                return "4-速度失败";
            case 0X05:
                return "5-保留";
            case 0X06:
                return "6-过流关断";
            case 0X07:
                return "7-过热关断";
            case 0X08:
                return "8-过压关断";
            case 0X09:
                return "9-欠压关断";
            case 0X10:
                return "10-短路/过流";
            case 0X11:
                return "11-保留";
            case 0X12:
                return "12-保留";
            case 0X13:
                return "13-驱动器内部通讯异常";
            case 0X14:
                return "14-A门开关超时";
            case 0X15:
                return "15-B门开关超时";
            default:
                return "--";
        }
    }
    private static string GetDostate(int dostate)
    {
        switch (dostate)
        {
            case 0X00:
                return "关";
            case 0X01:
                return "开";
            default:
                return "--";
        }
    }
}