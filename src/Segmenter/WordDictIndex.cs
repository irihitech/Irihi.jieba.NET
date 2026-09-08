using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace JiebaNet.Segmenter
{
    /// <summary>
    /// Arena-allocated node of the character-level prefix index built over the
    /// main dictionary. Nodes reference each other by index; children use a
    /// small sibling chain while the branching factor is low and upgrade to a
    /// dictionary beyond <see cref="UpgradeThreshold"/> entries.
    /// </summary>
    internal struct WordDictNode
    {
        public char Char;
        public int Freq;

        /// <summary>Math.Log(Freq) precomputed; 0 when Freq is 0.</summary>
        public double LogFreq;

        public int FirstChild;   // -1 = none
        public int NextSibling;  // -1 = none
        public int ChildCount;
        public int DictIndex;    // -1 = none; otherwise index into the children-dictionary arena
    }

    /// <summary>
    /// Character-level prefix index over the main dictionary. The segmentation
    /// hot path walks it char by char, probing the dictionary without
    /// allocating substring keys. Nodes are arena-allocated (index-based), so
    /// building the index over ~350k dictionary entries costs no per-node heap
    /// allocation.
    /// </summary>
    internal sealed class WordDictIndex
    {
        private const int UpgradeThreshold = 8;

        private readonly List<WordDictNode> _nodes;
        private readonly List<Dictionary<char, int>> _childrenDicts;

        public WordDictIndex()
        {
            _nodes = new List<WordDictNode>(1 << 20)
            {
                new WordDictNode { Char = '\0', Freq = 0, LogFreq = 0, FirstChild = -1, NextSibling = -1, ChildCount = 0, DictIndex = -1 }
            };
            _childrenDicts = new List<Dictionary<char, int>>();
        }

        public int Root => 0;

        public int GetChild(int parent, char ch)
        {
            ref var p = ref NodeRef(parent);
            if (p.DictIndex >= 0)
            {
                return _childrenDicts[p.DictIndex].TryGetValue(ch, out var found) ? found : -1;
            }

            for (var child = p.FirstChild; child >= 0; child = NodeRef(child).NextSibling)
            {
                if (NodeRef(child).Char == ch)
                {
                    return child;
                }
            }

            return -1;
        }

        public bool IsWordEnd(int node)
        {
            return NodeRef(node).Freq > 0;
        }

        public double GetLogFreq(int node)
        {
            return NodeRef(node).LogFreq;
        }

        public void SetWordFreq(int node, int freq)
        {
            ref var n = ref NodeRef(node);
            n.Freq = freq;
            n.LogFreq = freq > 0 ? Math.Log(freq) : 0;
        }

        public int GetOrAddChild(int parent, char ch)
        {
            ref var p = ref NodeRef(parent);
            if (p.DictIndex >= 0)
            {
                var dict = _childrenDicts[p.DictIndex];
                if (dict.TryGetValue(ch, out var existing))
                {
                    return existing;
                }

                var addedInDict = AppendNode(ch);
                dict[ch] = addedInDict;
                return addedInDict;
            }

            var prev = -1;
            var child = p.FirstChild;
            while (child >= 0)
            {
                ref var c = ref NodeRef(child);
                if (c.Char == ch)
                {
                    return child;
                }

                prev = child;
                child = c.NextSibling;
            }

            var newChild = AppendNode(ch);
            if (prev >= 0)
            {
                NodeRef(prev).NextSibling = newChild;
            }
            else
            {
                NodeRef(parent).FirstChild = newChild;
            }

            var count = NodeRef(parent).ChildCount + 1;
            NodeRef(parent).ChildCount = count;
            if (count > UpgradeThreshold)
            {
                Upgrade(parent);
            }

            return newChild;
        }

        private int AppendNode(char ch)
        {
            var index = _nodes.Count;
            _nodes.Add(new WordDictNode { Char = ch, Freq = 0, LogFreq = 0, FirstChild = -1, NextSibling = -1, ChildCount = 0, DictIndex = -1 });
            return index;
        }

        private void Upgrade(int parent)
        {
            var dict = new Dictionary<char, int>(NodeRef(parent).ChildCount * 2);
            for (var child = NodeRef(parent).FirstChild; child >= 0; child = NodeRef(child).NextSibling)
            {
                dict[NodeRef(child).Char] = child;
            }

            _childrenDicts.Add(dict);
            NodeRef(parent).DictIndex = _childrenDicts.Count - 1;
            NodeRef(parent).FirstChild = -1;
        }

        private ref WordDictNode NodeRef(int index)
        {
            return ref CollectionsMarshal.AsSpan(_nodes)[index];
        }
    }
}
