using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using JiebaNet.Segmenter.Common;

namespace JiebaNet.Segmenter.FinalSeg;

public partial class Viterbi : IFinalSeg
{
    private static readonly Lazy<Viterbi> Lazy = new(() => new Viterbi());

    // State order used by every array below: B = 0, M = 1, E = 2, S = 3.
    private static readonly char[] States = ['B', 'M', 'E', 'S'];

    private static readonly int[][] PrevStatusIndexes =
    [
        [2, 3],  // B <- E, S
        [1, 0],  // M <- M, B
        [0, 1],  // E <- B, M
        [3, 2] // S <- S, E
    ];

    [GeneratedRegex(@"([\u4E00-\u9FD5]+)")]
    private static partial Regex RegexChinese();

    [GeneratedRegex(@"([a-zA-Z0-9]+(?:\.\d+)?%?)")]
    private static partial Regex RegexSkip();

    private static Dictionary<char, double>[] _emitProbs;  // [state] -> char of sentence -> prob
    private static double[] _startProbs;                    // [state]
    private static double[][] _transProbs;                  // [prev][cur]

    private Viterbi()
    {
        LoadModel();
    }

    // TODO: synchronized
    public static Viterbi Instance
    {
        get { return Lazy.Value; }
    }

    public IEnumerable<string> Cut(string sentence)
    {
        var tokens = new List<string>();
        foreach (var blk in RegexChinese().Split(sentence))
        {
            if (RegexChinese().IsMatch(blk))
            {
                tokens.AddRange(ViterbiCut(blk));
            }
            else
            {
                var segments = RegexSkip().Split(blk).Where(seg => !string.IsNullOrEmpty(seg));
                tokens.AddRange(segments);
            }
        }
        return tokens;
    }

    #region Private Helpers

    private void LoadModel()
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        _startProbs =
        [
            -0.26268660809250016,  // B
            -3.14e+100,            // M
            -3.14e+100,            // E
            -1.4652633398537678    // S
        ];

        var transJson = ConfigManager.ReadResourceText("prob_trans.json");
        var transTable = JsonHelper.DeserializeProbTable(transJson);

        var emitJson = ConfigManager.ReadResourceText("prob_emit.json");
        var emitTable = JsonHelper.DeserializeProbTable(emitJson);

        _transProbs = new double[4][];
        _emitProbs = new Dictionary<char, double>[4];
        for (var si = 0; si < 4; si++)
        {
            var state = States[si];

            var row = new double[4];
            for (var sj = 0; sj < 4; sj++)
            {
                row[sj] = transTable[state].TryGetValue(States[sj], out var tranValue)
                    ? tranValue
                    : Constants.MinProb;
            }
            _transProbs[si] = row;

            _emitProbs[si] = new Dictionary<char, double>(emitTable[state]);
        }

        stopWatch.Stop();
        Debug.WriteLine("model loading finished, time elapsed {0} ms.", stopWatch.ElapsedMilliseconds);
    }

    private IEnumerable<string> ViterbiCut(string sentence)
    {
        var n = sentence.Length;
        var v = new double[n * 4];
        var bestPrev = new int[n * 4];

        // Init weights of the first char.
        for (var si = 0; si < 4; si++)
        {
            var emp = _emitProbs[si].TryGetValue(sentence[0], out var emitValue)
                ? emitValue
                : Constants.MinProb;
            v[si] = _startProbs[si] + emp;
        }

        // For each remaining char
        for (var i = 1; i < n; i++)
        {
            var baseCur = i * 4;
            var basePrev = baseCur - 4;
            var ch = sentence[i];
            for (var si = 0; si < 4; si++)
            {
                var emp = _emitProbs[si].TryGetValue(ch, out var emitValue)
                    ? emitValue
                    : Constants.MinProb;

                var best = double.MinValue;
                var bestPrevState = 0;
                foreach (var y0 in PrevStatusIndexes[si])
                {
                    var prob = v[basePrev + y0] + _transProbs[y0][si] + emp;
                    if (best <= prob)
                    {
                        best = prob;
                        bestPrevState = y0;
                    }
                }

                v[baseCur + si] = best;
                bestPrev[baseCur + si] = bestPrevState;
            }
        }

        var lastBase = (n - 1) * 4;
        var probE = v[lastBase + 2];
        var probS = v[lastBase + 3];
        var state = probE < probS ? 3 : 2;

        var posList = new List<char>(n);
        posList.Add(States[state]);
        for (var i = n - 1; i > 0; i--)
        {
            state = bestPrev[i * 4 + state];
            posList.Add(States[state]);
        }
        posList.Reverse();

        var tokens = new List<string>();
        int begin = 0, next = 0;
        for (var i = 0; i < sentence.Length; i++)
        {
            var pos = posList[i];
            if (pos == 'B')
                begin = i;
            else if (pos == 'E')
            {
                tokens.Add(sentence.Sub(begin, i + 1));
                next = i + 1;
            }
            else if (pos == 'S')
            {
                tokens.Add(sentence.Sub(i, i + 1));
                next = i + 1;
            }
        }
        if (next < sentence.Length)
        {
            tokens.Add(sentence.Substring(next));
        }

        return tokens;
    }

    #endregion
}