using FtpsServerMaui.ViewModels;

namespace FtpsServerMaui.Views;

public partial class UserEditorPage : ContentPage
{
    public UserEditorPage(UserEditorViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
