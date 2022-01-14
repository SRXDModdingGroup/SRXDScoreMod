namespace SRXDScoreMod; 

public interface IScoreSystem : IReadOnlyScoreSystem {
    public void Init();

    public void Complete();

    public HighScoreInfo GetHighScoreInfoForTrack(TrackInfoAssetReference trackInfoRef, TrackDataMetadata metadata);
}