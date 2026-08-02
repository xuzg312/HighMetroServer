using CommunityToolkit.Mvvm.ComponentModel;

namespace HighMetro.ViewModels;

public partial class MainPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _welcomeText = "登录成功！主业务界面";
}