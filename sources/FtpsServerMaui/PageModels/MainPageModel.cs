using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FtpsServerMaui.Models;

namespace FtpsServerMaui.PageModels
{
    public partial class MainPageModel(ModalErrorHandler errorHandler) : ObservableObject, IProjectTaskPageModel
    {
        private bool _isNavigatedTo;
        private bool _dataLoaded;
        private readonly ModalErrorHandler _errorHandler = errorHandler;

        [ObservableProperty]
        private List<Brush> _todoCategoryColors = [];

        [ObservableProperty]
        bool _isBusy;

        [ObservableProperty]
        bool _isRefreshing;

        [ObservableProperty]
        private string _today = DateTime.Now.ToString("dddd, MMM d");

        private async Task LoadData()
        {
            try
            {
                IsBusy = true;

                // TODO: data loading
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task Refresh()
        {
            try
            {
                IsRefreshing = true;
                await LoadData();
            }
            catch (Exception e)
            {
                _errorHandler.HandleError(e);
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private void NavigatedTo() =>
            _isNavigatedTo = true;

        [RelayCommand]
        private void NavigatedFrom() =>
            _isNavigatedTo = false;

        [RelayCommand]
        private async Task Appearing()
        {
            if (!_dataLoaded)
            {
                _dataLoaded = true;
                await Refresh();
            }
            // This means we are being navigated to
            else if (!_isNavigatedTo)
            {
                await Refresh();
            }
        }

        [RelayCommand]
        private Task AddTask()
            => Shell.Current.GoToAsync($"task");
    }
}