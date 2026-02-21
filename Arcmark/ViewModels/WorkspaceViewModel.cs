using System.Windows.Media;
using Arcmark.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arcmark.ViewModels;

public partial class WorkspaceViewModel : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private WorkspaceColorId _colorId;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isRenaming;
    [ObservableProperty] private SolidColorBrush _colorBrush = new(Colors.Transparent);
    [ObservableProperty] private SolidColorBrush _backgroundColorBrush = new(Colors.Transparent);

    partial void OnColorIdChanged(WorkspaceColorId value)
    {
        ColorBrush           = new SolidColorBrush(value.GetColor());
        BackgroundColorBrush = new SolidColorBrush(value.GetBackgroundColor());
    }

    public static WorkspaceViewModel FromWorkspace(Workspace workspace, Guid? selectedId)
    {
        var vm = new WorkspaceViewModel
        {
            Id         = workspace.Id,
            Name       = workspace.Name,
            ColorId    = workspace.ColorId,
            IsSelected = workspace.Id == selectedId
        };
        // Force brush init
        vm.ColorBrush           = new SolidColorBrush(workspace.ColorId.GetColor());
        vm.BackgroundColorBrush = new SolidColorBrush(workspace.ColorId.GetBackgroundColor());
        return vm;
    }
}
