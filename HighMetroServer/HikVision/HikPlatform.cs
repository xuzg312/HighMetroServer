using System;

namespace HighMetroServer.HikVision;

public static class HikPlatform
{
    public static bool IsWindows => OperatingSystem.IsWindows();
    public static bool IsLinux => OperatingSystem.IsLinux();
    public static bool IsMac => OperatingSystem.IsMacOS();
}