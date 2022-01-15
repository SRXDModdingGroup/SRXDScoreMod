namespace SRXDScoreMod; 

public readonly struct RankThreshold {
    public string Rank { get; }
    
    public float Threshold { get; }

    public RankThreshold(string rank, float threshold) {
        Rank = rank;
        Threshold = threshold;
    }
}