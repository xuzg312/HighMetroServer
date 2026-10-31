using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HighMetroServer.ClassLib;

public static class LogUntil
{
    public static async Task WriteAscAsync(string message,string extend)
    {
        var logDirectory = SystemInfo.LogErrorDir;
        try
        {
            // 获取当前日期并生成文件名，例如：2026-10-03.log
            var fileName = $"{DateTime.Now:yyyy-MM-dd}-{extend}.log";
            var filePath = Path.Combine(logDirectory, fileName);
            // 格式化日志内容（添加时间戳）
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}";
            // 异步追加写入文件
            await File.AppendAllTextAsync(filePath, logEntry, Encoding.UTF8);
        }
        catch (Exception)
        {
            //忽略;
        }
    }
}