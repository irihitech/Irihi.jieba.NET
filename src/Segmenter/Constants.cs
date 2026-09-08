using System.Collections.Generic;
using System.Linq;

namespace JiebaNet.Segmenter;

public class Constants
{
    public const double MinProb = -3.14e100;

    public static readonly List<string> NounPos = ["n", "ng", "nr", "nrfg", "nrt", "ns", "nt", "nz"];
    public static readonly List<string> VerbPos = ["v", "vd", "vg", "vi", "vn", "vq"];
    public static readonly List<string> NounAndVerbPos = NounPos.Union(VerbPos).ToList();
    public static readonly List<string> IdiomPos = ["i"];
}