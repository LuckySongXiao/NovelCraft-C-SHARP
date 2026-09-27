using System;
using System.Linq;
using NovelManagement.Application.Services;
using Xunit;

namespace NovelManagement.Tests;

/// <summary>
/// ForceDirectedGraphLayout 单元测试：确定性、边界约束、收敛质量（社群聚簇）。
/// </summary>
public class ForceDirectedGraphLayoutTests
{
    private static double Dist((double X, double Y) a, (double X, double Y) b) =>
        Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    [Fact]
    public void Compute_EmptyGraph_ReturnsEmpty()
    {
        var result = ForceDirectedGraphLayout.Compute(
            Array.Empty<string>(), Array.Empty<(string, string)>(), 1200, 800);
        Assert.Empty(result.Positions);
    }

    [Fact]
    public void Compute_SingleNode_Centered()
    {
        var result = ForceDirectedGraphLayout.Compute(
            new[] { "a" }, Array.Empty<(string, string)>(), 1200, 800);
        var pos = Assert.Contains("a", result.Positions);
        Assert.Equal(600, pos.X, 0);
        Assert.Equal(400, pos.Y, 0);
    }

    [Fact]
    public void Compute_AllNodesWithinCanvasBounds()
    {
        var nodes = Enumerable.Range(0, 30).Select(i => $"n{i}").ToArray();
        var edges = Enumerable.Range(0, 29).Select(i => ($"n{i}", $"n{i + 1}")).ToArray();
        var result = ForceDirectedGraphLayout.Compute(nodes, edges, 1200, 800);
        Assert.Equal(30, result.Positions.Count);
        foreach (var (x, y) in result.Positions.Values)
        {
            Assert.InRange(x, 60, 1140);
            Assert.InRange(y, 60, 740);
        }
    }

    [Fact]
    public void Compute_Deterministic_SameInputSameOutput()
    {
        var nodes = Enumerable.Range(0, 15).Select(i => $"n{i}").ToArray();
        var edges = new[] { ("n0", "n1"), ("n1", "n2"), ("n2", "n0"), ("n3", "n4") };
        var r1 = ForceDirectedGraphLayout.Compute(nodes, edges, 1200, 800);
        var r2 = ForceDirectedGraphLayout.Compute(nodes, edges, 1200, 800);
        foreach (var kv in r1.Positions)
        {
            Assert.Equal(kv.Value, r2.Positions[kv.Key]);
        }
    }

    [Fact]
    public void Compute_ConnectedPairCloserThanDisconnectedPair()
    {
        // 链式图 a—b—c：b 与两端相连，a-c 间无边。收敛后 a-c 距离应大于 a-b（无斥力抵消时）
        var nodes = new[] { "a", "b", "c" };
        var edges = new[] { ("a", "b"), ("b", "c") };
        var result = ForceDirectedGraphLayout.Compute(nodes, edges, 1200, 800);
        var dAb = Dist(result.Positions["a"], result.Positions["b"]);
        var dAc = Dist(result.Positions["a"], result.Positions["c"]);
        Assert.True(dAc > dAb, $"a-c ({dAc:F1}) 应大于 a-b ({dAb:F1})");
    }

    [Fact]
    public void Compute_ClustersCommunity_DenseGroupCloserThanOutsider()
    {
        // 三角社群 {a,b,c} 两两相连 + 孤立节点 x：社群内平均距离应小于社群到 x 的平均距离
        var nodes = new[] { "a", "b", "c", "x" };
        var edges = new[] { ("a", "b"), ("b", "c"), ("a", "c") };
        var result = ForceDirectedGraphLayout.Compute(nodes, edges, 1200, 800);
        var p = result.Positions;
        var intra = new[] { Dist(p["a"], p["b"]), Dist(p["b"], p["c"]), Dist(p["a"], p["c"]) }.Average();
        var inter = new[] { Dist(p["x"], p["a"]), Dist(p["x"], p["b"]), Dist(p["x"], p["c"]) }.Average();
        Assert.True(inter > intra, $"社群外平均距离 ({inter:F1}) 应大于社群内 ({intra:F1})");
    }

    [Fact]
    public void Compute_SparseNodes_DoNotOverlap()
    {
        // 10 个无关节点：斥力应将它们彼此推开（节点半径约 25px，间距应显著大于 0）
        var nodes = Enumerable.Range(0, 10).Select(i => $"n{i}").ToArray();
        var result = ForceDirectedGraphLayout.Compute(
            nodes, Array.Empty<(string, string)>(), 1200, 800, iterations: 500);
        var positions = result.Positions.Values.ToArray();
        var minDist = double.MaxValue;
        for (var i = 0; i < positions.Length; i++)
        {
            for (var j = i + 1; j < positions.Length; j++)
            {
                minDist = Math.Min(minDist, Dist(positions[i], positions[j]));
            }
        }
        Assert.True(minDist > 30, $"最小节点间距 {minDist:F1} 应大于 30（节点不重叠）");
    }

    [Fact]
    public void Compute_EdgesToUnknownNodes_AreIgnored()
    {
        var result = ForceDirectedGraphLayout.Compute(
            new[] { "a", "b" }, new[] { ("a", "ghost"), ("phantom", "b") }, 1200, 800);
        Assert.Equal(2, result.Positions.Count);
        Assert.DoesNotContain("ghost", result.Positions);
        Assert.DoesNotContain("phantom", result.Positions);
    }
}
