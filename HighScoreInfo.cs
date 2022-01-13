namespace SRXDScoreMod; 

public class HighScoreInfo {
    public int Score { get; }
    public int Streak { get; }
    public int MaxScore { get; }
    public int SecondaryScore { get; }
            
    public HighScoreInfo(int score, int streak, int maxScore, int secondaryScore) {
        Score = score;
        Streak = streak;
        MaxScore = maxScore;
        SecondaryScore = secondaryScore;
    }
}