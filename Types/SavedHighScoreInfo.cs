namespace SRXDScoreMod;

internal readonly struct SavedHighScoreInfo {
    public int Score { get; }
    public int Streak { get; }
    public int MaxScore { get; }
    public int MaxStreak { get; }
    public int SecondaryScore { get; }
    public string Hash { get; }
            
    public SavedHighScoreInfo(string key, int score, int streak, int maxScore, int maxStreak, int secondaryScore) {
        Score = score;
        Streak = streak;
        MaxScore = maxScore;
        MaxStreak = maxStreak;
        SecondaryScore = secondaryScore;
        
        unchecked {
            Hash = ((uint) HashUtility.Combine(key, score, streak, maxScore, maxStreak, secondaryScore)).ToString("x8");
        }
    }
}