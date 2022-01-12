using System;
using UnityEngine;

namespace SRXDScoreMod {
    public class BaseScoreContainerWrapper : IReadOnlyScoreContainer {
        private static readonly CustomTimingAccuracy PERFECT = new("Perfect", NoteTimingAccuracy.Perfect);
        private static readonly CustomTimingAccuracy EARLY = new("Early", NoteTimingAccuracy.Early);
        private static readonly CustomTimingAccuracy LATE = new("Late", NoteTimingAccuracy.Late);
        
        private static readonly Func<float, string> TrackDataMetadata_GetRankFromNormalizedScore =
            ReflectionUtils.MethodToFunc<float, string>(typeof(TrackDataMetadata), "GetRankFromNormalizedScore");
        
        public string Name => "Base";
        
        public int Hash => Name.GetHashCode();
        
        public int Score => scoreState.FinalisedScore > 0 ? scoreState.FinalisedScore : scoreState.totalNoteScore;
        
        public int Multiplier => scoreState.Multiplier;
        
        public int Streak => scoreState.combo;

        public FullComboState StarState => scoreState.fullComboState;
        
        public Color StarColor => Color.cyan;

        public bool ImplementsScorePrediction => false;
        
        public int MaxScore => 0;
        
        public int MaxScoreSoFar => 0;

        public int HighScore => 0;

        public bool ImplementsSecondaryScore => false;

        public int SecondaryScore => 0;

        public FullComboState FullComboState => scoreState.fullComboState;
        
        public bool IsHighScore { get; private set; }
        
        public string Rank { get; private set; }

        public string PostGameInfo1Name => scoreState.AccuracyBonus > 0 ? "Accuracy" : string.Empty;

        public string PostGameInfo1Value => scoreState.AccuracyBonus.ToString();

        public string PostGameInfo2Name => string.Empty;

        public string PostGameInfo2Value => string.Empty;
        
        public string PostGameInfo3Name => scoreState.fullComboState == FullComboState.PerfectFullCombo ? "PFC" : string.Empty;

        public string PostGameInfo3Value => scoreState.PfcBonus.ToString();

        private PlayState.ScoreState scoreState;

        public void SetScoreState(PlayState.ScoreState scoreState) => this.scoreState = scoreState;

        public void SetPostGameInfo(bool isHighScore, string rank) {
            IsHighScore = isHighScore;
            Rank = rank;
        }
    }
}