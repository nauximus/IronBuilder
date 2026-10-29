namespace HOI4Builder.Core.Focuses;

public readonly record struct GridPoint(int X, int Y);

public static class FocusLayout
{
    public static Dictionary<string, GridPoint> ResolvePositions(FocusTree tree)
    {
        var byId = new Dictionary<string, Focus>();
        foreach (var focus in tree.Focuses)
            byId[focus.Id] = focus;

        var result = new Dictionary<string, GridPoint>();
        foreach (var focus in tree.Focuses)
            Resolve(focus, byId, result, new HashSet<string>());
        return result;
    }

    private static GridPoint Resolve(
        Focus focus,
        Dictionary<string, Focus> byId,
        Dictionary<string, GridPoint> cache,
        HashSet<string> visiting)
    {
        if (cache.TryGetValue(focus.Id, out var done))
            return done;

        var own = new GridPoint(focus.X, focus.Y);

        if (focus.RelativePositionId is null
            || !byId.TryGetValue(focus.RelativePositionId, out var anchor)
            || !visiting.Add(focus.Id))
        {
            cache[focus.Id] = own;
            return own;
        }

        var anchorPoint = Resolve(anchor, byId, cache, visiting);
        visiting.Remove(focus.Id);

        var point = new GridPoint(anchorPoint.X + focus.X, anchorPoint.Y + focus.Y);
        cache[focus.Id] = point;
        return point;
    }

    public static GridPoint ToOwnCoordinates(
        Focus focus,
        IReadOnlyDictionary<string, GridPoint> positions,
        GridPoint absolute)
    {
        if (focus.RelativePositionId is not null
            && positions.TryGetValue(focus.RelativePositionId, out var anchor))
        {
            return new GridPoint(absolute.X - anchor.X, absolute.Y - anchor.Y);
        }

        return absolute;
    }
}