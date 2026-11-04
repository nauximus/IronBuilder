using System.Collections.Generic;

namespace HOI4Builder.App.ViewModels.Navigation;


public sealed class NavSection
{
    public NavSection(string title, IReadOnlyList<NavPage> pages)
    {
        Title = title;
        Pages = pages;
    }

    public string Title { get; }
    public IReadOnlyList<NavPage> Pages { get; }
}