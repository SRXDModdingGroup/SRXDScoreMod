using SMU.Utilities;

namespace SRXDScoreMod; 

/// <summary>
/// Contains information about note point values, timing windows, and points per multiplier for each score container
/// </summary>
public class ScoreSystemProfile {
    /// <summary>
    /// The name of the score profile
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// The unique identifier for the profile. This value should not be changed after the profile is first introduced
    /// </summary>
    public string Id { get; }
    /// <summary>
    /// The timing windows and corresponding point values and timing accuracies for taps and holds
    /// </summary>
    /// <remarks>Timing accuracies are determined using the signed timing error value, so timing windows must account for negative error values, in which the upper threshold should be the boundary that is closer to 0</remarks>
    public TimingWindow[] TapTimingWindows { get; }
    /// <summary>
    /// The timing windows and corresponding point values and timing accuracies for beats
    /// </summary>
    /// <remarks>Timing accuracies are determined using the signed timing error value, so timing windows must account for negative error values, in which the upper threshold should be the boundary that is closer to 0</remarks>
    public TimingWindow[] BeatTimingWindows { get; }
    /// <summary>
    /// The timing windows and corresponding point values and timing accuracies for liftoffs
    /// </summary>
    /// <remarks>Timing accuracies are determined using the signed timing error value, so timing windows must account for negative error values, in which the upper threshold should be the boundary that is closer to 0</remarks>
    public TimingWindow[] LiftoffTimingWindows { get; }
    /// <summary>
    /// The timing windows and corresponding point values and timing accuracies for hard beat releases
    /// </summary>
    /// <remarks>Timing accuracies are determined using the signed timing error value, so timing windows must account for negative error values, in which the upper threshold should be the boundary that is closer to 0</remarks>
    public TimingWindow[] BeatReleaseTimingWindows { get; }
    /// <summary>
    /// The point value of a match note
    /// </summary>
    public int MatchPointValue { get; }
    /// <summary>
    /// The point value for hitting the start of a spin
    /// </summary>
    public int SpinStartPointValue { get; }
    /// <summary>
    /// The point tick rate for holds
    /// </summary>
    public float HoldTickRate { get; }
    /// <summary>
    /// The point tick rate for beat holds
    /// </summary>
    public float BeatHoldTickRate { get; }
    /// <summary>
    /// The point tick rate for spins
    /// </summary>
    public float SpinTickRate { get; }
    /// <summary>
    /// The point tick rate for scratches
    /// </summary>
    public float ScratchTickRate { get; }
    /// <summary>
    /// The maximum multiplier
    /// </summary>
    public int MaxMultiplier { get; }
    /// <summary>
    /// The number of points that must be gained to advance to the next multiplier. Multipliers beyond the length of the array will use the last value
    /// </summary>
    public int[] PointsPerMultiplier { get; }
    /// <summary>
    /// The change in multiplier after an overbeat. Should be negative or 0
    /// </summary>
    public int MultiplierChangeForOverbeat { get; }
    /// <summary>
    /// The change in multiplier after missing a match. Should be negative or 0
    /// </summary>
    public int MultiplierChangeForMissedMatch { get; }
    /// <summary>
    /// The change in multiplier after missing a tap or hold. Should be negative or 0
    /// </summary>
    public int MultiplierChangeForMissedTapOrHold { get; }
    /// <summary>
    /// The change in multiplier after missing a beat. Should be negative or 0
    /// </summary>
    public int MultiplierChangeForMissedBeat { get; }
    /// <summary>
    /// The change in multiplier after missing a liftoff. Should be negative or 0
    /// </summary>
    public int MultiplierChangeForMissedLiftoff { get; }
    /// <summary>
    /// The change in multiplier after missing a hard beat release. Should be negative or 0
    /// </summary>
    public int MultiplierChangeForMissedBeatRelease { get; }
    /// <summary>
    /// The change in multiplier after missing a spin. Should be negative or 0
    /// </summary>
    public int MultiplierChangeForMissedSpin { get; }
    /// <summary>
    /// The change in multiplier after breaking a hold. Should be negative or 0
    /// </summary>
    public int MultiplierChangeForBrokenHold { get; }
    /// <summary>
    /// The change in multiplier after breaking a beat hold. Should be negative or 0
    /// </summary>
    public int MultiplierChangeForBrokenBeatHold { get; }
    /// <summary>
    /// The change in multiplier after breaking a spin. Should be negative or 0
    /// </summary>
    public int MultiplierChangeForBrokenSpin { get; }
    /// <summary>
    /// The change in multiplier after breaking a scratch. Should be negative or 0
    /// </summary>
    public int MultiplierChangeForBrokenScratch { get; }
    /// <summary>
    /// The point threshold for a Great (green) section
    /// </summary>
    public int GreatSectionThreshold { get; }
    /// <summary>
    /// The point threshold for an S+ rank
    /// </summary>
    public int SPlusThreshold { get; }
    /// <summary>
    /// The score percentage thresholds for each rank
    /// </summary>
    public RankThreshold[] RankThresholds { get; }
    
