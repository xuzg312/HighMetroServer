using System;
using CommunityToolkit.Mvvm.Input;

namespace HighMetroServer.ViewModels;

public partial class ExitConfirmViewModel : ViewModelBase
{
    public event Action? OnConfirm;
    public event Action? OnCancel;

    [RelayCommand]
    private void Confirm()
    {
        OnConfirm?.Invoke();
    }
    [RelayCommand]
    private void Cancel(){
        OnCancel?.Invoke();
    }
}