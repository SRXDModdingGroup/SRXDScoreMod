namespace SRXDScoreMod; 

public readonly struct TimingWindow {
    public CustomTimingAccuracy TimingAccuracy { get; }
    
    public float UpperBound { get; }

    public TimingWindow(CustomTimingAccuracy timingAccuracy, float upperBound) {
        TimingAccuracy = timingAccuracy;
        UpperBound = upperBound;
    }
}