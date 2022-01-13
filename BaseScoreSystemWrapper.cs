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
}