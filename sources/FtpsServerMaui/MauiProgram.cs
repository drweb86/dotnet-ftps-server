using CommunityToolkit.Maui;
using FtpsServerMaui.ViewModels;
using FtpsServerMaui.Views;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;

namespace FtpsServerMaui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureSyncfusionToolkit()
                .ConfigureMauiHandlers(handlers =>
                {
#if WINDOWS
    				Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler.Mapper.AppendToMapping("KeyboardAccessibleCollectionView", (handler, view) =>
    				{
    					handler.PlatformView.SingleSelectionFollowsFocus = false;
    				});

#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                });

#if DEBUG
    		builder.Logging.AddDebug();
    		builder.Services.AddLogging(configure => configure.AddDebug());
#endif

            builder.Services.AddSingleton<IFtpsService, FtpsService>();
            builder.Services.AddSingleton<ILogService, LogService>();
            builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
            builder.Services.AddSingleton<IUserEditorService, UserEditorService>();

            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddTransient<SimpleSetupViewModel>();
            builder.Services.AddTransient<AdvancedSetupViewModel>();
            builder.Services.AddTransient<UserEditorViewModel>();

            builder.Services.AddSingleton<Views.MainPage>();
            builder.Services.AddTransient<SimpleSetupPage>();
            builder.Services.AddTransient<AdvancedSetupPage>();
            builder.Services.AddTransient<UserEditorPage>();

            builder.Services.AddSingleton<ProjectRepository>();
            builder.Services.AddSingleton<TaskRepository>();
            builder.Services.AddSingleton<CategoryRepository>();
            builder.Services.AddSingleton<TagRepository>();
            builder.Services.AddSingleton<SeedDataService>();
            builder.Services.AddSingleton<ModalErrorHandler>();
            builder.Services.AddSingleton<MainPageModel>();

            Routing.RegisterRoute(nameof(SimpleSetupPage), typeof(SimpleSetupPage));
            Routing.RegisterRoute(nameof(AdvancedSetupPage), typeof(AdvancedSetupPage));
            Routing.RegisterRoute(nameof(UserEditorPage), typeof(UserEditorPage));

            return builder.Build();
        }
    }
}
