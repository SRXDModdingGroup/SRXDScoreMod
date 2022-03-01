namespace SRXDScoreMod; 

/// <summary>
/// Contains information about a rank threshold
/// </summary>
public class RankThreshold {
    /// <summary>
    /// The name of the rank
    /// </summary>
    public string Rank { get; }
    
    /// <summary>
    /// The minimum score fraction threshold needed to attain this rank
    /// </summary>
    public float Threshold { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="rank">The name of the rank</param>
    /// <param name="threshold">The minimum score fraction threshold needed to attain this rank</param>
    public RankThreshold(string rank, float threshold) {
        Rank = rank;
        Threshold = threshold;
    }
}