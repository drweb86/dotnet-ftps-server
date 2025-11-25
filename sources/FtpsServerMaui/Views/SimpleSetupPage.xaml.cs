using FtpsServerMaui.ViewModels;

namespace FtpsServerMaui.Views;

public partial class SimpleSetupPage : ContentPage
{
    public SimpleSetupPage(SimpleSetupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
