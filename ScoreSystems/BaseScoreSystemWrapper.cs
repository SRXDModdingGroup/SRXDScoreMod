﻿using System;
using UnityEngine;

namespace SRXDScoreMod; 

internal class BaseScoreSystemWrapper : IScoreSystem {
    private static readonly CustomTimingAccuracy PERFECT = new("Perfect", "Perfect", Color.cyan, NoteTimingAccuracy.Perfect);
    private static readonly CustomTimingAccuracy EARLY = new("Early", "Good", Color.yellow, NoteTimingAccuracy.Early);
    private static readonly CustomTimingAccuracy LATE = new("Late", "Good", Color.yellow, NoteTimingAccuracy.Late);

    public int Score => scoreState.FinalisedScore > 0 ? scoreState.FinalisedScore : scoreState.totalNoteScore;

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
    
    public TimingWindow[] TimingWindowsForDisplay { get; } = {
        new (EARLY, -130f),
        new (EARLY, -50f),
        new (PERFECT, 50f),
        new (LATE, 130f)
    };

    private GameplayVariables gameplayVariables;
    private PlayState.ScoreState scoreState = new();

    public void Init(PlayState playState) {
        gameplayVariables = GameplayVariables.Instance;
        scoreState = playState.scoreState;
    }

    public void Complete(PlayState playState) {
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

    private CustomTimingAccuracy BaseToCustomTimingAccuracy(NoteTimingAccuracy timingAccuracy) => timingAccuracy switch {
        NoteTimingAccuracy.Perfect => PERFECT,
        NoteTimingAccuracy.Early => EARLY,
        NoteTimingAccuracy.Late => LATE,
        _ => PERFECT
    };
}