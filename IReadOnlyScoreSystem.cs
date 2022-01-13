using UnityEngine;

namespace SRXDScoreMod; 

public interface IReadOnlyScoreSystem {
    int Score { get; }
        
    int SecondaryScore { get; }
        
    int HighScore { get; }
        
    int HighSecondaryScore { get; }
        
    int MaxScore { get; }
        
    int MaxScoreSoFar { get; }

    int Streak { get; }
        
    int BestStreak { get; }
        
    bool IsHighScore { get; }
        
    int Multiplier { get; }
        
    FullComboState FullComboState { get; }

    FullComboState StarState { get; }

    Color StarColor { get; }
        
    string Rank { get; }
        
    string PostGameInfo1Value { get; }
        
    string PostGameInfo2Value { get; }
        
    string PostGameInfo3Value { get; }
    
    public bool ImplementsSecondaryScore { get; }
    
    public bool ImplementsScorePrediction { get; }
    
    public string PostGameInfo1Name { get; }
    
    public string PostGameInfo2Name { get; }
    
    public string PostGameInfo3Name { get; }
    
    public HighScoreInfo GetHighScoreInfoForTrack(MetadataHandle handle, TrackData.DifficultyType difficultyType);
}