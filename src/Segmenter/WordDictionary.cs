using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using JiebaNet.Segmenter.Common;

namespace JiebaNet.Segmenter
{
    public class WordDictionary
    {
        private static readonly Lazy<WordDictionary> lazy = new Lazy<WordDictionary>(() => new WordDictionary());

        internal IDictionary<string, int> Trie = new Dictionary<string, int>();

        /// <summary>
        /// Character-level prefix index over the same data as <see cref="Trie"/>,
        /// kept in sync by <see cref="AddWord(string, int, string)"/>; used by the
        /// segmentation hot path to avoid substring allocations.
        /// </summary>
        internal WordDictIndex Index { get; } = new WordDictIndex();

        /// <summary>
        /// total occurrence of all words.
        /// </summary>
        public double Total { get; set; }

        private readonly List<int> _indexPath = new List<int>(16);

        private WordDictionary()
        {
            LoadDict();

            Debug.WriteLine("{0} words (and their prefixes)", Trie.Count);
            Debug.WriteLine("total freq: {0}", Total);
        }

        public static WordDictionary Instance
        {
            get { return lazy.Value; }
        }

        private void LoadDict()
        {
            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                using (var sr = new StreamReader(ConfigManager.OpenResource("dict.txt"), Encoding.UTF8))
                {
                    string line = null;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var span = line.AsSpan();
                        var firstSpace = span.IndexOf(' ');
                        if (firstSpace < 0)
                        {
                            Debug.Fail(string.Format("Invalid line: {0}", line));
                            continue;
                        }

                        var wordSpan = span.Slice(0, firstSpace);
                        var rest = span.Slice(firstSpace + 1);
                        var secondSpace = rest.IndexOf(' ');
                        var freqSpan = secondSpace < 0 ? rest : rest.Slice(0, secondSpace);
                        if (!int.TryParse(freqSpan, out var freq))
                        {
                            throw new FormatException($"Invalid frequency '{freqSpan.ToString()}': {line}");
                        }

                        var word = wordSpan.ToString();

                        Trie[word] = freq;
                        Total += freq;
                        AddToFastTrie(word, freq, _indexPath);

                        for (var i = 0; i < word.Length; i++)
                        {
                            var wfrag = word.Substring(0, i + 1);
                            if (!Trie.ContainsKey(wfrag))
                            {
                                Trie[wfrag] = 0;
                                Index.SetWordFreq(_indexPath[i], 0);
                            }
                        }
                    }
                }

                stopWatch.Stop();
                Debug.WriteLine("main dict load finished, time elapsed {0} ms", stopWatch.ElapsedMilliseconds);
            }
            catch (IOException e)
            {
                Debug.Fail(string.Format("dict.txt load failure, reason: {0}", e.Message));
            }
            catch (FormatException fe)
            {
                Debug.Fail(fe.Message);
            }
        }

        public bool ContainsWord(string word)
        {
            return Trie.ContainsKey(word) && Trie[word] > 0;
        }

        public bool ContainsWord(ReadOnlySpan<char> word)
        {
            var node = Index.Root;
            foreach (var ch in word)
            {
                node = Index.GetChild(node, ch);
                if (node < 0)
                {
                    return false;
                }
            }

            return Index.IsWordEnd(node);
        }

        public int GetFreqOrDefault(string key)
        {
            if (ContainsWord(key))
                return Trie[key];
            else
                return 1;
        }

        public void AddWord(string word, int freq, string tag = null)
        {
            if (ContainsWord(word))
            {
                Total -= Trie[word];
            }

            Trie[word] = freq;
            Total += freq;
            AddToFastTrie(word, freq, _indexPath);

            for (var i = 0; i < word.Length; i++)
            {
                var wfrag = word.Substring(0, i + 1);
                if (!Trie.ContainsKey(wfrag))
                {
                    Trie[wfrag] = 0;
                    Index.SetWordFreq(_indexPath[i], 0);
                }
            }
        }

        private void AddToFastTrie(string word, int freq, List<int> path)
        {
            path.Clear();
            var node = Index.Root;
            foreach (var ch in word)
            {
                node = Index.GetOrAddChild(node, ch);
                path.Add(node);
            }

            Index.SetWordFreq(node, freq);
        }

        public void DeleteWord(string word)
        {
            AddWord(word, 0);
        }

        internal int SuggestFreq(string word, IEnumerable<string> segments)
        {
            double freq = 1;
            foreach (var seg in segments)
            {
                freq *= GetFreqOrDefault(seg) / Total;
            }

            return Math.Max((int)(freq * Total) + 1, GetFreqOrDefault(word));
        }
    }
}
