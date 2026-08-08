namespace HighMetro.BaseModel;

public class PublicConst
{
    //组激活定义；
    public const string FlagYes = "Y";
    public const string FlagNo = "N";

    public const string SaveOk = "ok";
    public const string Mainboard = "M";//主板;
    public const string CommHmi = "H";//HMI;
    public const string HardCamera = "H";//硬盘摄像机;
    public const string PhotoCamera = "C";//摄像机;
    //Socket数据；
    public const int InitBufferLength = 64;
    public const int ExpandBufferLength = 64;
    public const int SockDataMinLength = 1;
    public const int SockDataMaxLength = 4096;
    public const int PerSockDataMaxLength = 128;
    public const int ClientMaxLength = 255;
    public const int MaxBufferSize = 1024 * 1024;
        
    public const byte OnLine = 0xF0;          //上线命令；
    public const byte OffLine = 0xF1;          //下线命令；
    public const byte SendCount = 2;           //UDP发送次数；

    public const string Clr = "\r\n";
    public const long TimeLength = 30 * 60 * 1000;
    public const long UdpTimeLength = 5 * 60 * 1000;

    public const string Tcp = "TCP";
    public const string Comm = "COMM";

    public const byte GetEnter = 0;
    public const byte GetExit = 1;

    public const string Enter = "入口";
    public const string Exit = "出口";

    public const byte DireAEnter = 0X03;//入口A; 
    public const byte DireAExit = 0X0C; //出口B; 
    public const byte DireAll = 0X0F;//出入口AB; 

    public const string DireADoor = "A";//A门; 
    public const string DireBDoor = "B";//B门; 
    public const string DireDoor = "-";//不区分; 

    public const byte IdentifyNone = 0;//未验证；
    public const byte IdentifyHeart = 1;//验证,仅发送心跳；
    public const byte IdentifyAll = 2;//验证,实时监控数据；
    public const byte IdentifyPhoto = 3;//获取拍照的图片文件；
}