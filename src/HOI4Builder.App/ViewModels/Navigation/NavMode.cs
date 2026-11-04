using System.Collections.Generic;

namespace HOI4Builder.App.ViewModels.Navigation;

public sealed class NavMode
{
    public NavMode(string title, IReadOnlyList<NavSection> sections)
    {
        Title = title;
        Sections = sections;
    }

    public string Title { get; }
    public IReadOnlyList<NavSection> Sections { get; }
}