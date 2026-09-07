using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JiebaNet.Segmenter.Common;
using JiebaNet.Segmenter.FinalSeg;

namespace JiebaNet.Segmenter
{
    public class JiebaSegmenter
    {
        private static readonly WordDictionary WordDict = WordDictionary.Instance;
        private static readonly IFinalSeg FinalSeg = Viterbi.Instance;
        private static readonly ISet<string> LoadedPath = new HashSet<string>();

        private static readonly object locker = new object();

        internal IDictionary<string, string> UserWordTagTab { get; set; }

        #region Regular Expressions

        internal static readonly Regex RegexChineseDefault = new Regex(@"([\u4E00-\u9FD5a-zA-Z0-9+#&\._%·\-]+)", RegexOptions.Compiled);

        internal static readonly Regex RegexSkipDefault = new Regex(@"(\r\n|\s)", RegexOptions.Compiled);

        internal static readonly Regex RegexChineseCutAll = new Regex(@"([\u4E00-\u9FD5]+)", RegexOptions.Compiled);
        internal static readonly Regex RegexSkipCutAll = new Regex(@"[^a-zA-Z0-9+#\n]", RegexOptions.Compiled);

        internal static readonly Regex RegexEnglishChars = new Regex(@"[a-zA-Z0-9]", RegexOptions.Compiled);

        internal static readonly Regex RegexUserDict = new Regex("^(?<word>.+?)(?<freq> [0-9]+)?(?<tag> [a-z]+)?$", RegexOptions.Compiled);

        #endregion

        public JiebaSegmenter()
        {
            UserWordTagTab = new Dictionary<string, string>();
        }

        /// <summary>
        /// The main function that segments an entire sentence that contains 
        /// Chinese characters into seperated words.
        /// </summary>
        /// <param name="text">The string to be segmented.</param>
        /// <param name="cutAll">Specify segmentation pattern. True for full pattern, False for accurate pattern.</param>
        /// <param name="hmm">Whether to use the Hidden Markov Model.</param>
        /// <returns></returns>
        public IEnumerable<string> Cut(string text, bool cutAll = false, bool hmm = true)
        {
            var reHan = cutAll ? RegexChineseCutAll : RegexChineseDefault;
            var reSkip = cutAll ? RegexSkipCutAll : RegexSkipDefault;
            var cutMethod = cutAll ? CutAll : hmm ? CutDag : (Func<string, IEnumerable<string>>)CutDagWithoutHmm;
            return CutIt(text, cutMethod, reHan, reSkip, cutAll);
        }
        
        public IEnumerable<IEnumerable<string>> CutInParallel(IEnumerable<string> texts, bool cutAll = false, bool hmm = true)
        {
            var reHan = cutAll ? RegexChineseCutAll : RegexChineseDefault;
            var reSkip = cutAll ? RegexSkipCutAll : RegexSkipDefault;
            var cutMethod = cutAll ? CutAll : hmm ? CutDag : (Func<string, IEnumerable<string>>)CutDagWithoutHmm;

            return texts.AsParallel().AsOrdered().Select(text => CutIt(text, cutMethod, reHan, reSkip, cutAll));
        }
        
        public IEnumerable<string> CutInParallel(string text, bool cutAll = false, bool hmm = true)
        {
            var lines = text.SplitLines();
            return CutInParallel(lines, cutAll, hmm).SelectMany(words => words);
        }

        public IEnumerable<string> CutForSearch(string text, bool hmm = true)
        {
            var result = new List<string>();

            var words = Cut(text, hmm: hmm);
            foreach (var w in words)
            {
                if (w.Length > 2)
                {
                    for (var i = 0; i < w.Length - 1; i++)
                    {
                        var gram2 = w.Substring(i, 2);
                        if (WordDict.ContainsWord(gram2))
                        {
                            result.Add(gram2);
                        }
                    }
                }

                if (w.Length > 3)
                {
                    for (var i = 0; i < w.Length - 2; i++)
                    {
                        var gram3 = w.Substring(i, 3);
                        if (WordDict.ContainsWord(gram3))
                        {
                            result.Add(gram3);
                        }
                    }
                }

                result.Add(w);
            }

            return result;
        }
        
        public IEnumerable<IEnumerable<string>> CutForSearchInParallel(IEnumerable<string> texts, bool hmm = true)
        {
            return texts.AsParallel().AsOrdered().Select(line => CutForSearch(line, hmm));
        }
        
