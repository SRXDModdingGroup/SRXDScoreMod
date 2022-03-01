using SMU.Utilities;

namespace SRXDScoreMod; 

/// <summary>
/// Contains information about a timing window
/// </summary>
public class TimingWindow : IHashable {
    /// <summary>
    /// The timing accuracy given when hitting in this window
    /// </summary>
    public CustomTimingAccuracy TimingAccuracy { get; }
    
    /// <summary>
    /// The points gained when hitting in this window
    /// </summary>
    public int PointValue { get; }
    
    /// <summary>
    /// The secondary points gained when hitting in this window
    /// </summary>
    public int SecondaryPointValue { get; }
    
    /// <summary>
    /// The upper timing error threshold for this window
    /// </summary>
    /// <remarks>Timing accuracies are determined using the signed timing error value, so timing windows must account for negative error values, in which the upper threshold should be the boundary that is closer to 0</remarks>
    public float UpperBound { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="timingAccuracy">The timing accuracy given when hitting in this window</param>
    /// <param name="pointValue">The points gained when hitting in this window</param>
    /// <param name="secondaryPointValue">The secondary points gained when hitting in this window</param>
    /// <param name="upperBound">The upper timing error threshold for this window</param>
    /// <remarks>Timing accuracies are determined using the signed timing error value, so timing windows must account for negative error values, in which the upper threshold should be the boundary that is closer to 0</remarks>
    public TimingWindow(CustomTimingAccuracy timingAccuracy, int pointValue, int secondaryPointValue, float upperBound) {
        TimingAccuracy = timingAccuracy;
        PointValue = pointValue;
        SecondaryPointValue = secondaryPointValue;
        UpperBound = upperBound;
    }

    /// <summary>
    /// Gets a hash value used to identify this timing window
    /// </summary>
    /// <returns></returns>
    public int GetStableHash() => HashUtility.Combine(PointValue, SecondaryPointValue, UpperBound);
}