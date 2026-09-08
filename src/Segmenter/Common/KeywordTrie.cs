using System.Collections.Generic;

namespace JiebaNet.Segmenter.Common;

public class KeywordTrieNode
{
    private readonly Dictionary<char, KeywordTrieNode> _children;
    // private string _value;
        
    public KeywordTrieNode(string? value = null)
    {
        _children = new Dictionary<char, KeywordTrieNode>();
        Value = value;
    }

    public string? Value { get; set; }

    public bool HasValue => Value is not null;

    public KeywordTrieNode AddChild(char ch, string? value = null, bool overwrite = false)
    {
        var child = _children.GetOrDefault(ch);
        if (child is null)
        {
            child = new KeywordTrieNode(value);
            _children[ch] = child;
        }
        else if (overwrite)
        {
            child.Value = value;
        }

        return child;
    }
        
    public KeywordTrieNode? GetChild(char ch)
    {
        var child = _children.GetOrDefault(ch);
        return child;
    }

    public bool HasChild(char ch)
    {
        return _children.ContainsKey(ch);
    }
}

public class KeywordTrie: KeywordTrieNode
{
    public KeywordTrie()
    {
        Count = 0;
    }
        
    public int Count { get; set; }
        
    public bool Contains(string key)
    {
        return GetItem(key) is not null;
    }

    public void Remove(string key)
    {
        // TODO: impl and count
        this[key] = null;
    }

    public string? this[string key]
    {
        get => GetItem(key);
        set => SetItem(key, value);
    }

    #region Private Methods

    private string? GetItem(string key)
    {
        KeywordTrieNode? state = this;
        foreach (var ch in key)
        {
            state = state.GetChild(ch);
            if (state is null)
            {
                return null;
            }
        }

        return state.Value;
    }

    private void SetItem(string key, string? value)
    {
        KeywordTrieNode state = this;
        for (int i = 0; i < key.Length; i++)
        {
            if (i < key.Length - 1)
            {
                state = state.AddChild(key[i]);
            }
            else
            {
                var child = state.GetChild(key[i]);
                state = state.AddChild(key[i], value, true);
                if (child is null || !child.HasValue)
                {
                    Count += 1;
                }
            }
        }
    }

    #endregion
}
    
public record TextSpan(string Text, int Start, int End)
{
    public override string ToString()
    {
        return $"<{Text}({Start}, {End})>";
    }
}