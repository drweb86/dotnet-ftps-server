using FtpsServerMaui.Models;
using FtpsServerMaui.PageModels;

namespace FtpsServerMaui.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}