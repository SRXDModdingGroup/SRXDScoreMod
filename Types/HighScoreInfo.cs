using System;

namespace SRXDScoreMod; 

public class HighScoreInfo {
    public static HighScoreInfo Blank { get; } = new(0, 0, 0, 0, string.Empty, FullComboState.None);
    
    public int Score { get; }
    public int Streak { get; }
    public int MaxScore { get; }
    public int SecondaryScore { get; }
    public string Rank { get; }
    public FullComboState FullComboState { get; }
            
    public HighScoreInfo(int score, int streak, int maxScore, int secondaryScore, string rank, FullComboState fullComboState) {
        Score = score;
        Streak = streak;
        MaxScore = maxScore;
        SecondaryScore = secondaryScore;
        Rank = rank;
        FullComboState = fullComboState;
    }
    
    public string GetScoreString() {
        if (SecondaryScore == 0)
            return Score.ToString();
        
        return $"<line-height=50%>{Score}\n<size=50%>+{SecondaryScore}";
    }

    public string GetStreakString() {
        switch (FullComboState) {
            case FullComboState.None:
                return Streak.ToString();
            case FullComboState.FullCombo:
                return "FC";
            case FullComboState.PerfectFullCombo:
            default:
                return "PFC";
        }
    }
}