using System;
using System.Collections.Generic;

namespace NovelManagement.Application.Services;

/// <summary>
/// 确定性力导向图布局（Fruchterman–Reingold 算法）。
/// <para>
/// 用于关系网络视图等图可视化场景：节点间库仑斥力（k²/d）+ 边弹簧引力（d²/k），
/// 温度冷却收敛，初始位置取黄金角圆环分布（无随机源，同输入必得同输出）。
/// 相比圆形布局，能直观呈现社群结构（关系密集的角色自然聚簇）。
/// </para>
/// </summary>
public static class ForceDirectedGraphLayout
{
    /// <summary>布局结果：节点 ID → 坐标（以画布左上角为原点）。</summary>
    public sealed class LayoutResult
    {
        /// <summary>节点坐标表。</summary>
        public Dictionary<string, (double X, double Y)> Positions { get; } = new();
    }

    /// <summary>
    /// 计算力导向布局。
    /// </summary>
    /// <param name="nodeIds">节点 ID 列表（不可为 null；可为空集合）。</param>
    /// <param name="edges">无向边列表（端点不必存在于 nodeIds 中的边会被忽略）。</param>
    /// <param name="width">画布宽度。</param>
    /// <param name="height">画布高度。</param>
    /// <param name="iterations">迭代轮数（默认 300，越多收敛越充分）。</param>
    /// <param name="margin">节点与画布边界的最小间距（默认 60，为节点圆与名称标签留空间）。</param>
    /// <returns>布局结果；空图返回空坐标表。</returns>
    public static LayoutResult Compute(
        IReadOnlyList<string> nodeIds,
        IReadOnlyList<(string From, string To)> edges,
        double width,
        double height,
        int iterations = 300,
        double margin = 60)
    {
        var result = new LayoutResult();
        var n = nodeIds?.Count ?? 0;
        if (n == 0 || width <= 0 || height <= 0)
        {
            return result;
        }

        // 单节点：居中即可
        if (n == 1)
        {
            result.Positions[nodeIds[0]] = (width / 2, height / 2);
            return result;
        }

        var usableW = Math.Max(width - 2 * margin, 1);
        var usableH = Math.Max(height - 2 * margin, 1);

        // 索引化：算法内部用数组，输出时映射回 ID
        var index = new Dictionary<string, int>(n);
        var xs = new double[n];
        var ys = new double[n];
        for (var i = 0; i < n; i++)
        {
            index[nodeIds[i]] = i;
        }

        // 初始位置：黄金角圆环分布（确定性，比均匀圆环更不容易陷入对称僵局）
        var centerX = margin + usableW / 2;
        var centerY = margin + usableH / 2;
        var radius = Math.Min(usableW, usableH) / 2 * 0.85;
        const double goldenAngle = 2.399963229728653; // π * (3 - √5)
        for (var i = 0; i < n; i++)
        {
            var angle = goldenAngle * i;
            xs[i] = centerX + radius * Math.Cos(angle);
            ys[i] = centerY + radius * Math.Sin(angle);
        }

        // 边索引化（忽略指向不存在节点的边）
        var edgeList = new List<(int A, int B)>();
        if (edges != null)
        {
            foreach (var (from, to) in edges)
            {
                if (from != null && to != null && index.TryGetValue(from, out var a) && index.TryGetValue(to, out var b) && a != b)
                {
                    edgeList.Add((a, b));
                }
            }
        }

        // FR 参数：理想边长 k = C * sqrt(area / n)
        var area = usableW * usableH;
        var k = Math.Sqrt(area / n) * 0.9;
        var kSquared = k * k;

        var minX = margin;
        var minY = margin;
        var maxX = width - margin;
        var maxY = height - margin;

        var dispX = new double[n];
        var dispY = new double[n];
        var temperature = Math.Min(usableW, usableH) / 8; // 初始温度
        var minTemperature = temperature * 0.02;
        var cooling = Math.Pow(minTemperature / temperature, 1.0 / Math.Max(iterations, 1));

        for (var iter = 0; iter < iterations; iter++)
        {
            Array.Clear(dispX, 0, n);
            Array.Clear(dispY, 0, n);

            // 斥力：所有节点对
            for (var i = 0; i < n; i++)
            {
                for (var j = i + 1; j < n; j++)
                {
                    var dx = xs[i] - xs[j];
                    var dy = ys[i] - ys[j];
                    var distSq = dx * dx + dy * dy;
                    if (distSq < 1e-8)
                    {
                        // 完全重合时给一个确定性微推力，避免除零
                        dx = (i - j) * 0.01 + 0.01;
                        dy = (j - i) * 0.01 + 0.01;
                        distSq = dx * dx + dy * dy;
                    }

                    var dist = Math.Sqrt(distSq);
                    var force = kSquared / dist;
                    var fx = dx / dist * force;
                    var fy = dy / dist * force;
                    dispX[i] += fx; dispY[i] += fy;
                    dispX[j] -= fx; dispY[j] -= fy;
                }
            }

            // 引力：边的两端
            foreach (var (a, b) in edgeList)
            {
                var dx = xs[a] - xs[b];
                var dy = ys[a] - ys[b];
                var distSq = dx * dx + dy * dy;
                if (distSq < 1e-8)
                {
                    continue;
                }

                var dist = Math.Sqrt(distSq);
                var force = distSq / k;
                var fx = dx / dist * force;
                var fy = dy / dist * force;
                dispX[a] -= fx; dispY[a] -= fy;
                dispX[b] += fx; dispY[b] += fy;
            }

            // 限温位移 + 边界约束
            for (var i = 0; i < n; i++)
            {
                var dx = dispX[i];
                var dy = dispY[i];
                var dispLen = Math.Sqrt(dx * dx + dy * dy);
                if (dispLen > 1e-8)
                {
                    var limited = Math.Min(dispLen, temperature);
                    xs[i] += dx / dispLen * limited;
                    ys[i] += dy / dispLen * limited;
                }

                xs[i] = Math.Clamp(xs[i], minX, maxX);
                ys[i] = Math.Clamp(ys[i], minY, maxY);
            }

            temperature *= cooling;
            if (temperature < minTemperature)
            {
                temperature = minTemperature;
            }
        }

        for (var i = 0; i < n; i++)
        {
            var id = nodeIds[i] ?? string.Empty;
            result.Positions[id] = (xs[i], ys[i]);
        }

        return result;
    }
}