        public IEnumerable<string> CutForSearchInParallel(string text, bool hmm = true)
        {
            var lines = text.SplitLines();
            return CutForSearchInParallel(lines, hmm).SelectMany(words => words);
        }

        public IEnumerable<Token> Tokenize(string text, TokenizerMode mode = TokenizerMode.Default, bool hmm = true)
        {
            var result = new List<Token>();

            var start = 0;
            if (mode == TokenizerMode.Default)
            {
                foreach (var w in Cut(text, hmm: hmm))
                {
                    var width = w.Length;
                    result.Add(new Token(w, start, start + width));
                    start += width;
                }
            }
            else
            {
                foreach (var w in Cut(text, hmm: hmm))
                {
                    var width = w.Length;
                    if (width > 2)
                    {
                        for (var i = 0; i < width - 1; i++)
                        {
                            var gram2 = w.Substring(i, 2);
                            if (WordDict.ContainsWord(gram2))
                            {
                                result.Add(new Token(gram2, start + i, start + i + 2));
                            }
                        }
                    }
                    if (width > 3)
                    {
                        for (var i = 0; i < width - 2; i++)
                        {
                            var gram3 = w.Substring(i, 3);
                            if (WordDict.ContainsWord(gram3))
                            {
                                result.Add(new Token(gram3, start + i, start + i + 3));
                            }
                        }
                    }

                    result.Add(new Token(w, start, start + width));
                    start += width;
                }
            }

            return result;
        }

        #region Internal Cut Methods

        internal IDictionary<int, List<WordEnd>> GetDag(string sentence)
        {
            var dag = new Dictionary<int, List<WordEnd>>();
            var root = WordDict.Root;

            var N = sentence.Length;
            for (var k = 0; k < N; k++)
            {
                var templist = new List<WordEnd>();
                var node = root.GetChild(sentence[k]);
                var i = k;
                while (node != null)
                {
                    if (node.Freq > 0)
                    {
                        templist.Add(new WordEnd(i, node.LogFreq));
                    }

                    i++;
                    if (i < N)
                    {
                        node = node.GetChild(sentence[i]);
                    }
                    else
                    {
                        break;
                    }
                }

                if (templist.Count == 0)
                {
                    templist.Add(new WordEnd(k, 0.0));
                }

                dag[k] = templist;
            }

            return dag;
        }

        internal IDictionary<int, Pair<int>> Calc(string sentence, IDictionary<int, List<WordEnd>> dag)
        {
            var n = sentence.Length;
            var route = new Dictionary<int, Pair<int>>(n + 1);
            route[n] = new Pair<int>(0, 0.0);

            var logtotal = Math.Log(WordDict.Total);
            for (var i = n - 1; i > -1; i--)
            {
                var bestKey = -1;
                var bestFreq = double.MinValue;
                foreach (var candidate in dag[i])
                {
                    var freq = candidate.LogFreq - logtotal + route[candidate.End + 1].Freq;
                    if (bestFreq < freq)
                    {
                        bestFreq = freq;
                        bestKey = candidate.End;
                    }
                }

                route[i] = new Pair<int>(bestKey, bestFreq);
            }

            return route;
        }

        internal IEnumerable<string> CutAll(string sentence)
        {
            var dag = GetDag(sentence);

            var words = new List<string>();
            var lastPos = -1;

            foreach (var pair in dag)
            {
                var k = pair.Key;
                var nexts = pair.Value;
                if (nexts.Count == 1 && k > lastPos)
                {
                    words.Add(sentence.Substring(k, nexts[0].End + 1 - k));
                    lastPos = nexts[0].End;
                }
                else
                {
                    foreach (var j in nexts)
                    {
                        if (j.End > k)
                        {
                            words.Add(sentence.Substring(k, j.End + 1 - k));
                            lastPos = j.End;
                        }
                    }
                }
            }

            return words;
        }

        internal IEnumerable<string> CutDag(string sentence)
        {
            var dag = GetDag(sentence);
            var route = Calc(sentence, dag);

            var tokens = new List<string>();

            var x = 0;
            var n = sentence.Length;
            var buf = new StringBuilder();
            while (x < n)
            {
                var y = route[x].Key + 1;
                var w = sentence.Substring(x, y - x);
                if (y - x == 1)
                {
                    buf.Append(w);
                }
                else
                {
                    if (buf.Length > 0)
                    {
                        AddBufferToWordList(tokens, buf.ToString());
                        buf.Clear();
                    }
                    tokens.Add(w);
                }
                x = y;
            }

            if (buf.Length > 0)
            {
                AddBufferToWordList(tokens, buf.ToString());
            }

            return tokens;
        }

