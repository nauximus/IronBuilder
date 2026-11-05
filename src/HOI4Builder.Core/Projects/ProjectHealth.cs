namespace HOI4Builder.Core.Projects;

public sealed record ProjectHealthReport(
        IReadOnlyList<string> Errors,
        IReadOnlyList<string> Warnings,
        int AuthoredItems);

public static class ProjectHealth
{
    public static ProjectHealthReport Evaluate(ModProject project)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(project.Name))
            errors.Add("The mod name is empty.");

        var idError = ModIdRules.Validate(project.ModId);
        if (idError != null)
            errors.Add(idError);
        
        if (string.IsNullOrWhiteSpace(project.GameVersion))
            warnings.Add("The game version is empty.");

        if (!string.IsNullOrWhiteSpace(project.ExportFolder) && !Directory.Exists(project.GameInstallFolder))
            warnings.Add("The HOI4 install folder does not exist.");

        if (project.isSubmod is true && !string.IsNullOrWhiteSpace(project.ImportFromFolder) && !Directory.Exists(project.ImportFromFolder))
            warnings.Add("The imported mod folder does not exist.");

        int unassigned = project.UnassignedTrees().Count;
        if (unassigned > 0)
            warnings.Add($"{unassigned} focus tree(s) are not assigned to a country.");

        int authored = project.Countries.Count
            + project.FocusTrees.Count
            + project.FocusTrees.Sum(t => t.Focuses.Count());

        return new ProjectHealthReport(errors, warnings, authored);
    }
}