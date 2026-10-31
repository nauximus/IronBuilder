using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Script;

namespace HOI4Builder.Core.Projects;

public sealed record ImportResult(ModProject Project, IReadOnlyList<string> Warnings);

public static class ModImporter
{
    public static ImportResult ImportFolder(string folder)
    {
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException($"The folder '{folder}' does not exist." );
        
        var warnings = new List<string>();
        string folderName = Path.GetFileName(Path.TrimEndingDirectorySeparator(folder));

        ModDescriptor? descriptor = null;
        var descriptorPath = Path.Combine(folder, "descriptor.mod");

        if (File.Exists(descriptorPath))
        {
            try
            {
                descriptor = ModDescriptor.Parse(File.ReadAllText(descriptorPath));
            }
            catch (PdxParseException ex)
            {
                warnings.Add($"descriptor.mod could not be read: {ex.Message}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                warnings.Add($"descriptor.mod could not be read: {ex.Message}");
            }
        }
        else
        {
            warnings.Add("No descriptor.mod was found, so the folder name is used as the mod name.");
        }

        var descriptorName = descriptor?.Name;

        var project = new ModProject
        {
            Name = string.IsNullOrWhiteSpace(descriptorName) ? folderName : descriptorName,
            ModId = ModIdRules.FromName(folderName),
            GameVersion = descriptor?.SupportedVersion ?? ModProject.DefaultGameVersion,
            ModVersion = descriptor?.Version,
            ImportFromFolder = folder,
        };

        if (descriptor != null)
            project.Tags.AddRange(descriptor.Tags);
        
        ImportFocusFiles(folder, project, warnings);

        return new ImportResult(project, warnings);
    }

    private static void ImportFocusFiles(string folder, ModProject project, List<string> warnings)
    {
        var focusDir = Path.Combine(folder, "common", "national_focus");
        if (!Directory.Exists(focusDir))
            return;
        
        var files = Directory.GetFiles(focusDir, "*.txt").OrderBy(f => f, StringComparer.Ordinal);

        foreach (var file in files)
        {
            string relative = Path.GetRelativePath(folder, file).Replace('\\', '/');

            try
            {
                var root = PdxParser.Parse(File.ReadAllText(file));

                foreach (var tree in FocusTreeMapper.ReadTrees(root))
                {
                    tree.SourceFile = relative;
                    project.FocusTrees.Add(tree);
                }

                var skipped = root.Entries
                    .Where(e => e.Key is not null and not "focus_tree")
                    .Select(e => e.Key!)
                    .ToList();
                
                if (skipped.Count > 0)
                {
                    warnings.Add(
                        $"{relative}: {skipped.Count} top-level entries other than focus_tree" +
                        $"({string.Join(", ", skipped.Distinct())}) were not imported.");
                }
            }
            catch (PdxParseException ex)
            {
                warnings.Add($"{relative}: {ex.Message}");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                warnings.Add($"{relative}: could not be read ({ex.Message})");
            }
        }
    }
}