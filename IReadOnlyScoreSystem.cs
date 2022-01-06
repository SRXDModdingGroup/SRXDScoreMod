using UnityEngine;

namespace SRXDScoreMod {
    public interface IReadOnlyScoreSystem {
        string Name { get; }
        
        int Hash { get; }
        
        int Score { get; }
        
        int Multiplier { get; }

        int Streak { get; }

        FCStarState StarState { get; }

        Color StarColor { get; }

        bool ImplementsScorePrediction { get; }
        
        int MaxScore { get; }
        
        int MaxScoreSoFar { get; }

        bool ImplementsSecondaryScore { get; }
        
        int SecondaryScore { get; }
        
        FullComboState FullComboState { get; }
        
        string PostGameInfo1Name { get; }
        
        string PostGameInfo1Value { get; }
        
        string PostGameInfo2Name { get; }
        
        string PostGameInfo2Value { get; }
        
        string PostGameInfo3Name { get; }
        
        string PostGameInfo3Value { get; }

        string GetRank(int score, int maxScore);

        HighScoreInfo GetHighScoreInfoForChart(TrackInfoAssetReference trackInfoRef, TrackDataMetadata metadata);
    }
}