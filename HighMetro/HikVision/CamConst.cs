namespace HighMetro.HikVision;

public static class CamConst
{
    public const int NetDvrSysHead = 1;   // 流头（视频参数头，属于视频相关，要送入PlayM4）
    public const int NetDvrStreamData = 2;   // 视频数据帧（H264/H265 NALU，核心）
    public const int NetDvrAudioStreamData = 3;   // 音频流 → 直接丢弃（当前只做视频预览）
    public const int NetDvrStdVideoData = 4;//标准视频流数据
    public const int NetDvrStdAudioData = 5;//标准音频流数据
    
    public const int StreamRealTime = 0;
    public const int StreamFile = 1;

    public const int BufPoolSize = 1024 * 1024 * 8;//分配8M缓冲区存储摄像头传递的待解码的数据；
    public const int DisplayBufNumber = 15;//设置播放库内部“显示缓冲区”的最大缓冲帧数。
}