using UnityEngine;

namespace SRXDScoreMod {
    public interface IReadOnlyScoreContainer {
        string Name { get; }
        
        int Hash { get; }
        
        int Score { get; }
        
        int Multiplier { get; }

        int Streak { get; }

        FullComboState StarState { get; }

        Color StarColor { get; }

        bool ImplementsScorePrediction { get; }
        
        int MaxScore { get; }
        
        int MaxScoreSoFar { get; }
        
        int HighScore { get; }

        bool ImplementsSecondaryScore { get; }
        
        int SecondaryScore { get; }
        
        FullComboState FullComboState { get; }
        
        bool IsHighScore { get; }
        
        string Rank { get; }
        
        string PostGameInfo1Name { get; }
        
        string PostGameInfo1Value { get; }
        
        string PostGameInfo2Name { get; }
        
        string PostGameInfo2Value { get; }
        
        string PostGameInfo3Name { get; }
        
        string PostGameInfo3Value { get; }
    }
}