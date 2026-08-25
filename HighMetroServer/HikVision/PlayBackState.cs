namespace HighMetroServer.HikVision;

public enum PlayBackState
{
    /// <summary>
    /// 空闲/未打开文件
    /// </summary>
    Idle = 0,

    /// <summary>
    /// 正在播放
    /// </summary>
    Playing = 1,

    /// <summary>
    /// 已暂停
    /// </summary>
    Paused = 2,
    
    /// <summary>
    /// 播放结束(EOF)
    /// </summary>
    Ended = 3
}