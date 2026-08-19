using System;
using System.IO;

namespace HighMetroServer.ClassLib;

public class WriteErrorToLog
{
 #region 私有数据成员；
    private static object _syncErrorLogRoot;
    private static object _syncSocketLogRoot;
    private static string _errorLogDic;
    private static string _updateLogDic;
    #endregion

    #region 静态构造函数；
    static WriteErrorToLog()
    {
        _syncErrorLogRoot = new object();
        _syncSocketLogRoot = new object();
        _errorLogDic = SystemInfo.LogErrorDir;
        _updateLogDic = SystemInfo.UpdateDir;
    }
    #endregion

    public static void WriteToErrorLog(Exception ex, string funName)
    {
        //lock (_syncErrorLogRoot)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(_errorLogDic + DateTime.Now.ToString("yyyy-MM-dd") + ".log", true))
                {
                    writer.WriteLine(funName + " " + ex.GetType().ToString() + " " + ex.Message + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    writer.Flush();
                    writer.Close();
                }
            }
            catch { }
        }
    }
    public static void WriteSocketToLog(string message)
    {
        lock (_syncSocketLogRoot)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(_errorLogDic + DateTime.Now.ToString("yyyy-MM-dd") + "socket.log", true))
                {
                    writer.WriteLine(message + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    writer.Flush();
                    writer.Close();
                }
            }
            catch { }
        }
    }
    public static void WriteUpdateToLog(string message)
    {
        lock (_syncSocketLogRoot)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(_errorLogDic + DateTime.Now.ToString("yyyy-MM-dd") + "updatesuccess.log", true))
                {
                    writer.WriteLine(message + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    writer.Flush();
                    writer.Close();
                }
            }
            catch { }
        }
    }
    public static void WriteUpdateLog(string message)
    {
        //lock (_syncSocketLogRoot)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(_updateLogDic + DateTime.Now.ToString("yyyy-MM-dd") + "update.log", true))
                {
                    writer.WriteLine(message + " " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    writer.Flush();
                    writer.Close();
                }
            }
            catch { }
        }
    }
    public static void WriteConnectLog(string message)
    {
        //lock (_syncSocketLogRoot)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(_errorLogDic + DateTime.Now.ToString("yyyy-MM-dd") + "connect.log", true))
                {
                    writer.WriteLine(message);
                    writer.Flush();
                    writer.Close();
                }
            }
            catch { }
        }
    }
    public static void WriteDataBaseLog(string message)
    {
        //lock (_syncSocketLogRoot)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(_errorLogDic + DateTime.Now.ToString("yyyy-MM-dd") + "database.log", true))
                {
                    writer.WriteLine(message);
                    writer.Flush();
                    writer.Close();
                }
            }
            catch { }
        }
    }   
}