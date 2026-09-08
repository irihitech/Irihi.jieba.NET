using System;
using System.Collections.Generic;

namespace JiebaNet.Segmenter.FinalSeg;

/// <summary>
/// �ڴʵ��з�֮��ʹ�ô˽ӿڽ����з֣�Ĭ��ʵ��ΪHMM������
/// </summary>
public interface IFinalSeg
{
    IEnumerable<string> Cut(string sentence);
}