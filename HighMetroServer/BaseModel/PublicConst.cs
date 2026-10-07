namespace HighMetroServer.BaseModel;

public static class PublicConst
{
    //组激活定义；
    public const string FlagYes = "Y";
    public const string FlagNo = "N";

    public const string Mainboard = "M";//主板;
    public const string PhotoCamera = "C";//摄像机；
    
    //Socket数据；
    public const int SockDataMaxLength = 4096;
    public const int PerSockDataMaxLength = 128;
    public const int ClientMaxLength = 254;
    public const int MaxBufferSize = 1024 * 512;
    public const int MaxLogLines = 100;
    
    public const string DireDoor = "-";//不区分; 

    public const byte IdentifyNone = 0;//未验证；
    public const byte IdentifyHeart = 1;//验证,仅发送心跳；
    public const byte IdentifyAll = 2;//验证,实时监控数据；
    public const byte IdentifyPhoto = 3;//获取拍照的图片文件；

    public const string DoorStateCapture = "拍照";
    public const string DoorStateCamera = "录像";
    public const string DoorStatePerson = "人数";

    public const byte SelfStart = 1;//开机自启动；
    public const byte CommDataParseTask = 1;//串口数据解析后台任务个数；
    public const byte TcpDataParseTask = 1;//Tcp数据解析后台任务个数；

    public const byte PageSize = 10;
    
    public const byte TcpMessage = 0X01;//TCP消息;
    public const byte CommMessage = 0X02;//Comm消息;

    public const int HeartTcp = 2*60*1000;//检测周期；单位：秒；
    public const int HeartComm = 2*60*1000;//检测周期；单位：秒；
    public const int HeartCame = 2*60*1000;//检测周期；单位：秒；
    public const int HeartCommInter = 1;//串口空闲时间内无数据回传，断开重连接；单位：分钟；
    public const int HeartTcpInter = 2;//客户端空闲时间内无消息，强制关闭；单位：分钟；

    public const byte CamPhoto = 0X01;
    public const byte CamCamera = 0X02;
    public const byte CamPerson = 0X03;
    
    public const int CommAlarmPdc = 0x1103;//人流量统计报警上传，对应NET_DVR_PDC_ALRAM_INFO
    public const int DataBufferPoolMaxLength = 1_000;//最大1000个生产者数据;
}