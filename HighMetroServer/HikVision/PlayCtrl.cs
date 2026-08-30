using System;
using System.Runtime.InteropServices;

namespace HighMetroServer.HikVision;

public static partial class PlayCtrl
{
    public delegate void DeccbFun(
        int nPort, IntPtr pBuf, int nSize, ref FrameInfo frameInfo, IntPtr pUser);
    public delegate void FileEndCallBack(int nPort, IntPtr pUser);

    [StructLayout(LayoutKind.Sequential)]
    public struct FrameInfo
    {
        public int NWidth;
        public int NHeight;
        public int NStamp;
        public int NType;
        public int NFrameRate;
        public uint DwFrameNum;
        public void Init()
        {
            NWidth = 0;
            NHeight = 0;
            NStamp = 0;
            NType = 0;
            NFrameRate = 0;
            DwFrameNum = 0;
        }
    }

    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_OpenStream(int nPort, IntPtr pFileHeadBuf, uint nSize, uint nBufPoolSize);

    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_Play(int nPort, IntPtr hWnd);

    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_InputData(int nPort, IntPtr pBuf, uint nSize);
    
    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_SetDecCallBackEx(int nPort, DeccbFun decCbFun, IntPtr pDest, int nDestSize);
    
    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_SetDecCallBackExMend(int nPort, DeccbFun decCbFun, IntPtr pDest, int nDestSize,IntPtr pUser);

    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_GetPort(ref int nPort);
    
    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_Stop(int nPort);

    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_CloseStream(int nPort);

    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_FreePort(int nPort);
    
    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_SetStreamOpenMode(int nPort, uint nMode);

    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_SetDisplayBuf(int nPort, uint nNum);

    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_SetOverlayFlipMode(int nPort, int bOverlay, uint colorKey);

    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_GetLastError(int nPort);
    
    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_SetDecodeEngine(int nPort, int nEngine);
    
    [DllImport("PlayCtrl.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool PlayM4_OpenFile(int nPort, String sFileName);

    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_SetFileEndCallback(int nPort, FileEndCallBack fileEndCallback, IntPtr pUser);
    
    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_CloseFile(int nPort);

    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_Pause(int nPort, uint nPause);
    
    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_GetPlayPos(int nPort);
    
    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_SetPlayPos(int nPort, uint fPos);
}