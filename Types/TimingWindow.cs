namespace SRXDScoreMod; 

public class TimingWindow : IHashable {
    public int PointValue { get; }
    
    public CustomTimingAccuracy TimingAccuracy { get; }
    
    public float UpperBound { get; }

    public TimingWindow(int pointValue, CustomTimingAccuracy timingAccuracy, float upperBound) {
        PointValue = pointValue;
        TimingAccuracy = timingAccuracy;
        UpperBound = upperBound;
    }

    public int GetStableHash() => HashUtility.Combine(TimingAccuracy, UpperBound);
}