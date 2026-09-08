using System;
using JiebaNet.Analyser;
using JiebaNet.Segmenter;
using JiebaNet.Segmenter.PosSeg;

namespace JiebaNet.NativeAotSmokeTest;

// Self-checking smoke test: exercises the full feature set of jieba.NET and
// compares every output against the expected value, so that CI can validate
// a NativeAOT published binary by its exit code alone.
internal static class Program
{
    private static int Main()
    {
        var seg = new JiebaSegmenter();
        seg.Cut("热身");

        var pos = new PosSegmenter();
        var tfidf = new TfidfExtractor();

        var failures = 0;
        failures += Check("accurate",
            "我/ 来到/ 北京/ 清华大学",
            string.Join("/ ", seg.Cut("我来到北京清华大学")));
        failures += Check("cut-all",
            "他/ 来到/ 了/ 网易/ 杭/ 研/ 大厦",
            string.Join("/ ", seg.Cut("他来到了网易杭研大厦", cutAll: true)));
        failures += Check("hmm",
            "这是/ 一个/ 伸手不见五指/ 的/ 黑夜/ 。/ 我/ 叫/ 孙悟空/ ，/ 我/ 爱/ 北京/ ，/ 我/ 爱/ 吃/ 烤鸭/ 。",
            string.Join("/ ", seg.Cut("这是一个伸手不见五指的黑夜。我叫孙悟空，我爱北京，我爱吃烤鸭。")));
        failures += Check("cut-for-search",
            "小明/ 硕士/ 毕业/ 于/ 中国/ 科学/ 学院/ 科学院/ 中国科学院/ 计算/ 计算所/ ，/ 后/ 在/ 日本/ 京都/ 大学/ 日本京都大学/ 深造",
            string.Join("/ ", seg.CutForSearch("小明硕士毕业于中国科学院计算所，后在日本京都大学深造")));
        failures += Check("pos",
            "这/r 是/v 一个/m 伸手不见五指/i 的/uj 黑夜/n 。/x",
            string.Join(" ", pos.Cut("这是一个伸手不见五指的黑夜。")));
        failures += Check("keywords",
            "烤鸭/ 伸手不见五指/ 孙悟空/ 黑夜/ 北京/ 这是",
            string.Join("/ ", tfidf.ExtractTags("这是一个伸手不见五指的黑夜。我叫孙悟空，我爱北京，我爱吃烤鸭。")));

        if (failures > 0)
        {
            Console.WriteLine($"NativeAOT smoke test FAILED: {failures} check(s) mismatched.");
            return 1;
        }

        Console.WriteLine("NativeAOT smoke test passed.");
        return 0;
    }

    private static int Check(string name, string expected, string actual)
    {
        var ok = string.Equals(expected, actual, StringComparison.Ordinal);
        Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] {name}");
        if (!ok)
        {
            Console.WriteLine($"  expected: {expected}");
            Console.WriteLine($"  actual:   {actual}");
        }

        return ok ? 0 : 1;
    }
}