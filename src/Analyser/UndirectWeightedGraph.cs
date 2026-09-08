using System.Collections.Generic;
using System.Linq;

namespace JiebaNet.Analyser;

internal readonly record struct Edge(string Start, string End, double Weight);

internal class UndirectWeightedGraph
{
    private static readonly double d = 0.85;

    private readonly Dictionary<string, List<Edge>> _graph = new();

    public void AddEdge(string start, string end, double weight)
    {
        if (!_graph.ContainsKey(start))
        {
            _graph[start] = new List<Edge>();
        }

        if (!_graph.ContainsKey(end))
        {
            _graph[end] = new List<Edge>();
        }

        _graph[start].Add(new Edge(start, end, weight));
        _graph[end].Add(new Edge(end, start, weight));
    }

    public IDictionary<string, double> Rank()
    {
        var ws = new Dictionary<string, double>();
        var outSum = new Dictionary<string, double>();

        // init scores
        var count = _graph.Count > 0 ? _graph.Count : 1;
        var wsdef = 1.0/count;

        foreach (var pair in _graph)
        {
            ws[pair.Key] = wsdef;
            outSum[pair.Key] = pair.Value.Sum(e => e.Weight);
        }

        // TODO: 10 iterations?
        var sortedKeys = _graph.Keys.OrderBy(k => k).ToList();
        for (var i = 0; i < 10; i++)
        {
            foreach (var n in sortedKeys)
            {
                var s = 0d;
                foreach (var edge in _graph[n])
                {
                    s += edge.Weight/outSum[edge.End]*ws[edge.End];
                }
                ws[n] = (1 - d) + d*s;
            }
        }

        var minRank = double.MaxValue;
        var maxRank = double.MinValue;

        foreach (var w in ws.Values)
        {
            if (w < minRank)
            {
                minRank = w;
            }
            if(w > maxRank)
            {
                maxRank = w;
            }
        }

        foreach (var pair in ws.ToList())
        {
            ws[pair.Key] = (pair.Value - minRank/10.0)/(maxRank - minRank/10.0);
        }

        return ws;
    }
}