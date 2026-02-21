using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Arcmark.Models;

namespace Arcmark.ViewModels;

/// <summary>
/// ViewModel for a single workspace row in the Settings panel.
/// </summary>
public partial class WorkspaceSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private WorkspaceColorId _colorId;

    [ObservableProperty]
    private bool _isRenaming;

    [ObservableProperty]
    private int _itemCount;

    [ObservableProperty]
    private SolidColorBrush _colorBrush = Brushes.LightBlue;

    partial void OnColorIdChanged(WorkspaceColorId value)
    {
        var color = value.GetColor();
        ColorBrush = new SolidColorBrush(color);
    }

    public WorkspaceSettingsViewModel() { }

    public WorkspaceSettingsViewModel(Arcmark.Models.Workspace workspace)
    {
        _id = workspace.Id;
        _name = workspace.Name;
        _colorId = workspace.ColorId;
        _itemCount = CountItems(workspace.Items);
        var color = workspace.ColorId.GetColor();
        _colorBrush = new SolidColorBrush(color);
    }

    private static int CountItems(IList<Arcmark.Models.Node> nodes)
    {
        int count = 0;
        foreach (var node in nodes)
        {
            count++;
            if (node is Arcmark.Models.FolderNode fn)
                count += CountItems(fn.Folder.Children);
        }
        return count;
    }
}
