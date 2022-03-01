namespace SRXDScoreMod; 

/// <summary>
/// Container for information about a high score
/// </summary>
public class HighScoreInfo {
    /// <summary>
    /// The high score
    /// </summary>
    public int Score { get; }
    /// <summary>
    /// The best streak
    /// </summary>
    public int Streak { get; }
    /// <summary>
    /// The secondary score associated with the high score
    /// </summary>
    public int SecondaryScore { get; }
    /// <summary>
    /// The rank of the high score
    /// </summary>
    public string Rank { get; }
    /// <summary>
    /// The best full combo state achieved
    /// </summary>
    public FullComboState FullComboState { get; }
    
    internal HighScoreInfo(int score, int streak, int secondaryScore, string rank, FullComboState fullComboState) {
        Score = score;
        Streak = streak;
        SecondaryScore = secondaryScore;
        Rank = rank;
        FullComboState = fullComboState;
    }
    
    internal string GetScoreString() {
        if (SecondaryScore == 0)
            return Score.ToString();
        
        return $"<line-height=50%>{Score}\n<size=50%>+{SecondaryScore}";
    }

    internal string GetStreakString() {
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