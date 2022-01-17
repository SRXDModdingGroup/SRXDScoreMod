namespace SRXDScoreMod; 

public interface IReadOnlyScoreSystem {
    string Name { get; }
    
    int Score { get; }
        
    int SecondaryScore { get; }
        
    int HighScore { get; }
        
    int HighSecondaryScore { get; }
        
    int MaxPossibleScore { get; }

    int Streak { get; }

    int MaxStreak { get; }
        
    int BestStreak { get; }
        
    bool IsHighScore { get; }
        
    int Multiplier { get; }
        
    FullComboState FullComboState { get; }
        
    string Rank { get; }
        
    string PostGameInfo1Value { get; }
        
    string PostGameInfo2Value { get; }
        
    string PostGameInfo3Value { get; }
    
    public bool ImplementsSecondaryScore { get; }
    
    public bool ImplementsScorePrediction { get; }
    
    public TimingWindow[] TimingWindowsForDisplay { get; }
    
    public HighScoreInfo GetHighScoreInfoForTrack(MetadataHandle handle, TrackData.DifficultyType difficultyType);
}