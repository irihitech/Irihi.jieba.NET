using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JiebaNet.Segmenter.Common;

namespace JiebaNet.Segmenter.FinalSeg
{
    public partial class Viterbi : IFinalSeg
    {
        private static readonly Lazy<Viterbi> Lazy = new Lazy<Viterbi>(() => new Viterbi());
        private static readonly char[] States = { 'B', 'M', 'E', 'S' };

        [GeneratedRegex(@"([\u4E00-\u9FD5]+)")]
        private static partial Regex RegexChinese();

        [GeneratedRegex(@"([a-zA-Z0-9]+(?:\.\d+)?%?)")]
        private static partial Regex RegexSkip();

        private static IDictionary<char, IDictionary<char, double>> _emitProbs;
        private static IDictionary<char, double> _startProbs;
        private static IDictionary<char, IDictionary<char, double>> _transProbs;
        private static IDictionary<char, char[]> _prevStatus;

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

            _prevStatus = new Dictionary<char, char[]>()
            {
                {'B', new []{'E', 'S'}},
                {'M', new []{'M', 'B'}},
                {'S', new []{'S', 'E'}},
                {'E', new []{'B', 'M'}}
            };

            _startProbs = new Dictionary<char, double>()
            {
                {'B', -0.26268660809250016},
                {'E', -3.14e+100},
                {'M', -3.14e+100},
                {'S', -1.4652633398537678}
            };

            var transJson = ConfigManager.ReadResourceText("prob_trans.json");
            _transProbs = JsonHelper.DeserializeProbTable(transJson);

            var emitJson = ConfigManager.ReadResourceText("prob_emit.json");
            _emitProbs = JsonHelper.DeserializeProbTable(emitJson);

            stopWatch.Stop();
            Debug.WriteLine("model loading finished, time elapsed {0} ms.", stopWatch.ElapsedMilliseconds);
        }

        private IEnumerable<string> ViterbiCut(string sentence)
        {
            var v = new List<Dictionary<char, double>>();
            IDictionary<char, Node> path = new Dictionary<char, Node>();

            // Init weights and paths.
            v.Add(new Dictionary<char, double>());
            foreach (var state in States)
            {
                var emP = _emitProbs[state].TryGetValue(sentence[0], out var startEmit)
                    ? startEmit
                    : Constants.MinProb;
                v[0][state] = _startProbs[state] + emP;
                path[state] = new Node(state, null);
            }

            // For each remaining char
            for (var i = 1; i < sentence.Length; ++i)
            {
                var vPrev = v[i - 1];
                var vv = new Dictionary<char, double>();
                v.Add(vv);
                IDictionary<char, Node> newPath = new Dictionary<char, Node>();
                foreach (var y in States)
                {
                    var emp = _emitProbs[y].TryGetValue(sentence[i], out var emitValue)
                        ? emitValue
                        : Constants.MinProb;

                    Pair<char> candidate = new Pair<char>('\0', double.MinValue);
                    foreach (var y0 in _prevStatus[y])
                    {
                        var tranp = _transProbs[y0].TryGetValue(y, out var tranValue)
                            ? tranValue
                            : Constants.MinProb;
                        tranp = vPrev[y0] + tranp + emp;
                        if (candidate.Freq <= tranp)
                        {
                            candidate.Freq = tranp;
                            candidate.Key = y0;
                        }
                    }
                    vv[y] = candidate.Freq;
                    newPath[y] = new Node(y, path[candidate.Key]);
                }
                path = newPath;
            }

            var probE = v[sentence.Length - 1]['E'];
            var probS = v[sentence.Length - 1]['S'];
            var finalPath = probE < probS ? path['S'] : path['E'];

            var posList = new List<char>(sentence.Length);
            while (finalPath != null)
            {
                posList.Add(finalPath.Value);
                finalPath = finalPath.Parent;
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
}