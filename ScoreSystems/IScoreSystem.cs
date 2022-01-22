using System.Collections.Generic;
using UnityEngine;

namespace SRXDScoreMod; 

internal interface IScoreSystem : IReadOnlyScoreSystem {
    public string Id { get; }
        
    int MaxPossibleScoreSoFar { get; }

    FullComboState StarState { get; }

    Color StarColor { get; }
    
    public string PostGameInfo1Name { get; }
    
    public string PostGameInfo2Name { get; }
    
    public string PostGameInfo3Name { get; }
    
    public List<ColoredGraphValue> PerformanceGraphValues { get; }
    
    public PieGraphValue[] PieGraphValues { get; }
    
    public void Init(PlayState playState, int startIndex, int endIndex);

    public void Complete(PlayState playState);

    public int GetPointValueForSustain(int baseValue, int noteIndex);

    public CustomTimingAccuracy GetTimingAccuracyForTap(float timeOffset);

    public CustomTimingAccuracy GetTimingAccuracyForBeat(float timeOffset);

    public CustomTimingAccuracy GetTimingAccuracyForLiftoff(float timeOffset);

    public CustomTimingAccuracy GetTimingAccuracyForBeatRelease(float timeOffset);

    public HighScoreInfo GetHighScoreInfoForTrack(TrackInfoAssetReference trackInfoRef, TrackDataMetadata metadata);
}