using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using HOI4Builder.Core.Focuses;
using HOI4Builder.Core.Script;

namespace HOI4Builder.Core.Projects;


public sealed record ExportResult(
    string ModFolder,
    IReadOnlyList<string> Written,
    int Unchanged,
    IReadOnlyList<string> Warnings);

public static class ModExporter
{
    // descriptor and script files are written without a byte order mark, localisation files with one.
    private static readonly UTF8Encoding NoBom = new(false);
    private static readonly UTF8Encoding WithBom = new(true);

    public static ExportResult Export(ModProject project)
    {
        if (string.IsNullOrWhiteSpace(project.ExportFolder))
            throw new InvalidOperationException("Choose an export folder first.");

        var idError = ModIdRules.Validate(project.ModId);
        if (idError != null)
            throw new InvalidOperationException(idError);
        
        var root = Path.GetFullPath(project.ExportFolder);
        var modFolder = Path.Combine(root, project.ModId);
        Directory.CreateDirectory(modFolder);

        var written = new List<string>();
        var warnings = new List<string>();
        int unchanged = 0;


        void Write(string path, string text, Encoding encoding)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            if (File.Exists(path))
            {
                if (File.ReadAllText(path) == text)
                {
                    unchanged++;
                    return;
                }

                File.Copy(path, path + ".bak", overwrite: true);
            }

            File.WriteAllText(path, text, encoding);
            written.Add(Path.GetRelativePath(root, path).Replace('\\', '/'));
        }

        Write(Path.Combine(modFolder, "descriptor.mod"), BuildDescriptor(project, null), NoBom);
        Write(Path.Combine(root, project.ModId + ".mod"),
            BuildDescriptor(project, LauncherPath(root, modFolder, project.ModId)), NoBom);
        
        foreach (var group in project.FocusTrees.GroupBy(t => t.SourceFile ?? DefaultFocusPath(t)))
        {
            if (project.ProtectedFiles.Contains(group.Key))
            {
                warnings.Add(
                    $"{group.Key} was not written: it also holds blocks the tool cannot keep yet " +
                    "(such as shared_focus). Copy that file from the original mod by hand.");
                continue;
            }

            var path = SafeCombine(modFolder, group.Key);
            Write(path, PdxWriter.Write(FocusTreeMapper.ToDocument(group)), NoBom);
        }

        var localisation = BuildFocusLocalisation(project);
        if (localisation != null)
        {
            Write(
                Path.Combine(modFolder, "localisation", "english", project.ModId + "_focuses_l_english.yml"),
                localisation,
                WithBom);
        }

        return new ExportResult(modFolder, written, unchanged, warnings);
    } 

    private static string DefaultFocusPath(FocusTree tree) => 
        $"common/national_focus/{ModIdRules.FromName(tree.Id)}.txt";

    // the launcher accepts mod/<id> when the export folder is the game's mod folder
    private static string LauncherPath(string root, string modFolder, string modId) =>
        string.Equals(Path.GetFileName(root.TrimEnd('/', '\\')), "mod", StringComparison.OrdinalIgnoreCase)
        ? "mod/" + modId
        : modFolder.Replace('\\', '/');

    private static string BuildDescriptor(ModProject project, string? launcherPath)
    {
        var sb = new StringBuilder();
        sb.Append("version=").Append(Quote(project.ModVersion ?? "1.0")).Append('\n');

        if (project.Tags.Count > 0)
        {
            sb.Append("tags={\n");
            foreach (var tag in project.Tags)
                sb.Append('\t').Append(Quote(tag)).Append('\n');
            sb.Append("}\n");
        }

        sb.Append("name=").Append(Quote(project.Name)).Append('\n');
        sb.Append("supported_version=").Append(Quote(project.GameVersion)).Append('\n');

        if (launcherPath != null)
            sb.Append("path=").Append(Quote(launcherPath)).Append('\n');

        return sb.ToString();
    }

    private static string Quote(string text) => "\"" + text.Replace("\"", "'") + "\"";

    private static string? BuildFocusLocalisation(ModProject project)
    {
        var lines = new List<string> { "l_english:" };

        foreach (var focus in project.FocusTrees.SelectMany(t => t.Focuses))
        {
            if (!string.IsNullOrWhiteSpace(focus.Name))
                lines.Add($" {focus.Id}:0 \"{LocText(focus.Name)}\"");

            if (!string.IsNullOrWhiteSpace(focus.Description))
                lines.Add($" {focus.Id}_desc:0 \"{LocText(focus.Description)}\"");
        }

        return lines.Count == 1 ? null : string.Join("\n", lines) + "\n";
    }

    private static string LocText(string text) => text.Replace("\r\n", "\n").Replace("\n", "\\n");

    // a project file is untrusted input. stored path must never lead outside the mod folder.
    private static string SafeCombine(string folder, string relative)
    {
        var full = Path.GetFullPath(Path.Combine(folder, relative.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!full.StartsWith(prefix, StringComparison.Ordinal))
            throw new InvalidDataException($"The path '{relative}' points outside the mod folder.");
        
        return full;
    }
}