        internal IEnumerable<string> CutDagWithoutHmm(string sentence)
        {
            var dag = GetDag(sentence);
            var route = Calc(sentence, dag);

            var words = new List<string>();

            var x = 0;
            var buf = new StringBuilder();
            var N = sentence.Length;

            var y = -1;
            while (x < N)
            {
                y = route[x].Key + 1;
                var lWord = sentence.Substring(x, y - x);
                if (lWord.Length == 1 && IsEnglishChar(lWord[0]))
                {
                    buf.Append(lWord);
                    x = y;
                }
                else
                {
                    if (buf.Length > 0)
                    {
                        words.Add(buf.ToString());
                        buf.Clear();
                    }
                    words.Add(lWord);
                    x = y;
                }
            }

            if (buf.Length > 0)
            {
                words.Add(buf.ToString());
            }

            return words;
        }

        private static bool IsEnglishChar(char ch)
        {
            return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9');
        }

        internal IEnumerable<string> CutIt(string text, Func<string, IEnumerable<string>> cutMethod,
                                           Regex reHan, Regex reSkip, bool cutAll)
        {
            var result = new List<string>();
            var blocks = reHan.Split(text);
            foreach (var blk in blocks)
            {
                if (string.IsNullOrEmpty(blk))
                {
                    continue;
                }

                if (reHan.IsMatch(blk))
                {
                    result.AddRange(cutMethod(blk));
                }
                else
                {
                    var tmp = reSkip.Split(blk);
                    foreach (var x in tmp)
                    {
                        if (reSkip.IsMatch(x))
                        {
                            result.Add(x);
                        }
                        else if (!cutAll)
                        {
                            foreach (var ch in x)
                            {
                                result.Add(ch.ToString());
                            }
                        }
                        else
                        {
                            result.Add(x);
                        }
                    }
                }
            }

            return result;
        }

        #endregion

        #region Extend Main Dict

        /// <summary>
        /// Loads user dictionaries.
        /// </summary>
        /// <param name="userDictFile"></param>
        public void LoadUserDict(string userDictFile)
        {
            var dictFullPath = Path.GetFullPath(userDictFile);
            Debug.WriteLine("Initializing user dictionary: " + userDictFile);

            lock (locker)
            {
                if (LoadedPath.Contains(dictFullPath))
                    return;

                try
                {
                    var startTime = DateTime.Now.Millisecond;

                    var lines = File.ReadAllLines(dictFullPath, Encoding.UTF8);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        var tokens = RegexUserDict.Match(line.Trim()).Groups;
                        var word = tokens["word"].Value.Trim();
                        var freq = tokens["freq"].Value.Trim();
                        var tag = tokens["tag"].Value.Trim();

                        var actualFreq = freq.Length > 0 ? int.Parse(freq) : 0;
                        AddWord(word, actualFreq, tag);
                    }

                    Debug.WriteLine("user dict '{0}' load finished, time elapsed {1} ms",
                        dictFullPath, DateTime.Now.Millisecond - startTime);
                }
                catch (IOException e)
                {
                    Debug.Fail(string.Format("'{0}' load failure, reason: {1}", dictFullPath, e.Message));
                }
                catch (FormatException fe)
                {
                    Debug.Fail(fe.Message);
                }
            }
        }

        public void AddWord(string word, int freq = 0, string tag = null)
        {
            if (freq <= 0)
            {
                freq = WordDict.SuggestFreq(word, Cut(word, hmm: false));
            }
            WordDict.AddWord(word, freq);

            // Add user word tag of POS
            if (!string.IsNullOrEmpty(tag))
            {
                UserWordTagTab[word] = tag;
            }
        }

        public void DeleteWord(string word)
        {
            WordDict.DeleteWord(word);
        }

        #endregion

        #region Private Helpers

        private void AddBufferToWordList(List<string> words, string buf)
        {
            if (buf.Length == 1)
            {
                words.Add(buf);
            }
            else
            {
                if (!WordDict.ContainsWord(buf))
                {
                    var tokens = FinalSeg.Cut(buf);
                    words.AddRange(tokens);
                }
                else
                {
                    for (var i = 0; i < buf.Length; i++)
                    {
                        words.Add(buf[i].ToString());
                    }
                }
            }
        }

        #endregion
    }

    public enum TokenizerMode
    {
        Default,
        Search
    }
}
