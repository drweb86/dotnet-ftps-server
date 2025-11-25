using FtpsServerMaui.Models;
using FtpsServerMaui.ViewModels;

namespace FtpsServerMaui.Views;

public partial class AdvancedSetupPage : ContentPage
{
    private readonly AdvancedSetupViewModel _viewModel;

    public AdvancedSetupPage(AdvancedSetupViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Refresh the view when returning from user editor
        _viewModel.RefreshUsers();
    }
}
