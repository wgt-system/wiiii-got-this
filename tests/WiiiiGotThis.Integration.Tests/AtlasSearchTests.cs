using WiiiiGotThis.Application;
using WiiiiGotThis.Domain;
using WiiiiGotThis.Presentation;

namespace WiiiiGotThis.Integration.Tests;

public sealed class AtlasSearchTests
{
    [Fact]
    public void Find_ranks_title_matches_before_internal_node_id_matches()
    {
        var vocation = Node("service:vocation", AtlasNodeKind.Service, "Vocation", "vocation");
        var capability = Node(
            "capability:sample:internal-vocation-token",
            AtlasNodeKind.Capability,
            "Unrelated Capability",
            "sample",
            "sample.internal-vocation-token");

        var results = AtlasSearch.Find([capability, vocation], "voc");

        Assert.Equal(vocation, results[0]);
        Assert.Equal(capability, results[1]);
    }

    [Fact]
    public void Find_returns_map_projection_for_human_title_query()
    {
        var map = Node(
            "capability:vocation:vocation.map_projection",
            AtlasNodeKind.Capability,
            "Map Projection",
            "vocation",
            "vocation.map_projection");
        var vocation = Node("service:vocation", AtlasNodeKind.Service, "Vocation", "vocation");

        var results = AtlasSearch.Find([vocation, map], "map");

        Assert.Single(results);
        Assert.Equal(map, results[0]);
    }

    [Fact]
    public void Find_respects_result_limit_and_stable_title_order()
    {
        var nodes = new[]
        {
            Node("service:zeta", AtlasNodeKind.Service, "Alpha Search", "zeta"),
            Node("service:beta", AtlasNodeKind.Service, "Beta Search", "beta"),
            Node("service:gamma", AtlasNodeKind.Service, "Gamma Search", "gamma")
        };

        var results = AtlasSearch.Find(nodes, "search", limit: 2);

        Assert.Equal(2, results.Count);
        Assert.Equal("Alpha Search", results[0].Title);
        Assert.Equal("Beta Search", results[1].Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Find_returns_no_results_for_blank_query(string query)
    {
        var results = AtlasSearch.Find(
            [Node("service:vocation", AtlasNodeKind.Service, "Vocation", "vocation")],
            query);

        Assert.Empty(results);
    }

    private static AtlasNodePresentationViewModel Node(
        string nodeId,
        AtlasNodeKind kind,
        string title,
        string? serviceId = null,
        string? capabilityId = null) =>
        new(
            new AtlasNode(
                nodeId,
                kind,
                title,
                "Available",
                serviceId is null ? null : new ServiceIdentity(serviceId),
                capabilityId is null ? null : new CapabilityIdentity(capabilityId),
                IsEnabled: true,
                IsAvailable: true,
                IsIntegrated: true),
            0,
            0);
}
