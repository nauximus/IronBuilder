namespace HOI4Builder.Core.Focuses;

public static class FocusLinks
{
    public static bool HasPrerequisite(Focus target, string prerequisiteId) =>
        target.Prerequisites.Any(group => group.Contains(prerequisiteId));

    // true if making <paramref name="targetId/> require <paramref name="prerequisiteId"/> would form a loop.

    public static bool WouldCreateCycle(FocusTree tree, string prerequisiteId, string targetId)
    {
        if (prerequisiteId == targetId)
            return true;
        
        var visited = new HashSet<string>();
        var pending = new Stack<string>();
        pending.Push(prerequisiteId);

        while (pending.Count > 0)
        {
            var id = pending.Pop();
            if (!visited.Add(id))
                continue;
            
            var focus = tree.FindFocus(id);
            if (focus is null)
                continue;
            
            foreach (var group in focus.Prerequisites)
            {
                foreach (var required in group)
                {
                    if (required == targetId)
                        return true;
                    
                    pending.Push(required);
                }
            }
        }

        return false;
    }
}