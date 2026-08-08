using System;
using System.Runtime.InteropServices;

namespace HighMetro.HikVision;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void RealDataCallBackEx(int lRealHandle, uint dwDataType, IntPtr pBuffer, uint dwBufSize, IntPtr pUser);

public static partial class HikSdk
{
    [DllImport("HCNetSDK", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool NET_DVR_Init();
    
    [DllImport("HCNetSDK", CallingConvention = CallingConvention.Cdecl)]
    public static extern int NET_DVR_Login_V40(
        ref CHCNetSDK.NET_DVR_USER_LOGIN_INFO pLoginInfo, 
        ref CHCNetSDK.NET_DVR_DEVICEINFO_V40 lpDeviceInfo);

    [LibraryImport("HCNetSDK")]
    public static partial uint NET_DVR_GetLastError();

    [LibraryImport("HCNetSDK")]
    public static partial int NET_DVR_Logout(int iUserId);
    
    [LibraryImport("HCNetSDK")]
    public static partial int NET_DVR_CaptureJPEGPicture_NEW(
        int lUserId, 
        int lChannel, 
        ref CHCNetSDK.NET_DVR_JPEGPARA lpJpegPara, 
        IntPtr sJpegPicBuffer, 
        uint dwPicSize, 
        ref uint lpSizeReturned);

    [LibraryImport("HCNetSDK")]
    public static partial int NET_DVR_Cleanup();

    [LibraryImport("HCNetSDK")]
    public static partial int NET_DVR_SetRealDataCallBack_V30(
        int lUserId,
        RealDataCallBackEx fRealDataCallBack,
        IntPtr pUser,
        int dwBufferSize);
    
    [LibraryImport("HCNetSDK")]
    public static partial int NET_DVR_StopRealPlay(int lRealPlayHandle);
}