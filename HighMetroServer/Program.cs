using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using HighMetroServer.HikVision;

namespace HighMetroServer;

sealed class Program
{
    // 使用静态字典缓存已成功加载的库句柄，确保绝对只加载一次
    private static readonly Dictionary<string, IntPtr> LoadedLibraries = new();

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        // 注册原生库解析器：运行时根据OS返回对应的库文件名
        NativeLibrary.SetDllImportResolver(typeof(HikSdk).Assembly, ResolveHikLibrary);

        var app = BuildAvaloniaApp();
        return app.StartWithClassicDesktopLifetime(args);
    }
    private static IntPtr ResolveHikLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (LoadedLibraries.TryGetValue(libraryName, out var cachedHandle))
        {
            return cachedHandle;
        }
        // 1. 确定当前平台的基础路径（完美兼容单文件发布解压目录）
        var baseDir = AppContext.BaseDirectory;
        string libPath; // 默认回退到系统搜索
        if (HikPlatform.IsWindows)
        {
            var fileName = libraryName switch
            {
                "HCNetSDK" => "HCNetSDK.dll",
                "PlayCtrl" => "PlayCtrl.dll",
                "LibYuv" => "libyuv.dll",
                _ => libraryName
            };
            libPath = Path.Combine(baseDir, fileName);
        }
        else if (HikPlatform.IsLinux)
        {
            var fileName = libraryName switch
            {
                "HCNetSDK" => "libhcnetsdk.so",
                "PlayCtrl" => "libPlayCtrl.so",
                "LibYuv" => "libyuv.so",
                _ => libraryName
            }; 
            libPath = Path.Combine(baseDir, fileName);
        }
        else 
        {
            // macOS 或其他不支持的平台
            // 记录日志，返回 Zero 让上层处理，或者抛出明确异常
            Console.WriteLine($"[HikVision] 当前平台不支持加载 {libraryName}");
            return IntPtr.Zero; 
        }
        // 2. 使用 TryLoad 防止因为找不到文件直接抛出 DllNotFoundException
        if (NativeLibrary.TryLoad(libPath, assembly, searchPath, out var handle))
        {
            LoadedLibraries[libraryName] = handle;
            return handle;
        }
        // 3. 如果绝对路径加载失败，回退到默认解析逻辑
        Console.WriteLine($"[HikVision] 尝试加载原生库失败: {libPath}");
        return IntPtr.Zero;
    }
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}