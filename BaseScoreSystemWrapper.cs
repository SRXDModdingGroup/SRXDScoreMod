using System;

namespace SRXDScoreMod; 

internal class BaseScoreSystemWrapper : IScoreSystem {
    private static readonly CustomTimingAccuracy PERFECT = new("Perfect", NoteTimingAccuracy.Perfect);
    private static readonly CustomTimingAccuracy EARLY = new("Early", NoteTimingAccuracy.Early);
    private static readonly CustomTimingAccuracy LATE = new("Late", NoteTimingAccuracy.Late);
        
    private static readonly Func<float, string> TrackDataMetadata_GetRankFromNormalizedScore =
        ReflectionUtils.MethodToFunc<float, string>(typeof(TrackDataMetadata), "GetRankFromNormalizedScore");

    public IScoreContainer ScoreContainer => scoreContainer;
    
    public bool ImplementsSecondaryScore { get; }
    
    public bool ImplementsScorePrediction { get; }
    
    public string PostGameInfo1Name { get; }
    
    public string PostGameInfo2Name { get; }
    
    public string PostGameInfo3Name { get; }

    private PlayState playState;
    private BaseScoreContainerWrapper scoreContainer = new ();

    public void Init() {
        playState = Track.Instance.playStateFirst;
        scoreContainer.SetScoreState(playState.scoreState);
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
        
        scoreContainer.SetPostGameInfo(scoreContainer.Score > highScore, playState.trackData.GetRankCalculatedFromScore(scoreContainer.Score));
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

        return new HighScoreInfo(
            score,
            streak,
            metadata.MaxNoteScore,
            0,
            metadata.GetRankCalculatedFromScore(score),
            fullComboState);
    }
}