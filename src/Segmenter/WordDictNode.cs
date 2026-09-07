using System.Collections.Generic;

namespace JiebaNet.Segmenter
{
    /// <summary>
    /// Node of the character-level prefix index built over the main dictionary.
    /// The segmentation hot path walks this index char by char, probing the
    /// dictionary without allocating substring keys.
    /// </summary>
    /// <remarks>
    /// Children use a small list while the branching factor is low, which makes
    /// building the index over ~350k dictionary entries much cheaper than a
    /// per-node dictionary; once a node grows beyond <see cref="UpgradeThreshold"/>
    /// children, the list is upgraded to a dictionary to keep lookups O(1).
    /// </remarks>
    internal sealed class WordDictNode
    {
        public const int UpgradeThreshold = 8;

        public char Char;
        public int Freq;

        /// <summary>Math.Log(Freq) precomputed; 0 when Freq is 0.</summary>
        public double LogFreq;

        private List<WordDictNode> _smallChildren;
        private Dictionary<char, WordDictNode> _children;

        public WordDictNode GetChild(char ch)
        {
            if (_children != null)
            {
                return _children.TryGetValue(ch, out var child) ? child : null;
            }

            if (_smallChildren != null)
            {
                for (var i = 0; i < _smallChildren.Count; i++)
                {
                    if (_smallChildren[i].Char == ch)
                    {
                        return _smallChildren[i];
                    }
                }
            }

            return null;
        }

        public WordDictNode GetOrAddChild(char ch)
        {
            var child = GetChild(ch);
            if (child != null)
            {
                return child;
            }

            child = new WordDictNode { Char = ch };

            if (_children != null)
            {
                _children[ch] = child;
            }
            else
            {
                if (_smallChildren == null)
                {
                    _smallChildren = new List<WordDictNode>(4);
                }
                else if (_smallChildren.Count >= UpgradeThreshold)
                {
                    _children = new Dictionary<char, WordDictNode>(_smallChildren.Count * 2);
                    foreach (var existing in _smallChildren)
                    {
                        _children[existing.Char] = existing;
                    }

                    _smallChildren = null;
                    _children[ch] = child;
                    return child;
                }

                _smallChildren.Add(child);
            }

            return child;
        }
    }
}
