namespace JiebaNet.Segmenter.PosSeg;

public record Pair(string Word, string Flag)
{
    public override string ToString()
    {
        return $"{Word}/{Flag}";
    }
}