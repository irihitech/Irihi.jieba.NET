namespace JiebaNet.Segmenter;

/// <summary>
/// An end position (inclusive) of a dictionary word starting at the current
/// position, together with the pre-computed natural log of its frequency.
/// </summary>
internal readonly record struct WordEnd(int End, double LogFreq);