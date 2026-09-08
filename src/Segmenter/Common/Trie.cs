using System;
using System.Collections.Generic;
using System.Linq;

namespace JiebaNet.Segmenter.Common;

// Refer to: https://github.com/brianfromoregon/trie
public class TrieNode(char ch)
{
    public char Char { get; set; } = ch;
    public int Frequency { get; set; } = 0;
    public Dictionary<char, TrieNode>? Children { get; set; }

    public int Insert(string s, int pos, int freq = 1)
    {
        if (string.IsNullOrEmpty(s) || pos >= s.Length)
        {
            return 0;
        }

        Children ??= new Dictionary<char, TrieNode>();

        var c = s[pos];
        if (!Children.ContainsKey(c))
        {
            Children[c] = new TrieNode(c);
        }

        var curNode = Children[c];
        if (pos == s.Length - 1)
        {
            curNode.Frequency += freq;
            return curNode.Frequency;
        }

        return curNode.Insert(s, pos + 1, freq);
    }

    public TrieNode? Search(string s, int pos)
    {
        if (string.IsNullOrEmpty(s))
        {
            return null;
        }

        // if out of range or without any child nodes
        if (pos >= s.Length || Children == null)
        {
            return null;
        }
        // if reaches the last char of s, it's time to make the decision.
        if (pos == s.Length - 1)
        {
            return Children.ContainsKey(s[pos]) ? Children[s[pos]] : null;
        }
        // continue if necessary.
        return Children.ContainsKey(s[pos]) ? Children[s[pos]].Search(s, pos + 1) : null;
    }
}

public interface ITrie
{
    //string BestMatch(string word, long maxTime);
    bool Contains(string word);
    int Frequency(string word);
    int Insert(string word, int freq = 1);
    //bool Remove(string word);
    int Count { get; }
    int TotalFrequency { get; }
}

public class Trie : ITrie
{
    private static readonly char RootChar = '\0';

    internal readonly TrieNode Root = new(RootChar);

    public int Count { get; private set; } = 0;
    public int TotalFrequency { get; private set; }

    public bool Contains(string word)
    {
        CheckWord(word);

        var node = Root.Search(word.Trim(), 0);
        return node is not null && node.Frequency > 0;
    }

    public bool ContainsPrefix(string word)
    {
        CheckWord(word);

        var node = Root.Search(word.Trim(), 0);
        return node is not null;
    }

    public int Frequency(string word)
    {
        CheckWord(word);

        var node = Root.Search(word.Trim(), 0);
        return node is null ? 0 : node.Frequency;
    }

    public int Insert(string word, int freq = 1)
    {
        CheckWord(word);

        var i = Root.Insert(word.Trim(), 0, freq);
        if (i > 0)
        {
            TotalFrequency += freq;
            Count++;
        }

        return i;
    }

    public IEnumerable<char> ChildChars(string prefix)
    {
        var node = Root.Search(prefix.Trim(), 0);
        return node is null || node.Children is null ? null : node.Children.Select(p => p.Key);
    }

    private void CheckWord(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            throw new ArgumentException("word must not be null or whitespace");
        }
    }
}