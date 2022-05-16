using System.Collections.Generic;
using UnityEngine;

namespace SRXDScoreMod; 

internal class BaseScoreSystemWrapper : IScoreSystemInternal {
    private static readonly CustomTimingAccuracy EARLY = new("Early", "Good", Color.yellow, NoteTimingAccuracy.Early);
    private static readonly CustomTimingAccuracy PERFECT = new("Perfect", "Perfect", Color.cyan, NoteTimingAccuracy.Perfect);
    private static readonly CustomTimingAccuracy LATE = new("Late", "Good", Color.yellow, NoteTimingAccuracy.Late);

    public string Name => "Base";

    public string Id => "base";

    public string Key => "base_0.15";

    public int Score => ScoreMod.GetModifiedScore(scoreState.FinalisedScore > 0 ? scoreState.FinalisedScore : scoreState.totalNoteScore);

    public int HighSecondaryScore => 0;
        
    public int MaxPossibleScore => 0;
        
    public int MaxPossibleScoreSoFar => 0;

    public int HighScore => 0;

    public int SecondaryScore => 0;
        
    public int Multiplier => scoreState.Multiplier;
        
    public int Streak => scoreState.combo;

    public int MaxStreak => scoreState.maxCombo;

    public int BestStreak => 0;

    public FullComboState StarState => scoreState.fullComboState;
        
    public Color StarColor => Color.cyan;

    public FullComboState FullComboState => scoreState.fullComboState;
        
    public bool IsHighScore { get; private set; }
        
    public string Rank { get; private set; }

    public string PostGameInfo1Value => scoreState.AccuracyBonus > 0 ? scoreState.AccuracyBonus.ToString() : string.Empty;

    public string PostGameInfo2Value => string.Empty;

    public string PostGameInfo3Value => scoreState.fullComboState == FullComboState.PerfectFullCombo ? scoreState.PfcBonus.ToString() : string.Empty;

    public bool ImplementsSecondaryScore => false;

    public bool ImplementsScorePrediction => false;

    public string PostGameInfo1Name => "Accuracy";

    public string PostGameInfo2Name => string.Empty;

    public string PostGameInfo3Name => "PFC";

    public List<ColoredGraphValue> PerformanceGraphValues { get; }
    
    public PieGraphValue[] PieGraphValues { get; }

    public TimingWindow[] TimingWindowsForDisplay { get; } = {
        new (EARLY, 0, 0, -0.05f),
        new (PERFECT, 0, 0, 0.05f),
        new (LATE, 0, 0, 0.13f)
    };

    private GameplayVariables gameplayVariables;
    private PlayState.ScoreState scoreState = new();

    public BaseScoreSystemWrapper() {
        PerformanceGraphValues = new List<ColoredGraphValue>();
        PieGraphValues = new PieGraphValue[7];
    }

    public void Init(PlayState playState, int startIndex, int endIndex) {
        gameplayVariables = GameplayVariables.Instance;
        scoreState = playState.scoreState;
    }

