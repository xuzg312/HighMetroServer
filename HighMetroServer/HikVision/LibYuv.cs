using System;
using System.Runtime.InteropServices;

namespace HighMetroServer.HikVision;

public static partial class LibYuv
{
    [LibraryImport("LibYuv")]
    public static partial int I420ToBGRA(
        IntPtr srcY,
        int srcStrideY,
        IntPtr srcV,
        int srcStrideV,
        IntPtr srcU,
        int srcStrideU,
        IntPtr dstArgb,
        int dstStrideArgb,
        int width,
        int height);
}