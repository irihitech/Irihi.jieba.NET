using System.Collections.Generic;
using System.IO;
using JiebaNet.Segmenter;

namespace JiebaNet.Analyser;

public abstract class KeywordExtractor
{
    protected static readonly List<string> DefaultStopWords =
    [
        "the", "of", "is", "and", "to", "in", "that", "we", "for", "an", "are",
        "by", "be", "as", "on", "with", "can", "if", "from", "which", "you", "it",
        "this", "then", "at", "have", "all", "not", "one", "has", "or", "that"
    ];

    protected ISet<string> StopWords { get; set; } = new HashSet<string>();

    /// <summary>
    /// Loads the built-in stop words from the embedded resources.
    /// </summary>
    protected void SetDefaultStopWords()
    {
        StopWords = new HashSet<string>();

        using var reader = new StreamReader(ConfigManager.OpenResource("stopwords.txt"));
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            StopWords.Add(line.Trim());
        }
    }

    public void SetStopWords(string stopWordsFile)
    {
        StopWords = new HashSet<string>();

        var path = Path.GetFullPath(stopWordsFile);
        if (File.Exists(path))
        {
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                StopWords.Add(line.Trim());
            }
        }
    }

    public void AddStopWord(string word)
    {
        StopWords.Add(word.Trim());
    }

    public void AddStopWords(ICollection<string> words)
    {
        foreach (var word in words)
        {
            AddStopWord(word);
        }
    }

    public abstract IEnumerable<string> ExtractTags(string text, int count = 20, ICollection<string>? allowPos = null);
    public abstract IEnumerable<WordWeightPair> ExtractTagsWithWeight(string text, int count = 20, ICollection<string>? allowPos = null);
}