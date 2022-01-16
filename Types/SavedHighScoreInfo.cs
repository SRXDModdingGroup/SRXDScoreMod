namespace SRXDScoreMod;

public readonly struct SavedHighScoreInfo {
    private const uint HASH_BIAS = 2166136261u;
    private const int HASH_COEFF = 486187739;
    
    public int Score { get; }
    public int Streak { get; }
    public int MaxScore { get; }
    public int MaxStreak { get; }
    public int SecondaryScore { get; }
    public string Hash { get; }
            
    public SavedHighScoreInfo(string id, int score, int streak, int maxScore, int maxStreak, int secondaryScore) {
        Score = score;
        Streak = streak;
        MaxScore = maxScore;
        MaxStreak = maxStreak;
        SecondaryScore = secondaryScore;
                
        unchecked {
            int hash = (int) HASH_BIAS * HASH_COEFF ^ score.GetHashCode();

            hash = hash * HASH_COEFF ^ streak.GetHashCode();
            hash = hash * HASH_COEFF ^ maxScore.GetHashCode();
            hash = hash * HASH_COEFF ^ maxStreak.GetHashCode();
            hash = hash * HASH_COEFF ^ secondaryScore.GetHashCode();
                    
            Hash = ((uint) (hash * HASH_COEFF ^ id.GetHashCode())).ToString("x8");
        }
    }
}