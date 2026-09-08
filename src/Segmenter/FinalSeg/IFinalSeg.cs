using System.Collections.Generic;

namespace JiebaNet.Segmenter.FinalSeg;

/// <summary>
/// 最终分词接口，用于实现不同的分词算法，默认实现为HMM分词算法
/// 
/// </summary>
public interface IFinalSeg
{
    IEnumerable<string> Cut(string sentence);
}