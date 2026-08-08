using System;
using System.IO;

namespace HighMetro.ClassLib;

public static class SystemInfo
{
    #region 静态私有数据成员
    public static string CurrentDir { get; private set; }//当前目录；
    public static string SystemLib { get; private set; }//系统类库路径；
    public static string LogErrorDir { get; private set; }//错误日志目录；
    public static string SysConfigDir { get; private set; }//系统配置路径；
    public static string TempDir { get; private set; }//临时目录；
    public static string UpdateDir { get; private set; }//更新日志目录；
    public static string PhotoDir { get; private set; }//图像、录像目录；
    #endregion

    #region 构造函数；
    static SystemInfo()
    {
        //_currentDir = Directory.GetCurrentDirectory();
        CurrentDir = AppContext.BaseDirectory;
        SystemLib = Path.Combine(CurrentDir,"SystemLib");
        LogErrorDir = Path.Combine(CurrentDir,"Log");
        SysConfigDir = Path.Combine(CurrentDir,"Config");
        TempDir = Path.Combine(CurrentDir,"Temp");
        UpdateDir = Path.Combine(CurrentDir,"Update");
        PhotoDir = Path.Combine(CurrentDir,"PhotoLog");
        #region 创建目录；
        if (!Directory.Exists(LogErrorDir))
        {
            Directory.CreateDirectory(LogErrorDir);
        }
        if (!Directory.Exists(TempDir))
        {
            Directory.CreateDirectory(TempDir);
        }
        if (!Directory.Exists(SysConfigDir))
        {
            Directory.CreateDirectory(SysConfigDir);
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
}