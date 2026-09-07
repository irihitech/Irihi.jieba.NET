# Benchmark

对比本仓库（fork）与 NuGet 上的上游 [jieba.NET 0.42.2](https://www.nuget.org/packages/jieba.NET) 的性能。

## 布局

- `Shared/BenchSuite.cs` — 共享的 BenchmarkDotNet 套件（分词精确/全模式/HMM/搜索引擎、词性标注、关键词提取，共 12 个场景 + 冷启动），分别链接进两个项目。两个库的类型名完全相同，无法在单项目中同时引用，因此采用双项目布局。
- `JiebaNet.ForkBench` — 引用本仓库源码。
- `JiebaNet.UpstreamBench` — 引用 NuGet 包 `jieba.NET 0.42.2`，并自动把仓库 `src/Segmenter/Resources` 复制到输出目录（上游从磁盘加载词典），保证两边数据一致。

## 运行

```powershell
cd benchmark
.\run.ps1                          # 双方全场景 + 对比表
.\run.ps1 -Side fork               # 只跑本仓库
.\run.ps1 -Filter "*medium*"       # 只跑部分场景（BDN 过滤器）
```

脚本会：串行执行双方（避免 CPU 争抢）→ 打印 BDN 原始表格 → 各测 3 次冷启动（词典/模型加载）→ 输出按场景合并的对比表（`upstream / fork` 比值 > 1 表示 fork 更快）。

原始结果（CSV/Markdown）在各项目的 `BenchmarkDotNet.Artifacts/results/` 目录。

## 说明

- 套件使用 `ShortRunJob`（3 次预热 + 3 次迭代）+ `MemoryDiagnoser`：适合优化前后的方向性对比；如需发布级性能数据，把 `BenchSuite.cs` 上的 `[ShortRunJob]` 移除即可使用默认 Job。
- 新增场景：在 `Shared/BenchSuite.cs` 加 `[Benchmark]` 方法即可，两个项目自动共享。
- `Program.cs cold` 模式用于测量真正的冷启动（BDN 的预热会把词典加载排除在统计之外）。
