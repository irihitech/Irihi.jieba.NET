using System;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Running;
using JiebaNet.Benchmark;
using JiebaNet.Segmenter;

if (args.Length > 0 && args[0] == "cold")
{
    // One-shot cold start measurement: dictionary/model loading included.
    // Each `dotnet run` invocation is a fresh process.
    var sw = Stopwatch.StartNew();
    var seg = new JiebaSegmenter();
    var tokens = seg.Cut("我来到北京清华大学").Count();
    sw.Stop();
    Console.WriteLine($"COLD_INIT_MS={sw.ElapsedMilliseconds} tokens={tokens}");
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(BenchSuite).Assembly).Run(args);
