namespace JiebaNet.Segmenter
{
    /// <summary>
    /// An end position (inclusive) of a dictionary word starting at the current
    /// position, together with the pre-computed natural log of its frequency.
    /// </summary>
    internal readonly struct WordEnd
    {
        public readonly int End;
        public readonly double LogFreq;

        public WordEnd(int end, double logFreq)
        {
            End = end;
            LogFreq = logFreq;
        }
    }
}
