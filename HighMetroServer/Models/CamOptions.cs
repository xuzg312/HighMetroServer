using CommunityToolkit.Mvvm.ComponentModel;

namespace HighMetroServer.Models;

public partial class CamOptions : ObservableObject
{
    [ObservableProperty] 
    private string _ip;
    
    [ObservableProperty] 
    private int _port;
    
    [ObservableProperty] 
    private string _userName;
    
    public CamOptions(string ip,int port,string userName)
    {
        Ip = ip;
        Port = port;
        UserName = userName;
    }
}