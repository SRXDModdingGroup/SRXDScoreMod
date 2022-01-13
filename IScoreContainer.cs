using UnityEngine;

namespace SRXDScoreMod; 

public interface IScoreContainer {
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
}