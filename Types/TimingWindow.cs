namespace SRXDScoreMod; 

public readonly struct TimingWindow : IHashable {
    public CustomTimingAccuracy TimingAccuracy { get; }
    
    public float UpperBound { get; }

    public TimingWindow(CustomTimingAccuracy timingAccuracy, float upperBound) {
        TimingAccuracy = timingAccuracy;
        UpperBound = upperBound;
    }

    public int GetStableHash() => HashUtility.Combine(TimingAccuracy, UpperBound);
}