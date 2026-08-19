using System;
using System.IO;
using System.Reflection;

namespace HighMetroServer.ClassLib;

public static class SystemInfo
{
    #region 静态私有数据成员
    public static string SystemLib { get; private set; }//系统类库路径；
    public static string LogErrorDir { get; private set; }//错误日志目录；
    public static string SysConfigDir { get; private set; }//系统配置路径；
    public static string UpdateDir { get; private set; }//更新日志目录；
    public static string PhotoDir { get; private set; }//图像、录像目录；
    
    public static string TempDir { get; private set; }//临时目录；
    #endregion

    #region 构造函数；
    static SystemInfo()
    {
        //_currentDir = Directory.GetCurrentDirectory();
        //var currentDir = AppContext.BaseDirectory;
        var currentDir = GetApplicationDirectory();
        SystemLib = Path.Combine(currentDir,"SystemLib");
        LogErrorDir = Path.Combine(currentDir,"Log");
        SysConfigDir = Path.Combine(currentDir,"Config");
        UpdateDir = Path.Combine(currentDir,"Update");
        PhotoDir = Path.Combine(currentDir,"PhotoLog");
        TempDir = Path.Combine(currentDir,"Temp");
        #region 创建目录；
        if (!Directory.Exists(LogErrorDir))
        {
            Directory.CreateDirectory(LogErrorDir);
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
        if (!Directory.Exists(TempDir))
        {
            Directory.CreateDirectory(TempDir);
        }
        #endregion
    }
    #endregion

    static string GetApplicationDirectory()
    {
        var exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath))
        {
            return Path.GetDirectoryName(exePath)!;
        }
        // 降级兜底：非单文件场景
        return AppContext.BaseDirectory.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}