using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FtpsServerMaui.Services;
using FtpsServerMaui.Views;
using System.Collections.ObjectModel;

namespace FtpsServerMaui.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFtpsService _ftpsService;
    private readonly ILogService _logService;

    [ObservableProperty]
    private bool _isServerRunning;

    [ObservableProperty]
    private string _serverStatus = "Stopped";

    [ObservableProperty]
    private ObservableCollection<string> _logMessages = new();

    public MainViewModel(IFtpsService ftpsService, ILogService logService)
    {
        _ftpsService = ftpsService;
        _logService = logService;

        _ftpsService.ServerStateChanged += OnServerStateChanged;
        _logService.LogMessageReceived += OnLogMessageReceived;

        // Load recent logs
        foreach (var log in _logService.GetRecentLogs())
        {
            LogMessages.Add(log);
        }
    }

    private void OnServerStateChanged(object? sender, bool isRunning)
    {
        IsServerRunning = isRunning;
        ServerStatus = isRunning ? "Running" : "Stopped";
    }

    private void OnLogMessageReceived(object? sender, string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            LogMessages.Add(message);
            while (LogMessages.Count > 100)
            {
                LogMessages.RemoveAt(0);
            }
        });
    }

    [RelayCommand]
    private async Task OpenSimpleSetupAsync()
    {
        await Shell.Current.GoToAsync(nameof(SimpleSetupPage));
    }

    [RelayCommand]
    private async Task OpenAdvancedSetupAsync()
    {
        await Shell.Current.GoToAsync(nameof(AdvancedSetupPage));
    }

    [RelayCommand]
    private async Task StopServerAsync()
    {
        try
        {
            await _ftpsService.StopServerAsync();
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to stop server: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        LogMessages.Clear();
        _logService.Clear();
    }
}
