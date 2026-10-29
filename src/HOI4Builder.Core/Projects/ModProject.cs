using HOI4Builder.Core.Editing;
using HOI4Builder.Core.Focuses;

namespace HOI4Builder.Core.Projects;

public sealed class ModProject
{
    public string Name { get; set; } = "Demo Mod";
    public string ModId { get; set; } = "demo_mod";

    public List<FocusTree> FocusTrees { get; } = new();

    // one history for the whole project, shared by every editor
    public UndoStack History { get; } = new();
}