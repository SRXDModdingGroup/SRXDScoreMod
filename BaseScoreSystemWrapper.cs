using System;
using UnityEngine;

namespace SRXDScoreMod {
    public class BaseScoreSystemWrapper : IReadOnlyScoreSystem {
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

        public string PostGameInfo1Name => scoreState.AccuracyBonus > 0 ? "Accuracy" : string.Empty;

        public string PostGameInfo1Value => scoreState.AccuracyBonus.ToString();

        public string PostGameInfo2Name => string.Empty;

        public string PostGameInfo2Value => string.Empty;
        
        public string PostGameInfo3Name => scoreState.fullComboState == FullComboState.PerfectFullCombo ? "PFC" : string.Empty;

        public string PostGameInfo3Value => scoreState.PfcBonus.ToString();

        private PlayState.ScoreState scoreState;

        public void SetScoreState(PlayState.ScoreState scoreState) => this.scoreState = scoreState;
        
        public string GetRank(int score, int maxScore) {
            if (score >= Mathf.RoundToInt(maxScore * 1.1f))
                return "S+";

            return TrackDataMetadata_GetRankFromNormalizedScore((float) score / maxScore);
        }

        public CustomTimingAccuracy GetTimingAccuracyForTap(float timingOffset) => GetCustomTimingAccuracy(GameplayVariables.Instance.GetTimingAccuracy(timingOffset));

        public CustomTimingAccuracy GetTimingAccuracyForBeat(float timingOffset) => GetCustomTimingAccuracy(GameplayVariables.Instance.GetTimingAccuracyForBeat(timingOffset));

        public CustomTimingAccuracy GetTimingAccuracyForLiftoff(float timingOffset) => GetCustomTimingAccuracy(GameplayVariables.Instance.GetTimingAccuracy(timingOffset));

        public CustomTimingAccuracy GetTimingAccuracyForHardBeatRelease(float timingOffset) => GetCustomTimingAccuracy(GameplayVariables.Instance.GetTimingAccuracyForBeat(timingOffset));

        public HighScoreInfo GetHighScoreInfoForChart(TrackInfoAssetReference trackInfoRef, TrackDataMetadata metadata) {
            var stats = trackInfoRef.Stats;

            return new HighScoreInfo(stats.GetBestScoreForDifficulty(metadata).GetValue(), stats.GetBestStreakForDifficulty(metadata).GetValue(), metadata.MaxNoteScore, 0);
        }

        private static CustomTimingAccuracy GetCustomTimingAccuracy(NoteTimingAccuracy timingAccuracy) => timingAccuracy switch {
            NoteTimingAccuracy.Perfect => PERFECT,
            NoteTimingAccuracy.Early => EARLY,
            NoteTimingAccuracy.Late => LATE,
            _ => PERFECT
        };
    }
}