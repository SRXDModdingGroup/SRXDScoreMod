namespace SRXDScoreMod; 

public readonly struct RankThreshold : IHashable {
    public string Rank { get; }
    
    public float Threshold { get; }

    public RankThreshold(string rank, float threshold) {
        Rank = rank;
        Threshold = threshold;
    }

    public int GetStableHash() => HashUtility.Combine(Rank, Threshold);
}