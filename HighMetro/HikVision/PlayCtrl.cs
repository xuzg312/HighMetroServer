using System;
using System.Runtime.InteropServices;

namespace HighMetro.HikVision;

public static partial class PlayCtrl
{
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
    
    public delegate void DeccbFun(int nPort, IntPtr pBuf, int nSize, ref FrameInfo pFrameInfo, int nReserved1, int nReserved2);

    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_SetDecCallBackEx(int nPort, DeccbFun decCbFun, IntPtr pDest, int nDestSize);
    
    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_GetPort(ref int nPort);
    
    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_Stop(int nPort);

    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_CloseStream(int nPort);

    [LibraryImport("PlayCtrl")]
    public static partial int PlayM4_FreePort(int nPort);

}