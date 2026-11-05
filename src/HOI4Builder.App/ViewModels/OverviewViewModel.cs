using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HOI4Builder.Core.Editing;
using HOI4Builder.Core.Projects;

namespace HOI4Builder.App.ViewModels;

public sealed record CounterItem(string Label, int Count);

public sealed record WorldModeOption(WorldMode Mode, string Title, string Description);

public partial class OverviewViewModel : ViewModelBase
{
    private readonly ModProject _project;
    private string _modIdText;

    public OverviewViewModel(ModProject project)
    {
        _project = project;
        _modIdText = project.ModId;
        project.History.Changed += OnHistoryChanged;
        Refresh();
    }

    // ---- Content counters ----
    public ObservableCollection<CounterItem> Counters { get; } = new();

    // ---- Project settings (every edit goes through History, so it is undoable) ----
    public string ModName
    {
        get => _project.Name;
        set
        {
            var v = value?.Trim() ?? "";
            if (v.Length > 0)
                Apply("Change mod name", () => _project.Name, x => _project.Name = x, v);
            OnPropertyChanged();
        }
    }

    public string ModId
    {
        get => _modIdText;
        set
        {
            _modIdText = value ?? "";
            ModIdError = _modIdText.Length == 0 ? "The mod id cannot be empty." : ModIdRules.Validate(_modIdText);

            if (ModIdError is null)
                Apply("Change mod id", () => _project.ModId, x => _project.ModId = x, _modIdText);

            OnPropertyChanged();
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasModIdError))]
    private string? _modIdError;

    public bool HasModIdError => ModIdError != null;

    public string GameVersion
    {
        get => _project.GameVersion;
        set
        {
            var v = value?.Trim() ?? "";
            if (v.Length > 0)
                Apply("Change game version", () => _project.GameVersion, x => _project.GameVersion = x, v);
            OnPropertyChanged();
        }
    }

    public string ExportFolder
    {
        get => _project.ExportFolder ?? "";
        set => Apply("Change export folder", () => _project.ExportFolder, x => _project.ExportFolder = x, Clean(value));
    }

    public string Hoi4InstallFolder
    {
        get => _project.GameInstallFolder ?? "";
        set => Apply("Change HOI4 install folder", () => _project.GameInstallFolder, x => _project.GameInstallFolder = x, Clean(value));
    }

    public string ImportedModFolder
    {
        get => _project.ImportFromFolder ?? "";
        set => Apply("Change imported mod folder", () => _project.ImportFromFolder, x => _project.ImportFromFolder = x, Clean(value));
    }

    public bool IsSubmod
    {
        get => _project.isSubmod;
        set => Apply("Change project type", () => _project.isSubmod, x => _project.isSubmod = x, value);
    }

    public IReadOnlyList<WorldModeOption> WorldModes { get; } = new[]
    {
        new WorldModeOption(WorldMode.StandardMod, "Standard mod",
            "Adds or changes content on top of the base game world."),
        new WorldModeOption(WorldMode.BlankSlateOverhaul, "Blank-slate overhaul",
            "Starts from an empty world instead of the base game setup."),
        new WorldModeOption(WorldMode.ResetContent, "Reset content",
            "Clears base game content so you can rebuild it yourself."),
        new WorldModeOption(WorldMode.KeepCountriesAndBorders, "Keep countries and borders",
            "Keeps the existing countries and map, resets the rest."),
        new WorldModeOption(WorldMode.RandomizedPoliticalWorld, "Randomized political world",
            "Generates a random political layout. Preview it, then apply."),
    };

    public WorldModeOption SelectedWorldMode
    {
        get => WorldModes.First(o => o.Mode == _project.WorldMode);
        set
        {
            if (value is null)
                return;

            Apply("Change world mode", () => _project.WorldMode, x => _project.WorldMode = x, value.Mode);
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsRandomizedWorld));
        }
    }

    public bool IsRandomizedWorld => _project.WorldMode == WorldMode.RandomizedPoliticalWorld;

    // ---- Project health ----
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private bool _isReady;
    [ObservableProperty] private bool _hasWarnings;
    [ObservableProperty] private bool _isBlocked;
    [ObservableProperty] private int _warningCount;
    [ObservableProperty] private int _authoredItemCount;
    [ObservableProperty] private bool _showIssues;

    public ObservableCollection<string> Issues { get; } = new();

    [RelayCommand]
    private void ReviewValidation()
    {
        Refresh();
        ShowIssues = true;
    }

    [RelayCommand]
    private void ResetAdvancedMetadata()
        => _project.History.Execute(new ResetAdvancedMetadataCommand(_project));

    // ---- Internals ----
    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void Apply<T>(string description, Func<T> get, Action<T> set, T value)
    {
        if (EqualityComparer<T>.Default.Equals(get(), value))
            return;

        _project.History.Execute(new PropertyChangeCommand<T>(description, get, set, value));
    }

    // Runs after every edit, undo or redo anywhere in the project, so counters stay live.
    private void OnHistoryChanged()
    {
        if (ModIdError is null)
            _modIdText = _project.ModId;

        OnPropertyChanged(string.Empty);
        Refresh();
    }

    private void Refresh()
    {
        Counters.Clear();
        Counters.Add(new CounterItem("Focus trees", _project.FocusTrees.Count));
        Counters.Add(new CounterItem("Focuses", _project.FocusTrees.Sum(t => t.Focuses.Count())));
        Counters.Add(new CounterItem("Focus systems", 0));
        Counters.Add(new CounterItem("Countries", _project.Countries.Count));
        Counters.Add(new CounterItem("Characters", 0));
        Counters.Add(new CounterItem("Starting forces", 0));

        var report = ProjectHealth.Evaluate(_project);

        WarningCount = report.Warnings.Count;
        AuthoredItemCount = report.AuthoredItems;

        IsBlocked = report.Errors.Count > 0;
        HasWarnings = !IsBlocked && report.Warnings.Count > 0;
        IsReady = !IsBlocked && !HasWarnings;

        StatusText = IsBlocked ? $"Not ready to export: {report.Errors.Count} error(s)"
                   : HasWarnings ? $"Ready to export, {report.Warnings.Count} warning(s)"
                   : "Ready to export";

        Issues.Clear();
        foreach (var e in report.Errors) Issues.Add("Error: " + e);
        foreach (var w in report.Warnings) Issues.Add("Warning: " + w);
        if (Issues.Count == 0) Issues.Add("No issues found.");
    }
}