namespace SRXDScoreMod; 

public class TimingWindow : IHashable {
    public CustomTimingAccuracy TimingAccuracy { get; }
    
    public int PointValue { get; }
    
    public int SecondaryPointValue { get; }
    
    public float UpperBound { get; }

    public TimingWindow(CustomTimingAccuracy timingAccuracy, int pointValue, int secondaryPointValue, float upperBound) {
        TimingAccuracy = timingAccuracy;
        PointValue = pointValue;
        SecondaryPointValue = secondaryPointValue;
        UpperBound = upperBound;
    }

    public int GetStableHash() => HashUtility.Combine(TimingAccuracy, UpperBound);
}