    public void Complete(PlayState playState) {
        var trackInfoRef = playState.TrackInfoRef;
        
        TrackDataMetadata metadata = null;
        
        if (playState.TrackDataSetup.IsSetupForSingleTrackSegment)
            metadata = playState.TrackDataSetup.TrackDataSegmentForSingleTrackDataSetup.GetTrackDataMetadata();
        
        if (ScoreMod.AnyModifiersEnabled) {
            var modifierSet = ScoreMod.CurrentModifierSet;
            
            IsHighScore = HighScoresContainer.TrySetHighScore(playState.TrackInfoRef, playState.CurrentDifficulty, this, modifierSet, new SavedHighScoreInfo(
                string.Empty,
                Score,
                MaxStreak,
                metadata?.MaxNoteScore ?? 0,
                metadata?.MaxCombo ?? 0,
                SecondaryScore,
                modifierSet));
        }
        else {
            int highScore = 0;

            if (metadata != null && trackInfoRef?.Stats != null) {
                var scoreForDifficulty = trackInfoRef.Stats.GetBestScoreForDifficulty(metadata);

                if (scoreForDifficulty != null)
                    highScore = scoreForDifficulty.GetValue();
            }

            IsHighScore = Score > highScore;
        }

        Rank = playState.trackData.GetRankCalculatedFromScore(Score);
        PerformanceGraphValues.Clear();

        var stats = playState.playStateStats;

        foreach (var section in stats.sections) {
            if (section.maxPossibleScore == 0)
                continue;
            
            float value = Mathf.Clamp((float) section.score / section.maxPossibleScore, 0.05f, 1f);
            Color color;
            
            int lates = section.sectionStatsCollection.GetStat(PlayStateStats.StatType.Late).value;
            int earlies = section.sectionStatsCollection.GetStat(PlayStateStats.StatType.Early).value;
            
            if (section.sectionStatsCollection.GetStat(PlayStateStats.StatType.Missed).value > 0 || section.score == 0)
                color = Color.red;
            else if (section.score >= section.maxPossibleScore && lates <= 0 && earlies <= 0)
                color = Color.cyan;
            else
                color = Color.yellow;
            
            PerformanceGraphValues.Add(new ColoredGraphValue(value, color));
        }

        var pieStats = new[] {
            stats.match,
            stats.hold,
            stats.tap,
            stats.beat,
            stats.release,
            stats.spins,
            stats.scratches
        };

        for (int i = 0; i < 7; i++) {
            var statsForPie = pieStats[i];
            
            int perfect = statsForPie.GetStat(PlayStateStats.StatType.Perfect).value + statsForPie.GetStat(PlayStateStats.StatType.Scored).value;
            int good = statsForPie.GetStat(PlayStateStats.StatType.Early).value + statsForPie.GetStat(PlayStateStats.StatType.Late).value;
            int missed = statsForPie.GetStat(PlayStateStats.StatType.Missed).value;

            PieGraphValues[i] = new PieGraphValue(perfect, good, missed);
        }
    }

    public CustomTimingAccuracy GetTimingAccuracyForTap(float timeOffset) => BaseToCustomTimingAccuracy(gameplayVariables.GetTimingAccuracy(timeOffset));

    public CustomTimingAccuracy GetTimingAccuracyForBeat(float timeOffset) => BaseToCustomTimingAccuracy(gameplayVariables.GetTimingAccuracyForBeat(timeOffset));

    public CustomTimingAccuracy GetTimingAccuracyForLiftoff(float timeOffset) => BaseToCustomTimingAccuracy(gameplayVariables.GetTimingAccuracy(timeOffset));

    public CustomTimingAccuracy GetTimingAccuracyForBeatRelease(float timeOffset) => BaseToCustomTimingAccuracy(gameplayVariables.GetTimingAccuracyForBeat(timeOffset));

    public HighScoreInfo GetHighScoreInfoForTrack(MetadataHandle handle, TrackData.DifficultyType difficultyType) {
        var metadataSet = handle.TrackDataMetadata;
        
        return GetHighScoreInfoForTrack(
            handle.TrackInfoRef,
            metadataSet.GetMetadataForActiveIndex(metadataSet.GetClosestActiveIndexForDifficulty(difficultyType)));
    }

    public HighScoreInfo GetHighScoreInfoForTrack(TrackInfoAssetReference trackInfoRef, TrackDataMetadata metadata) {
        var stats = trackInfoRef.Stats;
        var fullComboState = FullComboState.None;
        int score;

        if (ScoreMod.AnyModifiersEnabled)
            score = HighScoresContainer.GetHighScore(trackInfoRef, metadata.DifficultyType, this, ScoreMod.CurrentModifierSet).Score;
        else
            score = stats.GetBestScoreForDifficulty(metadata).GetValue();

        int streak = stats.GetBestStreakForDifficulty(metadata).GetValue();

        if (score >= ScoreMod.GetModifiedScore(metadata.PfcScoreThreshold) && score > 0)
            fullComboState = FullComboState.PerfectFullCombo;
        else if (streak >= metadata.MaxCombo && streak > 0)
            fullComboState = FullComboState.FullCombo;

        string rank = metadata.GetRankCalculatedFromScore(score);
        
        return new HighScoreInfo(
            score,
            streak,
            0,
            rank,
            fullComboState);
    }

    private static CustomTimingAccuracy BaseToCustomTimingAccuracy(NoteTimingAccuracy timingAccuracy) => timingAccuracy switch {
        NoteTimingAccuracy.Perfect => PERFECT,
        NoteTimingAccuracy.Early => EARLY,
        NoteTimingAccuracy.Late => LATE,
        _ => PERFECT
    };
}