namespace SRXDScoreMod; 

internal interface IScoreSystem : IReadOnlyScoreSystem {
    public string Id { get; }
    
    public void Init(PlayState playState);

    public void Complete(PlayState playState);

    public CustomTimingAccuracy GetTimingAccuracyForTap(float timeOffset);

    public CustomTimingAccuracy GetTimingAccuracyForBeat(float timeOffset);

    public CustomTimingAccuracy GetTimingAccuracyForLiftoff(float timeOffset);

    public CustomTimingAccuracy GetTimingAccuracyForBeatRelease(float timeOffset);

    public HighScoreInfo GetHighScoreInfoForTrack(TrackInfoAssetReference trackInfoRef, TrackDataMetadata metadata);
}