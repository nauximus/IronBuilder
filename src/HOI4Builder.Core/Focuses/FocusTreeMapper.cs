using System.Globalization;
using HOI4Builder.Core.Script;
using Microsoft.VisualBasic;

namespace HOI4Builder.Core.Focuses;

public static class FocusTreeMapper
{
    // ----- Import
    public static List<FocusTree> ReadTrees(PdxBlock root)
    {
        var trees = new List<FocusTree>();
        foreach (var entry in root.FindAll("focus_tree"))
        {
            if (entry.Value is PdxBlock block)
                trees.Add(TreeFromBlock(block));
        }
        return trees;
    }

    public static FocusTree TreeFromBlock(PdxBlock block)
    {
        var tree = new FocusTree();

        foreach (var entry in block.Entries)
        {
            switch (entry.Key)
            {
                case "id" when entry.Value is PdxScalar s:
                    tree.Id = s.Text;
                    break;
                case "country" when entry.Value is PdxBlock b:
                    tree.Country = b;
                    break;
                case "default" when entry.Value is PdxScalar s:
                    tree.IsDefault = s.IsYes;
                    break;
                case "focus" when entry.Value is PdxBlock b:
                    tree.Focuses.Add(FocusFromBlock(b));
                    break;
                case "shared_focus" when entry.Value is PdxScalar s:
                    tree.SharedFocusIds.Add(s.Text);
                    break;
                default:
                    tree.ExtraEntries.Add(entry);
                    break;
            }
        }

        return tree;
    }

    private static Focus FocusFromBlock(PdxBlock block)
    {
        var focus = new Focus();

        foreach (var entry in block.Entries)
        {
            switch (entry.Key)
            {
                case "id" when entry.Value is PdxScalar s:
                    focus.Id = s.Text;
                    break;
                case "icon" when entry.Value is PdxScalar s:
                    focus.Icon = s.Text;
                    break;
                case "x" when entry.Value is PdxScalar s && s.TryGetInt(out var xv):
                    focus.X = xv;
                    break;
                case "y" when entry.Value is PdxScalar s && s.TryGetInt(out var yv):
                    focus.Y = yv;
                    break;
                case "relative_position_id" when entry.Value is PdxScalar s:
                    focus.RelativePositionId = s.Text;
                    break;
                case "cost" when entry.Value is PdxScalar s && s.TryGetDouble(out var cv):
                    focus.Cost = cv;
                    break;
                case "prerequisite" when entry.Value is PdxBlock b:
                    focus.Prerequisites.Add(FocusIds(b));
                    break;
                case "mutually_exclusive" when entry.Value is PdxBlock b:
                    focus.MutuallyExclusive.AddRange(FocusIds(b));
                    break;
                case "search_filters" when entry.Value is PdxBlock b:
                    focus.SearchFilters.AddRange(b.BareScalars());
                    break;
                case "available" when entry.Value is PdxBlock b:
                    focus.Available = b;
                    break;
                case "bypass" when entry.Value is PdxBlock b:
                    focus.Bypass = b;
                    break;
                case "allow_branch" when entry.Value is PdxBlock b:
                    focus.AllowBranch = b;
                    break;
                case "completion_reward" when entry.Value is PdxBlock b:
                    focus.CompletionReward = b;
                    break;
                case "ai_will_do" when entry.Value is PdxBlock b:
                    focus.AiWillDo = b;
                    break;
                default:
                    focus.ExtraEntries.Add(entry);
                    break;
            }
        }

        return focus;
    }

    private static List<string> FocusIds(PdxBlock block) =>
        block.FindAll("focus")
            .Select(e => e.Value)
            .OfType<PdxScalar>()
            .Select(s => s.Text)
            .ToList();

        
    // ----- Export ----

    public static PdxBlock ToDocument(IEnumerable<FocusTree> trees)
    {
        var root = new PdxBlock();
        foreach (var tree in trees)
            root.Entries.Add(new PdxEntry("focus_tree", TreeToBlock(tree)));
        return root;
    }

    public static PdxBlock TreeToBlock(FocusTree tree)
    {
        var block = new PdxBlock();
        
        block.Add("id", tree.Id);
        AddRaw(block, "country", tree.Country);
        if (tree.IsDefault)
            block.Add("default", "yes");

        foreach (var id in tree.SharedFocusIds)
            block.Add("shared_focus",id);

        foreach (var focus in tree.Focuses)
            block.Entries.Add(new PdxEntry("focus", FocusToBlock(focus)));

        block.Entries.AddRange(tree.ExtraEntries);
        return block;
    }

    public static PdxBlock FocusToBlock(Focus focus)
    {
        var block = new PdxBlock();

        block.Add("id", focus.Id);
        if (focus.Icon != null)
            block.Add("icon", focus.Icon);
        
        block.Add("x", focus.X.ToString(CultureInfo.InvariantCulture));
        block.Add("y", focus.Y.ToString(CultureInfo.InvariantCulture));

        if (focus.RelativePositionId != null)
        {
            block.Add("relative_position_id", focus.RelativePositionId);
        }

        if (focus.Cost != null)
        {
            block.Add("cost", focus.Cost.Value.ToString(CultureInfo.InvariantCulture));
        }

        foreach (var group in focus.Prerequisites)
        {
            var prerequisite = block.AddBlock("prerequisite");
            foreach (var id in group)
                prerequisite.Add("focus", id);
        }

        if (focus.MutuallyExclusive.Count > 0)
        {
            var exclusive = block.AddBlock("mutually_exclusive");
            foreach (var id in focus.MutuallyExclusive)
                exclusive.Add("focus", id);
        }

        if (focus.SearchFilters.Count > 0)
        {
            var filters = block.AddBlock("search_filters");
            foreach (var filter in focus.SearchFilters)
                filters.AddBare(filter);
        }

        AddRaw(block, "available", focus.Available);
        AddRaw(block, "bypass", focus.Bypass);
        AddRaw(block, "allow_branch", focus.AllowBranch);
        AddRaw(block, "ai_will_do", focus.AiWillDo);
        AddRaw(block, "completion_reward", focus.CompletionReward);

        block.Entries.AddRange(focus.ExtraEntries);
        return block;
    }

    private static void AddRaw(PdxBlock parent, string key, PdxBlock? value)
    {
        if (value != null)
            parent.Entries.Add(new PdxEntry(key, value));
    }
}