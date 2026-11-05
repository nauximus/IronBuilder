using System.Collections.Generic;
using System.Threading;
using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Projects;

namespace HOI4Builder.App.ViewModels.Navigation;

public static class NavigationCatalog
{
    public static IReadOnlyList<NavMode> Build(ModProject? project, CountrySelection? selection)
    {
        var modes =  new[]
        {
            new NavMode("Mod editors", new[]
            {
                new NavSection("Project", new[]
                {
                    project is null
                        ? Placeholder("overview", "Overview", "Project setup - status")
                        : new NavPage("overview", "Overview", "Project setup - status", new OverviewViewModel(project)),
                    Placeholder("planning", "Planning", "Project boards, notes"),
                    Placeholder("gameplay-connections", "Gameplay connections", "Activations, requirements & missing links"),
                    Placeholder("debug-tool", "Debug", "Live testing, logs & help"),
                }),
                new NavSection("World Setup", new[]
                {
                    project is null || selection is null ? Placeholder("countries", "Countries", "Custom & base game")
                        : new NavPage("countries", "Countries", "Custom & base game",
                            new CountriesViewModel(project, selection)),
                    Placeholder("map-states", "Map & states", "Map editor, state owners, cores"),
                    Placeholder("map-editor", "Map Editor", "Maps, state owners, cores"),
                    Placeholder("game-setup", "Game setup", "Scenarios & featured countries"),
                    Placeholder("generic-content", "Generic content", "Fallback names & countries")
                }),
                new NavSection("Politics", new[]
                {
                    Placeholder("ideologies", "Ideologies", "Groups & subtypes"),
                    Placeholder("factions", "Factions", "Political factions & goals"),
                    Placeholder("balance-of-power", "Balance of power", "Sides, decisions"),
                    Placeholder("custom-laws", "Custom laws", "Law groups, benefits & costs"),
                }),
                new NavSection("Country", new[]
                {
                    project is null || selection is null ?
                        Placeholder("focus-tree", "Focus tree", "Visual node editor")
                        : new NavPage("focus-tree", "Focus tree", "Visual node editor",
                            new FocusEditorViewModel(project, selection)),
                    Placeholder("characters", "Characters", "Leaders & advisors"),
                    Placeholder("traits", "Traits", "Political & commander traits"),
                    Placeholder("national-spirits", "National spirits", "Ideas & modifiers"),
                    Placeholder("events", "Events", "Country & news events"),
                    Placeholder("decisions", "Decisions", "Categories & decisions"),
                }),
                new NavSection("Military & Industry", new[]
                {
                    Placeholder("starting-forces", "Starting forces", "Divisions, air wings & fleets"),
                    Placeholder("technologies", "Technologies", "Research & doctrines"),
                    Placeholder("equipment", "Equipment", "Archetypes & variants"),
                    Placeholder("resources", "Resources", "Strategic resources & state deposits"),
                    Placeholder("intelligence", "Intelligence", "Agencies, operatives & operations"),
                }),
                new NavSection("AI & Systems", new[]
                {
                    Placeholder("ai-strategy", "AI strategy", "Diplomacy, production & focus plans"),
                    Placeholder("peace-deal-ai", "Peace deal AI", ""),
                    Placeholder("advanced-systems", "Advanced systems", "Reusable scripts & on-actions"),
                }),
                new NavSection("Project files", new[]
                {
                    Placeholder("localisation", "Localisation", "Translation table & languages"),
                    Placeholder("mod-files", "Mod files", "Create, edit & preserve every file"),
                    Placeholder("coverage-matrix", "Coverage matrix", "Supported & missing"),
                }),
            }),

            new NavMode("Builders", new[]
            {
                new NavSection("Interfaces & Layouts", new[]
                {
                    Placeholder("gui-builder", "GUI builder", "Buttons, windows & dashboards inside the game"),
                    Placeholder("diplomacy-ui", "Diplomacy UI", "Country panel & diplomatic actions"),
                    Placeholder("focus-inlays", "Focus inlays", "Council artwork & panels"),
                    Placeholder("super-events", "Super events", "Scripted-GUI cinematics"),
                }),
                new NavSection("Gameplay Systems", new[]
                {
                    Placeholder("civil-wars", "Civil wars & uprisings", "Sides, forces & victory effects"),
                    Placeholder("formable-nations", "Formable nations", "Territory, identity & forces"),
                    Placeholder("political-coalitions", "Political coalitions", "Partners, concessions & confidence"),
                }),
                new NavSection("Artwork & Audio", new[]
                {
                    Placeholder("portrait-studio", "Portrait studio", "HOI4 character portraits"),
                    Placeholder("gfx-builder", "GFX builder", "Focus & spirit artwork"),
                    Placeholder("music-stations", "Music stations", "Custom radio stations & songs"),
                    Placeholder("map-graphics", "Map graphics", "Terrain, water, borders & shaders"),
                }),
            }),
        };

        if (project is null)
        {
            foreach (var mode in modes)
                foreach (var section in mode.Sections)
                    foreach (var page in section.Pages)
                        page.IsEnabled = section.Title == "Project";
        }

        return modes;
    }

    private static NavPage Placeholder(string id, string title, string subtitle) =>
        new(id, title, subtitle, new PlaceholderEditorViewModel(title));
}