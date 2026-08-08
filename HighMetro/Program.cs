using Avalonia;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using HighMetro.HikVision;

namespace HighMetro;

sealed class Program
{
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
        // LibraryImport写的逻辑库名，这里映射到真实系统库
        Console.WriteLine($"【Resolver被触发】libraryName={libraryName}"); // 加打印，看有没有进来
        if (libraryName == "HCNetSDK")
        {
            if (HikPlatform.IsWindows)
            {
                return NativeLibrary.Load("HCNetSDK.dll", assembly, searchPath);
            }
            if (HikPlatform.IsLinux)
            {
                return NativeLibrary.Load("libhcnetsdk.so", assembly, searchPath);
            }
            // Mac环境：返回空，不加载，调用时抛异常，业务层做判断避开调用
            return IntPtr.Zero;
        }
        if (libraryName == "PlayCtrl")
        {
            if (HikPlatform.IsWindows)
                return NativeLibrary.Load("PlayCtrl.dll", assembly, searchPath);
            if (HikPlatform.IsLinux)
                return NativeLibrary.Load("libPlayCtrl.so", assembly, searchPath);
            return IntPtr.Zero;
        }
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