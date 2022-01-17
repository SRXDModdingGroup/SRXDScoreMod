﻿using System.Collections.ObjectModel;

namespace SRXDScoreMod; 

// Contains information about note point values, timing windows, and points per multiplier for each score container
public class ScoreSystemProfile {
    /// <summary>
    /// The name of the score profile
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// A unique identifier for the profile
    /// </summary>
    public string Id { get; }
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
    /// The timing windows and corresponding timing accuracies for taps
    /// </summary>
    public TimingWindow[] TapTimingWindows { get; }
    /// <summary>
    /// The timing windows and corresponding timing accuracies for beats
    /// </summary>
    public TimingWindow[] BeatTimingWindows { get; }
    /// <summary>
    /// The timing windows and corresponding timing accuracies for liftoffs
    /// </summary>
    public TimingWindow[] LiftoffTimingWindows { get; }
    /// <summary>
    /// The timing windows and corresponding timing accuracies for hard beat releases
    /// </summary>
    public TimingWindow[] BeatReleaseTimingWindows { get; }
    /// <summary>
    /// The point threshold for an S+ rank
    /// </summary>
    public int SPlusThreshold { get; }
    /// <summary>
    /// The score percentage thresholds for each rank
    /// </summary>
    public RankThreshold[] RankThresholds { get; }

    private int hash;

    public ScoreSystemProfile(
        string name,
        string id,
        int matchPointValue,
        int spinStartPointValue,
        float holdTickRate,
        float beatHoldTickRate,
        float spinTickRate,
        float scratchTickRate,
        int maxMultiplier,
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
        TimingWindow[] tapTimingWindows,
        TimingWindow[] beatTimingWindows,
        TimingWindow[] liftoffTimingWindows,
        TimingWindow[] beatReleaseTimingWindows,
        int sPlusThreshold,
        RankThreshold[] rankThresholds) {
        Name = name;
        Id = id;
        MatchPointValue = matchPointValue;
        SpinStartPointValue = spinStartPointValue;
        HoldTickRate = holdTickRate;
        BeatHoldTickRate = beatHoldTickRate;
        SpinTickRate = spinTickRate;
        ScratchTickRate = scratchTickRate;
        MaxMultiplier = maxMultiplier;
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
        TapTimingWindows = tapTimingWindows;
        BeatTimingWindows = beatTimingWindows;
        LiftoffTimingWindows = liftoffTimingWindows;
        BeatReleaseTimingWindows = beatReleaseTimingWindows;
        SPlusThreshold = sPlusThreshold;
        RankThresholds = rankThresholds;
        
        hash = HashUtility.Combine(
            matchPointValue,
            spinStartPointValue,
            holdTickRate,
            beatHoldTickRate,
            spinTickRate,
            scratchTickRate,
            maxMultiplier,
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
            multiplierChangeForBrokenScratch,
            tapTimingWindows,
            beatTimingWindows,
            liftoffTimingWindows,
            beatReleaseTimingWindows,
            sPlusThreshold,
            rankThresholds);
    }

    /// <summary>
    /// Applies a set of values to this profile's hash. Any tweakable parameter for a custom score profile should be applied to the hash in the profile's constructor
    /// </summary>
    /// <param name="values">The values to apply to the hash</param>
    protected void ApplyValuesToHash(params object[] values) => hash = HashUtility.Combine(hash, HashUtility.Combine(values));
}