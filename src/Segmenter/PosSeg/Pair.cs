namespace JiebaNet.Segmenter.PosSeg;

public record Pair
{
    public string Word { get; }
    public string Flag { get; }
    public Pair(string word, string flag)
    {
        Word = word;
        Flag = flag;
    }

    public override string ToString()
    {
        return $"{Word}/{Flag}";
    }
}