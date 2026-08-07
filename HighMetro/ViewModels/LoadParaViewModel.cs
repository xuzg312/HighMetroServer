using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighMetro.Models;

namespace HighMetro.ViewModels;

public partial class LoadParaViewModel : ViewModelBase
{
    public event Action? OnCancel;
    
    [ObservableProperty]
    private string _errorMessage = "";

    public LoadParaViewModel(ResultInfo resultInfo)
    {
        ErrorMessage = resultInfo.Message;
    }
    [RelayCommand]
    private void Cancel(MainViewModel rootVm)
    {
        OnCancel?.Invoke();
    }
}