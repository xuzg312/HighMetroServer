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

    public const byte SelfStart = 1;//开机自启动；
    public const byte CommDataParseTask = 3;//串口数据解析后台任务个数；
    public const byte TcpDataParseTask = 2;//Tcp数据解析后台任务个数；

    public const byte PageSize = 10;
}