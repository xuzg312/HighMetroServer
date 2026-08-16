using System;
using System.Runtime.InteropServices;

namespace HighMetro.HikVision;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void RealDataCallBackEx(int lRealHandle, uint dwDataType, IntPtr pBuffer, uint dwBufSize, IntPtr pUser);
public delegate void RealDataCallBack(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser);

public static partial class HikSdk
{
    [LibraryImport("HCNetSDK")]
    public static partial int NET_DVR_Init();
    
    [DllImport("HCNetSDK", CallingConvention = CallingConvention.Cdecl)]
    public static extern int NET_DVR_Login_V40(
        ref ChcNetSdk.NetDvrUserLoginInfo pLoginInfo, 
        ref ChcNetSdk.NetDvrDeviceinfoV40 lpDeviceInfo);

    [LibraryImport("HCNetSDK")]
    public static partial uint NET_DVR_GetLastError();

    [LibraryImport("HCNetSDK")]
    public static partial int NET_DVR_Logout(int iUserId);
    
    [LibraryImport("HCNetSDK")]
    public static partial int NET_DVR_CaptureJPEGPicture_NEW(
        int lUserId, 
        int lChannel, 
        ref ChcNetSdk.NetDvrJpegpara lpJpegPara, 
        IntPtr sJpegPicBuffer, 
        uint dwPicSize, 
        ref uint lpSizeReturned);

    [LibraryImport("HCNetSDK")]
    public static partial int NET_DVR_Cleanup();
    
    [LibraryImport("HCNetSDK")]
    public static partial int NET_DVR_StopRealPlay(int lRealPlayHandle);
    
    [LibraryImport("HCNetSDK")]
    public static partial int NET_DVR_RemoteControl(int lUserId, int dwCommand, IntPtr lpInBuffer, int dwInBufferSize);

    [DllImport("HCNetSDK", CallingConvention = CallingConvention.Cdecl)]
    public static extern int NET_DVR_RealPlay_V40(
        int iUserId, 
        ref ChcNetSdk.NetDvrPreviewInfo lpPreviewInfo, 
        RealDataCallBack fRealDataCallBack_V30, 
        IntPtr pUser);
    

}