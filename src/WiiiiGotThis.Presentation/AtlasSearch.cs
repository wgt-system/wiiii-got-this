namespace WiiiiGotThis.Presentation;

public static class AtlasSearch
{
    public static IReadOnlyList<AtlasNodePresentationViewModel> Find(
        IEnumerable<AtlasNodePresentationViewModel> nodes,
        string? query,
        int limit = 6)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

        var normalized = query?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return [];

        return nodes
            .Select(node => (Node: node, Score: MatchScore(node, normalized)))
            .Where(match => match.Score < int.MaxValue)
            .OrderBy(match => match.Score)
            .ThenBy(match => match.Node.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(match => match.Node.NodeId, StringComparer.Ordinal)
            .Take(limit)
            .Select(match => match.Node)
            .ToArray();
    }

    private static int MatchScore(AtlasNodePresentationViewModel node, string query)
    {
        if (string.Equals(node.Title, query, StringComparison.OrdinalIgnoreCase))
            return 0;
        if (node.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return 10;
        if (node.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 20;
        if (string.Equals(node.NodeId, query, StringComparison.OrdinalIgnoreCase))
            return 30;
        if (node.NodeId.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return 40;
        if (node.NodeId.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 50;
        if (node.KindLabel.Contains(query, StringComparison.OrdinalIgnoreCase))
            return 60;
        return int.MaxValue;
    }
}
