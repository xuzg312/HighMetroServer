using System.Threading;
using System.Threading.Tasks;
using HighMetroServer.HikVision;

namespace HighMetroServer.Services;

public static class CamRemoteManager
{
    private static bool _initialized;
    private static readonly SemaphoreSlim SdkInitLock = new(1,1);
    public static async Task<int> SdkInitialize()
    {
        await SdkInitLock.WaitAsync();
        try
        {
            if (_initialized)
                return 0; 
            var ret = HikSdk.NET_DVR_Init();
            if(ret >=0)
            {
                _initialized = true;
            }
            return ret;
        }
        finally
        {
            SdkInitLock.Release();
        }
    }
    public static void SdkCleanUp()
    {
        SdkInitLock.Wait();
        try
        {
            if(_initialized)
            {
                HikSdk.NET_DVR_Cleanup();
                _initialized = false;
            }
        }
        finally
        {
            SdkInitLock.Release();
        }
    }
}