    internal string Key { get; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="name">The name of the score profile</param>
    /// <param name="id">The unique identifier for the profile. This value should not be changed after the profile is first introduced</param>
    /// <param name="tapTimingWindows">The timing windows and corresponding point values and timing accuracies for taps and holds</param>
    /// <param name="beatTimingWindows">The timing windows and corresponding point values and timing accuracies for beats</param>
    /// <param name="liftoffTimingWindows">The timing windows and corresponding point values and timing accuracies for liftoffs</param>
    /// <param name="beatReleaseTimingWindows">The timing windows and corresponding point values and timing accuracies for hard beat releases</param>
    /// <param name="matchPointValue">The point value of a match note</param>
    /// <param name="spinStartPointValue">The point value for hitting the start of a spin</param>
    /// <param name="holdTickRate">The point tick rate for holds</param>
    /// <param name="beatHoldTickRate">The point tick rate for beat holds</param>
    /// <param name="spinTickRate">The point tick rate for spins</param>
    /// <param name="scratchTickRate">The point tick rate for scratches</param>
    /// <param name="maxMultiplier">The maximum multiplier</param>
    /// <param name="pointsPerMultiplier">The number of points that must be gained to advance to the next multiplier. Multipliers beyond the length of the array will use the last value</param>
    /// <param name="multiplierChangeForOverbeat">The change in multiplier after an overbeat. Should be negative or 0</param>
    /// <param name="multiplierChangeForMissedMatch">The change in multiplier after missing a match. Should be negative or 0</param>
    /// <param name="multiplierChangeForMissedTapOrHold">The change in multiplier after missing a tap or hold. Should be negative or 0</param>
    /// <param name="multiplierChangeForMissedBeat">The change in multiplier after missing a beat. Should be negative or 0</param>
    /// <param name="multiplierChangeForMissedLiftoff">The change in multiplier after missing a liftoff. Should be negative or 0</param>
    /// <param name="multiplierChangeForMissedBeatRelease">The change in multiplier after missing a hard beat release. Should be negative or 0</param>
    /// <param name="multiplierChangeForMissedSpin">The change in multiplier after missing a spin. Should be negative or 0</param>
    /// <param name="multiplierChangeForBrokenHold">The change in multiplier after breaking a hold. Should be negative or 0</param>
    /// <param name="multiplierChangeForBrokenBeatHold">The change in multiplier after breaking a beat hold. Should be negative or 0</param>
    /// <param name="multiplierChangeForBrokenSpin">The change in multiplier after breaking a spin. Should be negative or 0</param>
    /// <param name="multiplierChangeForBrokenScratch">The change in multiplier after breaking a scratch. Should be negative or 0</param>
    /// <param name="greatSectionThreshold">The point threshold for a Great (green) section</param>
    /// <param name="sPlusThreshold">The point threshold for an S+ rank</param>
    /// <param name="rankThresholds">The score percentage thresholds for each rank</param>
    /// <remarks>Timing accuracies are determined using the signed timing error value, so timing windows must account for negative error values, in which the upper threshold should be the boundary that is closer to 0</remarks>
    public ScoreSystemProfile(
        string name,
        string id,
        TimingWindow[] tapTimingWindows,
        TimingWindow[] beatTimingWindows,
        TimingWindow[] liftoffTimingWindows,
        TimingWindow[] beatReleaseTimingWindows,
        int matchPointValue,
        int spinStartPointValue,
        float holdTickRate,
        float beatHoldTickRate,
        float spinTickRate,
        float scratchTickRate,
        int maxMultiplier,
        int[] pointsPerMultiplier,
        int multiplierChangeForOverbeat,
        int multiplierChangeForMissedMatch,
        int multiplierChangeForMissedTapOrHold,
        int multiplierChangeForMissedBeat,
        int multiplierChangeForMissedLiftoff,
        int multiplierChangeForMissedBeatRelease,
        int multiplierChangeForMissedSpin,
        int multiplierChangeForBrokenHold,
        int multiplierChangeForBrokenBeatHold,
        int multiplierChangeForBrokenSpin,
        int multiplierChangeForBrokenScratch,
        int greatSectionThreshold,
        int sPlusThreshold,
        RankThreshold[] rankThresholds) {
        Name = name;
        Id = id.Replace(' ', '_');
        TapTimingWindows = tapTimingWindows;
        BeatTimingWindows = beatTimingWindows;
        LiftoffTimingWindows = liftoffTimingWindows;
        BeatReleaseTimingWindows = beatReleaseTimingWindows;
        MatchPointValue = matchPointValue;
        SpinStartPointValue = spinStartPointValue;
        HoldTickRate = holdTickRate;
        BeatHoldTickRate = beatHoldTickRate;
        SpinTickRate = spinTickRate;
        ScratchTickRate = scratchTickRate;
        MaxMultiplier = maxMultiplier;
        PointsPerMultiplier = pointsPerMultiplier;
        MultiplierChangeForOverbeat = multiplierChangeForOverbeat;
        MultiplierChangeForMissedMatch = multiplierChangeForMissedMatch;
        MultiplierChangeForMissedTapOrHold = multiplierChangeForMissedTapOrHold;
        MultiplierChangeForMissedBeat = multiplierChangeForMissedBeat;
        MultiplierChangeForMissedLiftoff = multiplierChangeForMissedLiftoff;
        MultiplierChangeForMissedBeatRelease = multiplierChangeForMissedBeatRelease;
        MultiplierChangeForMissedSpin = multiplierChangeForMissedSpin;
        MultiplierChangeForBrokenHold = multiplierChangeForBrokenHold;
        MultiplierChangeForBrokenBeatHold = multiplierChangeForBrokenBeatHold;
        MultiplierChangeForBrokenSpin = multiplierChangeForBrokenSpin;
        MultiplierChangeForBrokenScratch = multiplierChangeForBrokenScratch;
        GreatSectionThreshold = greatSectionThreshold;
        SPlusThreshold = sPlusThreshold;
        RankThresholds = rankThresholds;

        int hash = HashUtility.Combine(
            id,
            tapTimingWindows,
            beatTimingWindows,
            liftoffTimingWindows,
            beatReleaseTimingWindows,
            matchPointValue,
            spinStartPointValue,
            holdTickRate,
            beatHoldTickRate,
            spinTickRate,
            scratchTickRate,
            maxMultiplier,
            pointsPerMultiplier,
            multiplierChangeForOverbeat,
            multiplierChangeForMissedMatch,
            multiplierChangeForMissedTapOrHold,
            multiplierChangeForMissedBeat,
            multiplierChangeForMissedLiftoff,
            multiplierChangeForMissedBeatRelease,
            multiplierChangeForMissedSpin,
            multiplierChangeForBrokenHold,
            multiplierChangeForBrokenBeatHold,
            multiplierChangeForBrokenSpin,
            multiplierChangeForBrokenScratch);
        
        unchecked {
            Key = $"{Id}_{(uint) hash:x8}";
        }
    }
}