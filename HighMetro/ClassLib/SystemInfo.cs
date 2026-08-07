using System;
using System.IO;

namespace HighMetro.ClassLib;

public class SystemInfo
{
    #region 静态私有数据成员
    private static string _currentDir;           //当前目录；
    private static string _logErrorDir;          //错误日志目录；
    private static string _systemLib;            //系统类库路径；
    private static string _sysConfigDir;         //系统配置路径；
    private static string _tempDir;              //临时目录；
    private static string _updateDir;            //更新日志目录；
    private static string _photoDir;             //图像、录像目录；
    #endregion

    #region 构造函数；
    static SystemInfo()
    {
        //_currentDir = Directory.GetCurrentDirectory();
        _currentDir = AppContext.BaseDirectory;
        _systemLib = _currentDir + @"\SystemLib\";
        _logErrorDir = _currentDir + @"\Log\";
        _sysConfigDir = _currentDir + @"\Config\";
        _tempDir = _currentDir + @"\Temp\";
        UpdateDir = _currentDir + @"\Update\";
        PhotoDir = _currentDir + @"\PhotoLog\";
        #region 创建目录；
        if (!Directory.Exists(_logErrorDir))
        {
            Directory.CreateDirectory(_logErrorDir);
        }
        if (!Directory.Exists(_tempDir))
        {
            Directory.CreateDirectory(_tempDir);
        }
        if (!Directory.Exists(_sysConfigDir))
        {
            Directory.CreateDirectory(_sysConfigDir);
        }
        if (!Directory.Exists(UpdateDir))
        {
            Directory.CreateDirectory(UpdateDir);
        }
        if (!Directory.Exists(PhotoDir))
        {
            Directory.CreateDirectory(PhotoDir);
        }
        #endregion
    }
    #endregion

    #region 访问器成员函数属性；
    public static String CurrentDir { get { return _currentDir; } }
    public static String SystemLib { get { return _systemLib; } }
    public static String LogErrorDir { get { return _logErrorDir; } }
    public static String SysConfigDir { get { return _sysConfigDir; } }
    public static String TempDir { get { return _tempDir; } }
    public static string UpdateDir { get => _updateDir; set => _updateDir = value; }
    public static string PhotoDir { get => _photoDir; set => _photoDir = value; }
    #endregion
}