using UnityEngine;

namespace SRXDScoreMod {
    public abstract class CustomScoreContainer : IReadOnlyScoreContainer {
        public abstract string Name { get; }
        public abstract int Hash { get; }
        public abstract int Score { get; }
        public abstract int MaxScore { get; }
        public abstract int Multiplier { get; }
        public abstract int Streak { get; }
        public abstract FCStarState StarState { get; }
        public abstract Color StarColor { get; }
        public abstract bool ImplementsScorePrediction { get; }
        public abstract int MaxScoreSoFar { get; }
        public abstract bool ImplementsSecondaryScore { get; }
        public abstract int SecondaryScore { get; }
        public abstract FullComboState FullComboState { get; }
        public abstract string PostGameInfo1Name { get; }
        public abstract string PostGameInfo1Value { get; }
        public abstract string PostGameInfo2Name { get; }
        public abstract string PostGameInfo2Value { get; }
        public abstract string PostGameInfo3Name { get; }
        public abstract string PostGameInfo3Value { get; }
        public abstract string GetRank(int score, int maxScore);
        public abstract HighScoreInfo GetHighScoreInfoForChart(TrackInfoAssetReference trackInfoRef, TrackDataMetadata metadata);
        public abstract HighScoreInfo GetHighScoreInfoForChart(TrackDataMetadata metadata);
    }
}