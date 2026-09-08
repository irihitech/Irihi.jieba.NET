using System;
using System.Collections.Generic;
using System.Linq;
using JiebaNet.Segmenter.Common;

namespace JiebaNet.Segmenter.PosSeg;

public class Viterbi
{
    private static readonly Lazy<Viterbi> Lazy = new Lazy<Viterbi>(() => new Viterbi());

    private static IDictionary<string, double> _startProbs;
    private static IDictionary<string, IDictionary<string, double>> _transProbs;
    private static IDictionary<string, IDictionary<char, double>> _emitProbs;
    private static IDictionary<char, List<string>> _stateTab;

    private Viterbi()
    {
        LoadModel();
    }

    // TODO: synchronized
    public static Viterbi Instance
    {
        get { return Lazy.Value; }
    }

    public IEnumerable<Pair> Cut(string sentence)
    {
        var probPosList = ViterbiCut(sentence);
        var posList = probPosList.Item2;

        var tokens = new List<Pair>();
        int begin = 0, next = 0;
        for (var i = 0; i < sentence.Length; i++)
        {
            var parts = posList[i].Split('-');
            var charState = parts[0][0];
            var pos = parts[1];
            if (charState == 'B')
                begin = i;
            else if (charState == 'E')
            {
                tokens.Add(new Pair(sentence.Sub(begin, i + 1), pos));
                next = i + 1;
            }
            else if (charState == 'S')
            {
                tokens.Add(new Pair(sentence.Sub(i, i + 1), pos));
                next = i + 1;
            }
        }
        if (next < sentence.Length)
        {
            tokens.Add(new Pair(sentence.Substring(next), posList[next].Split('-')[1]));
        }
            
        return tokens;
    }

    #region Private Helpers

    private static void LoadModel()
    {
        _startProbs = JsonHelper.DeserializePosProbStart(ConfigManager.ReadResourceText("pos_prob_start.json"));

        _transProbs = JsonHelper.DeserializePosProbTrans(ConfigManager.ReadResourceText("pos_prob_trans.json"));

        _emitProbs = JsonHelper.DeserializePosProbEmit(ConfigManager.ReadResourceText("pos_prob_emit.json"));

        _stateTab = JsonHelper.DeserializeCharStateTab(ConfigManager.ReadResourceText("char_state_tab.json"));
    }

    // TODO: change sentence to obs?
    private Tuple<double, List<string>> ViterbiCut(string sentence)
    {
        var v = new List<IDictionary<string, double>>();
        var memPath = new List<IDictionary<string, string>>();

        var allStates = _transProbs.Keys.ToList();

        // Init weights and paths.
        v.Add(new Dictionary<string, double>());
        memPath.Add(new Dictionary<string, string>());
        foreach (var state in _stateTab.GetDefault(sentence[0], allStates))
        {
            var emP = _emitProbs[state].GetDefault(sentence[0], Constants.MinProb);
            v[0][state] = _startProbs[state] + emP;
            memPath[0][state] = string.Empty;
        }

        // Reusable buffers for the per-position computations below.
        var prevStates = new List<string>();
        var curPossibleStates = new HashSet<string>();
        var obsStates = new List<string>();

        // For each remaining char
        for (var i = 1; i < sentence.Length; ++i)
        {
            var vPrev = v[i - 1];
            var pathPrev = memPath[i - 1];

            prevStates.Clear();
            foreach (var key in pathPrev.Keys)
            {
                if (_transProbs[key].Count > 0)
                {
                    prevStates.Add(key);
                }
            }

            curPossibleStates.Clear();
            foreach (var s in prevStates)
            {
                var transitions = _transProbs[s];
                foreach (var next in transitions.Keys)
                {
                    curPossibleStates.Add(next);
                }
            }

            obsStates.Clear();
            foreach (var s in _stateTab.GetDefault(sentence[i], allStates))
            {
                if (curPossibleStates.Contains(s))
                {
                    obsStates.Add(s);
                }
            }

            if (obsStates.Count == 0)
            {
                if (curPossibleStates.Count > 0)
                {
                    foreach (var s in curPossibleStates)
                    {
                        obsStates.Add(s);
                    }
                }
                else
                {
                    obsStates.AddRange(allStates);
                }
            }

            var vCur = new Dictionary<string, double>();
            var pathCur = new Dictionary<string, string>();

            foreach (var y in obsStates)
            {
                var emit = _emitProbs[y];
                var emp = emit.TryGetValue(sentence[i], out var emitValue)
                    ? emitValue
                    : Constants.MinProb;

                var prob = double.MinValue;
                var state = string.Empty;

                foreach (var y0 in prevStates)
                {
                    var tranp = _transProbs[y0].TryGetValue(y, out var tranValue)
                        ? tranValue
                        : double.MinValue;
                    tranp = vPrev[y0] + tranp + emp;
                    // TODO: compare two very small values;
                    // TODO: how to deal with negative infinity
                    if (prob < tranp ||
                        (prob == tranp && string.Compare(state, y0, StringComparison.InvariantCulture) < 0))
                    {
                        prob = tranp;
                        state = y0;
                    }
                }

                vCur[y] = prob;
                pathCur[y] = state;
            }

            v.Add(vCur);
            memPath.Add(pathCur);
        }

        var vLast = v.Last();
        var last = memPath.Last().Keys.Select(y => new {State = y, Prob = vLast[y]});
        var endProb = double.MinValue;
        var endState = string.Empty;
        foreach (var endPoint in last)
        {
            // TODO: compare two very small values;
            if (endProb < endPoint.Prob ||
                (endProb == endPoint.Prob && String.Compare(endState, endPoint.State, StringComparison.InvariantCulture) < 0))
            {
                endProb = endPoint.Prob;
                endState = endPoint.State;
            }
        }

        var route = new string[sentence.Length];
        var n = sentence.Length - 1;
        var curState = endState;
        while(n >= 0)
        {
            route[n] = curState;
            curState = memPath[n][curState];
            n--;
        }

        return new Tuple<double, List<string>>(endProb, route.ToList());
    }

    #endregion
}