using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Projects;

namespace HOI4Builder.App.ViewModels;

public partial class FocusEditorViewModel : ViewModelBase
{
    public FocusEditorViewModel(ModProject project)
    {
        Project = project;
        SelectedTree = project.FocusTrees.FirstOrDefault();
    }

    public ModProject Project { get; }

    [ObservableProperty]
    private FocusTree? _selectedTree;
}