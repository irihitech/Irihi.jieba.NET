using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using JiebaNet.Analyser;
using JiebaNet.Segmenter;
using JiebaNet.Segmenter.PosSeg;

namespace JiebaNet.Benchmark
{
    // Runs in its own (fresh) process: measures true cold start, including
    // dictionary/model loading (embedded streams in this fork, files on disk upstream).
    [ShortRunJob]
    [MemoryDiagnoser]
    public class InitBench
    {
        [Benchmark(Description = "Cold init + first cut")]
        public string InitAndFirstCut()
        {
            var seg = new JiebaSegmenter();
            return string.Join("/", seg.Cut("我来到北京清华大学"));
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class BenchSuite
    {
        private const string ShortText =
            "我来到北京清华大学，打算去附近的餐馆吃晚饭，然后再回实验室继续写代码。";

        private string _medium;
        private string _large;
        private JiebaSegmenter _seg;
        private PosSegmenter _pos;
        private TfidfExtractor _tfidf;
        private TextRankExtractor _textrank;

        [GlobalSetup]
        public void Setup()
        {
            var input = Path.Combine(AppContext.BaseDirectory, "input");
            _medium = File.ReadAllText(Path.Combine(input, "article_social.txt"));
            _large = File.ReadAllText(Path.Combine(input, "weicheng.txt"));

            _seg = new JiebaSegmenter();
            _seg.Cut("热身一下"); // trigger dictionary/model loading outside the timed region
            _pos = new PosSegmenter(_seg);
            _tfidf = new TfidfExtractor(_seg);
            _textrank = new TextRankExtractor();
            _textrank.ExtractTags("热身一下");
        }

        [Benchmark(Description = "Cut accurate+HMM short")]
        public int CutAccurateHmmShort() => _seg.Cut(ShortText).Count();

        [Benchmark(Description = "Cut accurate no-HMM short")]
        public int CutAccurateNoHmmShort() => _seg.Cut(ShortText, hmm: false).Count();

        [Benchmark(Description = "CutAll short")]
        public int CutAllShort() => _seg.Cut(ShortText, cutAll: true).Count();

        [Benchmark(Description = "Cut accurate+HMM medium")]
        public int CutAccurateHmmMedium() => _seg.Cut(_medium).Count();

        [Benchmark(Description = "Cut accurate no-HMM medium")]
        public int CutAccurateNoHmmMedium() => _seg.Cut(_medium, hmm: false).Count();

        [Benchmark(Description = "CutAll medium")]
        public int CutAllMedium() => _seg.Cut(_medium, cutAll: true).Count();

        [Benchmark(Description = "CutForSearch medium")]
        public int CutForSearchMedium() => _seg.CutForSearch(_medium).Count();

        [Benchmark(Description = "POS cut medium")]
        public int PosCutMedium() => _pos.Cut(_medium).Count();

        [Benchmark(Description = "TF-IDF keywords medium")]
        public int TfidfMedium() => _tfidf.ExtractTags(_medium).Count();

        [Benchmark(Description = "TextRank keywords medium")]
        public int TextRankMedium() => _textrank.ExtractTags(_medium).Count();

        [Benchmark(Description = "Cut accurate+HMM large (novel)")]
        public int CutAccurateHmmLarge() => _seg.Cut(_large).Count();

        [Benchmark(Description = "CutAll large (novel)")]
        public int CutAllLarge() => _seg.Cut(_large, cutAll: true).Count();
    }
}
