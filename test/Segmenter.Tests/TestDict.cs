using System;
using NUnit.Framework;

namespace JiebaNet.Segmenter.Tests
{
    [TestFixture]
    public class TestDict
    {
        [TestCase]
        public void TestDictTrie()
        {
            var dict = WordDictionary.Instance;
            Console.WriteLine(dict.Trie.Count);
            Console.WriteLine(dict.Total);
        }
    }
}
