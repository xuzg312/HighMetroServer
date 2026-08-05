using CommunityToolkit.Mvvm.ComponentModel;

namespace HighMetro.Models;

public partial class HostOptions : ObservableObject
{
    [ObservableProperty] 
    private string _ip;
    
    [ObservableProperty] 
    private int _port;
    
    [ObservableProperty] 
    private string _code;
    
    [ObservableProperty] 
    private string _name;
    
    public HostOptions(string ip,int port,string code, string name)
    {
        Ip = ip;
        Port = port;
        Code = code;
        Name = name;
    }
}