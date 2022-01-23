using UnityEngine;

namespace SRXDScoreMod; 

internal static class DefaultScoreSystemProfiles {
    private static readonly CustomTimingAccuracy E_OKAY = new("Okay", "Okay", new Color(0.25f, 0.25f, 0.25f), NoteTimingAccuracy.Early);
    private static readonly CustomTimingAccuracy E_GOOD = new("Good", "Good", Color.yellow, NoteTimingAccuracy.Early);
    private static readonly CustomTimingAccuracy E_GREAT = new("Great", "Great", new Color(0f, 1f, 0.1f), NoteTimingAccuracy.Early);
    private static readonly CustomTimingAccuracy PERFECT = new("Perfect", "Perfect", Color.cyan, NoteTimingAccuracy.Perfect);
    private static readonly CustomTimingAccuracy L_GREAT = new("Great", "Great", new Color(0f, 1f, 0.1f), NoteTimingAccuracy.Late);
    private static readonly CustomTimingAccuracy L_GOOD = new("Good", "Good", Color.yellow, NoteTimingAccuracy.Late);
    private static readonly CustomTimingAccuracy L_OKAY = new("Okay", "Okay", new Color(0.25f, 0.25f, 0.25f), NoteTimingAccuracy.Late);

    private static readonly TimingWindow[] PRESS_TIMING_WINDOWS = {
        new(E_OKAY, 4, 0, -0.0667f),
        new(E_GOOD, 8, 0, -0.05f),
        new(E_GREAT, 14, 0, -0.0334f),
        new(PERFECT, 16, 0, -0.0167f),
        new(PERFECT, 16, 1, 0.0167f),
        new(PERFECT, 16, 0, 0.0334f),
        new(L_GREAT, 14, 0, 0.05f),
        new(L_GOOD, 8, 0, 0.0667f),
        new(L_OKAY, 4, 0, 0.15f)
    };
    
    private static readonly TimingWindow[] RELEASE_TIMING_WINDOWS = {
        new(E_OKAY, 3, 0, -0.0834f),
        new(E_GOOD, 6, 0, -0.0667f),
        new(E_GREAT, 10, 0, -0.05f),
        new(PERFECT, 12, 0, -0.0167f),
        new(PERFECT, 12, 1, 0.0167f),
        new(PERFECT, 12, 0, 0.05f),
        new(L_GREAT, 10, 0, 0.0667f),
        new(L_GOOD, 6, 0, 0.0834f),
        new(L_OKAY, 3, 0, 0.15f)
    };

    private static readonly RankThreshold[] RANK_THRESHOLDS = {
        new("S", 0.98f),
        new("A+", 0.965f),
        new("A", 0.95f),
        new("B+", 0.925f),
        new("B", 0.9f),
        new("C+", 0.85f),
        new("C", 0.8f),
        new("D+", 0.75f),
        new("D", 0f)
    };
    
    public static ScoreSystemProfile StandardPPM16 { get; } = new(
        name: "Standard",
        id: "fws16",
        tapTimingWindows: PRESS_TIMING_WINDOWS,
        beatTimingWindows: PRESS_TIMING_WINDOWS,
        liftoffTimingWindows: RELEASE_TIMING_WINDOWS,
        beatReleaseTimingWindows: RELEASE_TIMING_WINDOWS,
        matchPointValue: 4,
        spinStartPointValue: 16,
        holdTickRate: 20,
        beatHoldTickRate: 20,
        spinTickRate: 20,
        scratchTickRate: 20,
        maxMultiplier: 4,
        pointsPerMultiplier: new []{ 16 },
        multiplierChangeForOverbeat: -3,
        multiplierChangeForMissedMatch: -3,
        multiplierChangeForMissedTapOrHold: -3,
        multiplierChangeForMissedBeat: -3,
        multiplierChangeForMissedLiftoff: 0,
        multiplierChangeForMissedBeatRelease: 0,
        multiplierChangeForMissedSpin: -3,
        multiplierChangeForBrokenHold: 0,
        multiplierChangeForBrokenBeatHold: 0,
        multiplierChangeForBrokenSpin: 0,
        multiplierChangeForBrokenScratch: -3,
        sPlusThreshold: 96,
        rankThresholds: RANK_THRESHOLDS);
    
    // public static ScoreSystemProfile StandardPPM32 { get; } = new(
    //     name: "Standard (PPM 32)",
    //     tapTimingWindows: PRESS_TIMING_WINDOWS,
    //     beatTimingWindows: PRESS_TIMING_WINDOWS,
    //     liftoffTimingWindows: RELEASE_TIMING_WINDOWS,
    //     beatReleaseTimingWindows: RELEASE_TIMING_WINDOWS,
    //     matchPointValue: 4,
    //     spinStartPointValue: 16,
    //     holdTickRate: 20,
    //     beatHoldTickRate: 20,
    //     spinTickRate: 20,
    //     scratchTickRate: 20,
    //     maxMultiplier: 4,
    //     pointsPerMultiplier: new []{ 32 },
    //     multiplierChangeForOverbeat: -3,
    //     multiplierChangeForMissedMatch: -3,
    //     multiplierChangeForMissedTapOrHold: -3,
    //     multiplierChangeForMissedBeat: -3,
    //     multiplierChangeForMissedLiftoff: 0,
    //     multiplierChangeForMissedBeatRelease: 0,
    //     multiplierChangeForMissedSpin: -3,
    //     multiplierChangeForBrokenHold: 0,
    //     multiplierChangeForBrokenBeatHold: 0,
    //     multiplierChangeForBrokenSpin: 0,
    //     multiplierChangeForBrokenScratch: -3,
    //     sPlusThreshold: 96,
    //     rankThresholds: RANK_THRESHOLDS);
}