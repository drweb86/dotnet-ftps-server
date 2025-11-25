using CommunityToolkit.Mvvm.Input;
using FtpsServerMaui.Models;

namespace FtpsServerMaui.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}