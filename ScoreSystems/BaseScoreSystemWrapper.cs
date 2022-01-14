using System;
using UnityEngine;

namespace SRXDScoreMod; 

internal class BaseScoreSystemWrapper : IScoreSystem {
    private static readonly CustomTimingAccuracy PERFECT = new("Perfect", NoteTimingAccuracy.Perfect);
    private static readonly CustomTimingAccuracy EARLY = new("Early", NoteTimingAccuracy.Early);
    private static readonly CustomTimingAccuracy LATE = new("Late", NoteTimingAccuracy.Late);

    public int Score => scoreState.FinalisedScore > 0 ? scoreState.FinalisedScore : scoreState.totalNoteScore;

    public int HighSecondaryScore => 0;
        
    public int MaxScore => 0;
        
    public int MaxScoreSoFar => 0;

    public int HighScore => 0;

    public int SecondaryScore => 0;
        
    public int Multiplier => scoreState.Multiplier;
        
    public int Streak => scoreState.combo;

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

    private PlayState playState;
    private PlayState.ScoreState scoreState = new();

    public void Init() {
        playState = Track.Instance.playStateFirst;
        scoreState = playState.scoreState;
    }

    public void Complete() {
        var trackInfoRef = playState.TrackInfoRef;
        TrackDataMetadata metadata = null;
        
        if (playState.TrackDataSetup.IsSetupForSingleTrackSegment)
            metadata = playState.TrackDataSetup.TrackDataSegmentForSingleTrackDataSetup.GetTrackDataMetadata();
        
        int highScore = 0;

        if (metadata != null && trackInfoRef?.Stats != null) {
            var scoreForDifficulty = trackInfoRef.Stats.GetBestScoreForDifficulty(metadata);

            if (scoreForDifficulty != null)
                highScore = scoreForDifficulty.GetValue();
        }

        IsHighScore = Score > highScore;
        Rank = playState.trackData.GetRankCalculatedFromScore(Score);
    }
    
    public HighScoreInfo GetHighScoreInfoForTrack(MetadataHandle handle, TrackData.DifficultyType difficultyType) {
        var metadataSet = handle.TrackDataMetadata;
        
        return GetHighScoreInfoForTrack(
            handle.TrackInfoRef,
            metadataSet.GetMetadataForActiveIndex(metadataSet.GetClosestActiveIndexForDifficulty(difficultyType)));
    }

    public HighScoreInfo GetHighScoreInfoForTrack(TrackInfoAssetReference trackInfoRef, TrackDataMetadata metadata) {
        var stats = trackInfoRef.Stats;
        var fullComboState = FullComboState.None;
        int score = stats.GetBestScoreForDifficulty(metadata).GetValue();
        int streak = stats.GetBestStreakForDifficulty(metadata).GetValue();

        if (score > metadata.PfcScoreThreshold && score > 0)
            fullComboState = FullComboState.PerfectFullCombo;
        else if (streak >= metadata.MaxCombo && streak > 0)
            fullComboState = FullComboState.FullCombo;

        string rank = metadata.GetRankCalculatedFromScore(score);

        return new HighScoreInfo(
            score,
            streak,
            metadata.MaxNoteScore,
            0,
            rank,
            fullComboState);
    }
}