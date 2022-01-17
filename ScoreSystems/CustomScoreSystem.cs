using System.Collections.Generic;
using UnityEngine;

namespace SRXDScoreMod; 

public abstract class CustomScoreSystem : IScoreSystem {
    #region IReadOnlyScoreSystemProperties

    public string Name { get; private set; }
    
    public string Id { get; private set; }
    
    public int Score { get; private set; }
    
    public int SecondaryScore { get; protected set; }

    public int HighScore { get; private set; }

    public int HighSecondaryScore { get; private set; }
    
    public int MaxPossibleScore { get; private set; }

    public int MaxPossibleScoreSoFar { get; private set; }
    
    public int Streak { get; private set; }
    
    public int MaxStreak { get; private set; }

    public int BestStreak { get; private set; }

    public bool IsHighScore => Score > HighScore;
    
    public int Multiplier { get; private set; }

    public FullComboState FullComboState { get; private set; }

    public FullComboState StarState { get; private set; }

    public Color StarColor { get; private set; }
    
    public string Rank { get; private set; }

    public virtual string PostGameInfo1Value => string.Empty;
    
    public virtual string PostGameInfo2Value => string.Empty;
    
    public virtual string PostGameInfo3Value => string.Empty;

    public virtual bool ImplementsSecondaryScore => false;

    public bool ImplementsScorePrediction => true;
    
    public virtual string PostGameInfo1Name => string.Empty;
    
    public virtual string PostGameInfo2Name => string.Empty;
    
    public virtual string PostGameInfo3Name => string.Empty;

    public TimingWindow[] TimingWindowsForDisplay => tapTimingWindows;

    #endregion

    #region ScoreProfileValues

    private int matchPointValue;
    private int spinStartPointValue;
    private int maxMultiplier;
    private int multiplierChangeForOverbeat;
    private int multiplierChangeForMissedMatch;
    private int multiplierChangeForMissedTapOrHold;
    private int multiplierChangeForMissedBeat;
    private int multiplierChangeForMissedLiftoff;
    private int multiplierChangeForMissedBeatRelease;
    private int multiplierChangeForMissedSpin;
    private int multiplierChangeForBrokenHold;
    private int multiplierChangeForBrokenBeatHold;
    private int multiplierChangeForBrokenSpin;
    private int multiplierChangeForBrokenScratch;
    private int sPlusThreshold;
    private float holdTickRate;
    private float beatHoldTickRate;
    private float spinTickRate;
    private float scratchTickRate;
    private RankThreshold[] rankThresholds;
    private TimingWindow[] tapTimingWindows;
    private TimingWindow[] beatTimingWindows;
    private TimingWindow[] liftoffTimingWindows;
    private TimingWindow[] beatReleaseTimingWindows;

    #endregion
    
    private int maxPossibleStreak;
    private int maxPossibleStreakSoFar;
    private int pointsToNextMultiplier;
    private NoteScoreState[] scoreStates;
    private List<float> overbeatTimes;

    protected CustomScoreSystem(ScoreSystemProfile profile) {
        matchPointValue = profile.MatchPointValue;
        spinStartPointValue = profile.SpinStartPointValue;
        maxMultiplier = profile.MaxMultiplier;
        multiplierChangeForOverbeat = profile.MultiplierChangeForOverbeat;
        multiplierChangeForMissedMatch = profile.MultiplierChangeForMissedMatch;
        multiplierChangeForMissedTapOrHold = profile.MultiplierChangeForMissedTapOrHold;
        multiplierChangeForMissedBeat = profile.MultiplierChangeForMissedBeat;
        multiplierChangeForMissedLiftoff = profile.MultiplierChangeForMissedLiftoff;
        multiplierChangeForMissedBeatRelease = profile.MultiplierChangeForMissedBeatRelease;
        multiplierChangeForMissedSpin = profile.MultiplierChangeForMissedSpin;
        multiplierChangeForBrokenHold = profile.MultiplierChangeForBrokenHold;
        multiplierChangeForBrokenBeatHold = profile.MultiplierChangeForBrokenBeatHold;
        multiplierChangeForBrokenSpin = profile.MultiplierChangeForBrokenSpin;
        multiplierChangeForBrokenScratch = profile.MultiplierChangeForBrokenScratch;
        sPlusThreshold = profile.SPlusThreshold;
        holdTickRate = profile.HoldTickRate;
        beatHoldTickRate = profile.BeatHoldTickRate;
        spinTickRate = profile.SpinTickRate;
        scratchTickRate = profile.ScratchTickRate;
        rankThresholds = profile.RankThresholds;
        tapTimingWindows = profile.TapTimingWindows;
        beatTimingWindows = profile.BeatTimingWindows;
        liftoffTimingWindows = profile.LiftoffTimingWindows;
        beatReleaseTimingWindows = profile.BeatReleaseTimingWindows;
    }

    public virtual void Init(PlayState playState) {
        Score = 0;
        SecondaryScore = 0;
        MaxPossibleScore = 0;
        MaxPossibleScoreSoFar = 0;
        Streak = 0;
        MaxStreak = 0;
        Multiplier = maxMultiplier;
        FullComboState = FullComboState.PerfectFullCombo;
        StarState = FullComboState.PerfectFullCombo;
        StarColor = Color.cyan;
        maxPossibleStreak = 0;
        pointsToNextMultiplier = GetPointsToNextMultiplier(maxMultiplier);
        
        InitScoreStates(playState.trackData);

        if (overbeatTimes == null)
            overbeatTimes = new List<float>();
        else
            overbeatTimes.Clear();

        var highScoreInfo = HighScoresContainer.GetHighScore(playState.TrackInfoRef, playState.trackData.Difficulty, Id, string.Empty);

        HighScore = highScoreInfo.Score;
        HighSecondaryScore = highScoreInfo.SecondaryScore;
        BestStreak = highScoreInfo.Streak;
    }

    public virtual void Complete(PlayState playState) {
        Rank = GetRank(Score, MaxPossibleScore);
        UpdateFullComboState();
    }

    #region TimingAccuracyFunctions

    public CustomTimingAccuracy GetTimingAccuracyForTap(float timeOffset) => GetTimingAccuracy(timeOffset, tapTimingWindows);

    public CustomTimingAccuracy GetTimingAccuracyForBeat(float timeOffset) => GetTimingAccuracy(timeOffset, beatTimingWindows);

    public CustomTimingAccuracy GetTimingAccuracyForLiftoff(float timeOffset) => GetTimingAccuracy(timeOffset, liftoffTimingWindows);

    public CustomTimingAccuracy GetTimingAccuracyForBeatRelease(float timeOffset) => GetTimingAccuracy(timeOffset, beatReleaseTimingWindows);

    #endregion
    
    public HighScoreInfo GetHighScoreInfoForTrack(MetadataHandle handle, TrackData.DifficultyType difficultyType)
        => GetHighScoreInfoForTrack(handle.TrackInfoRef, difficultyType);

    public HighScoreInfo GetHighScoreInfoForTrack(TrackInfoAssetReference trackInfoRef, TrackDataMetadata metadata)
        => GetHighScoreInfoForTrack(trackInfoRef, metadata.DifficultyType);

    protected abstract int GetPointsToNextMultiplier(int currentMultiplier);

    #region TimedNoteValueFunctions

    protected abstract int GetPointValueForTap(float timeOffset);

    protected abstract int GetPointValueForBeat(float timeOffset);

    protected abstract int GetPointValueForLiftoff(float timeOffset);

    protected abstract int GetPointValueForBeatRelease(float timeOffset);

    #endregion

    #region NoteEvents

    internal void HitMatch(int noteIndex) => AddScore(noteIndex, matchPointValue, 1, null, false);

    internal void HitTap(int noteIndex, float timeOffset)
        => AddScore(noteIndex, GetPointValueForTap(timeOffset), 1, GetTimingAccuracyForTap(timeOffset), false);

    internal void HitBeat(int noteIndex, float timeOffset)
        => AddScore(noteIndex, GetPointValueForBeat(timeOffset), 1, GetTimingAccuracyForBeat(timeOffset), false);

    internal void HitLiftoff(int noteIndex, float timeOffset)
        => AddScore(noteIndex, GetPointValueForLiftoff(timeOffset), 1, GetTimingAccuracyForLiftoff(timeOffset), false);

    internal void HitBeatRelease(int noteIndex, float timeOffset)
        => AddScore(noteIndex, GetPointValueForBeatRelease(timeOffset), 1, GetTimingAccuracyForBeatRelease(timeOffset), false);

    internal void HitSpin(int noteIndex) => AddScore(noteIndex, spinStartPointValue, 1, null, false);

    internal void Overbeat(float time) {
        ChangeMultiplier(-1, multiplierChangeForOverbeat);
        
        if (multiplierChangeForOverbeat < 0)
            overbeatTimes.Add(time);
    }

    internal void MissMatch(int noteIndex) => MissNote(noteIndex, multiplierChangeForMissedMatch);

    internal void MissTap(int noteIndex) => MissNote(noteIndex, multiplierChangeForMissedTapOrHold);
    
    internal void MissBeat(int noteIndex) => MissNote(noteIndex, multiplierChangeForMissedBeat);
    
    internal void MissHold(int noteIndex, int endNoteIndex) => MissPairedNote(noteIndex, endNoteIndex, multiplierChangeForMissedTapOrHold);

    internal void MissBeatHold(int noteIndex, int endNoteIndex) => MissPairedNote(noteIndex, endNoteIndex, multiplierChangeForMissedBeat);

    internal void MissLiftoff(int noteIndex) => MissNote(noteIndex, multiplierChangeForMissedLiftoff);
    
    internal void MissBeatRelease(int noteIndex) => MissNote(noteIndex, multiplierChangeForMissedBeatRelease);

    internal void MissSpin(int noteIndex) => MissNote(noteIndex, multiplierChangeForMissedSpin);
    
    internal void BreakHold(int noteIndex, int endNoteIndex) => MissPairedNote(noteIndex, endNoteIndex, multiplierChangeForBrokenHold);
    
    internal void BreakBeatHold(int noteIndex, int endNoteIndex) => MissPairedNote(noteIndex, endNoteIndex, multiplierChangeForBrokenBeatHold);
    
    internal void BreakSpin(int noteIndex) => MissNote(noteIndex, multiplierChangeForBrokenSpin);
    
    internal void BreakScratch(int noteIndex) => MissNote(noteIndex, multiplierChangeForBrokenScratch);

    internal void UpdateHold(int noteIndex, float heldTime) => UpdateSustainedNoteValue(noteIndex, heldTime, holdTickRate);

    internal void UpdateBeatHold(int noteIndex, float heldTime) => UpdateSustainedNoteValue(noteIndex, heldTime, beatHoldTickRate);

    internal void UpdateSpin(int noteIndex, float heldTime) => UpdateSustainedNoteValue(noteIndex, heldTime, spinTickRate);

    internal void UpdateScratch(int noteIndex, float heldTime) => UpdateSustainedNoteValue(noteIndex, heldTime, scratchTickRate);

    internal void CompleteNote(int noteIndex) {
        if (AddRemainingValueToMaxScoreSoFar(noteIndex))
            UpdateFullComboState();
    }

    #endregion

    #region ScoringLogic

    private void AddScore(int noteIndex, int amount, int addStreak, CustomTimingAccuracy timingAccuracy, bool fromSustain) {
        int acc = amount;
        int scoreAdded = 0;
            
        while (Multiplier < maxMultiplier && acc >= pointsToNextMultiplier) {
            scoreAdded += Multiplier * pointsToNextMultiplier;
            acc -= pointsToNextMultiplier;
            ChangeMultiplier(noteIndex, 1);
        }

        if (Multiplier < maxMultiplier)
            pointsToNextMultiplier -= acc;
        
        scoreAdded += Multiplier * acc;
        Score += scoreAdded;

        var scoreState = scoreStates[noteIndex];

        if (fromSustain) {
            scoreState.GainedBaseSustainPoints += amount;
            scoreState.GainedTotalSustainPoints += scoreAdded;

            int maxAmount = maxMultiplier * amount;
            
            MaxPossibleScoreSoFar += maxAmount;
            scoreState.RemainingTotalSustainPoints -= maxAmount;
        }
        else {
            Streak += addStreak;

            if (Streak > MaxStreak)
                MaxStreak = Streak;
            
            scoreState.GainedBasePoints = amount;
            scoreState.GainedTotalPoints = scoreAdded;
            scoreState.TimingAccuracy = timingAccuracy;
            
            MaxPossibleScoreSoFar += scoreState.RemainingTotalPoints;
            scoreState.RemainingTotalPoints = 0;
            
            maxPossibleStreak += scoreState.RemainingStreak;
            scoreState.RemainingStreak = 0;
        }

        UpdateFullComboState();
    }

    private void MissNote(int noteIndex, int multiplierChange) {
        if (!AddRemainingValueToMaxScoreSoFar(noteIndex))
            return;
        
        ChangeMultiplier(noteIndex, multiplierChange);
        UpdateFullComboState();
    }
    
    private void MissPairedNote(int noteIndex, int endNoteIndex, int multiplierChange) {
        if (!AddRemainingValueToMaxScoreSoFar(noteIndex) && !AddRemainingValueToMaxScoreSoFar(endNoteIndex))
            return;
        
        ChangeMultiplier(noteIndex, multiplierChange);
        UpdateFullComboState();
    }

    private void UpdateSustainedNoteValue(int noteIndex, float heldTime, float tickRate) {
        var scoreState = scoreStates[noteIndex];

        if (scoreState.AvailableBaseSustainPoints == 0)
            return;
        
        int valueChange = Mathf.Clamp(Mathf.FloorToInt(tickRate * heldTime), 1, scoreState.AvailableBaseSustainPoints)
            - scoreState.GainedBaseSustainPoints;
        
        if (valueChange > 0)
            AddScore(noteIndex, valueChange, 0, null, true);
    }

    private void ChangeMultiplier(int noteIndex, int amount) {
        if (amount == 0)
            return;
        
        Multiplier = Mathf.Clamp(Multiplier + amount, 1, maxMultiplier);
        pointsToNextMultiplier = GetPointsToNextMultiplier(Multiplier);

        if (amount > 0)
            return;
        
        Streak = 0;
        
        if (noteIndex >= 0)
            scoreStates[noteIndex].LostMultiplier = true;
    }

    private void UpdateFullComboState() {
        if (Streak < maxPossibleStreakSoFar && MaxStreak < maxPossibleStreak) {
            FullComboState = FullComboState.None;
            StarState = FullComboState.None;

            return;
        }

        if (Score < MaxPossibleScoreSoFar - sPlusThreshold) {
            FullComboState = FullComboState.FullCombo;
            StarState = FullComboState.FullCombo;
            StarColor = Color.green;

            return;
        }
        
        if (Score < MaxPossibleScoreSoFar) {
            FullComboState = FullComboState.FullCombo;
            StarState = FullComboState.PerfectFullCombo;
            StarColor = Color.green;
            
            return;
        }
        
        FullComboState = FullComboState.PerfectFullCombo;
        StarState = FullComboState.PerfectFullCombo;
        StarColor = Color.cyan;
    }

    private void InitScoreStates(PlayableTrackData trackData) {
        scoreStates = new NoteScoreState[trackData.NoteCount];

        int maxTapValue = GetPointValueForTap(0f);
        int maxBeatValue = GetPointValueForBeat(0f);
        int maxLiftoffValue = GetPointValueForLiftoff(0f);
        int maxBeatReleaseValue = GetPointValueForBeatRelease(0f);

        for (int i = 0; i < scoreStates.Length; i++) {
            var note = trackData.GetNote(i);
            int availableBasePoints = 0;
            int availableBaseSustainPoints = 0;
            int availableStreak = 0;

            switch (note.NoteType) {
                case NoteType.Match:
                    availableBasePoints = matchPointValue;
                    availableStreak = 1;
                    
                    break;
                case NoteType.DrumStart:
                    availableBasePoints = maxBeatValue;
                    availableStreak = 1;

                    if (note.length > 0f)
                        availableBaseSustainPoints = Mathf.Max(1, Mathf.FloorToInt(beatHoldTickRate * note.length));
                    
                    break;
                case NoteType.SpinRightStart:
                case NoteType.SpinLeftStart:
                    var spinnerSection = trackData.SpinnerSections[trackData.SpinnerSectionIndexForNoteIndex[i]];
                    
                    availableBasePoints = spinStartPointValue;
                    availableBaseSustainPoints = Mathf.Max(1, Mathf.FloorToInt(spinTickRate * (spinnerSection.endsAtTime - spinnerSection.startsAtTime)));
                    availableStreak = 1;
                    
                    break;
                case NoteType.HoldStart:
                    var freestyleSection = trackData.FreestyleSections[trackData.FreestyleSectionIndexForNoteIndex[i]];

                    availableBasePoints = maxTapValue;
                    availableBaseSustainPoints = Mathf.Max(1, Mathf.FloorToInt(holdTickRate * (freestyleSection.EndTime - freestyleSection.Time)));
                    availableStreak = 1;
                    
                    break;
                case NoteType.SectionContinuationOrEnd:
                    if (note.FreestyleEndType == FreestyleSection.EndType.Release) {
                        availableBasePoints = maxLiftoffValue;
                        availableStreak = 1;
                    }

                    break;
                case NoteType.Tap:
                    availableBasePoints = maxTapValue;
                    availableStreak = 1;
                    
                    break;
                case NoteType.DrumEnd:
                    if (note.DrumEndType == DrumSection.EndType.Release) {
                        availableBasePoints = maxBeatReleaseValue;
                        availableStreak = 1;
                    }

                    break;
                case NoteType.ScratchStart:
                    var scratchSection = trackData.ScratchSections[trackData.ScratchSectionIndexForNoteIndex[i]];

                    availableBaseSustainPoints = Mathf.Max(1, Mathf.FloorToInt(scratchTickRate * (scratchSection.endsAtTime - scratchSection.startsAtTime)));
                    availableStreak = 1;
                    
                    break;
            }

            int availableTotalPoints = maxMultiplier * availableBasePoints;
            int availableTotalSustainPoints = maxMultiplier * availableBaseSustainPoints;

            MaxPossibleScore += availableTotalPoints + availableTotalSustainPoints;
            maxPossibleStreak += availableStreak;
            scoreStates[i] = new NoteScoreState(
                availableBasePoints,
                availableBaseSustainPoints,
                availableTotalPoints,
                availableTotalSustainPoints,
                availableStreak);
        }
    }

    private bool AddRemainingValueToMaxScoreSoFar(int noteIndex) {
        var scoreState = scoreStates[noteIndex];
        int remainingPoints = scoreState.RemainingTotalPoints + scoreState.RemainingTotalSustainPoints;
        bool anyRemainingPoints = remainingPoints > 0;

        if (anyRemainingPoints) {
            MaxPossibleScoreSoFar += remainingPoints;
            scoreState.RemainingTotalPoints = 0;
            scoreState.RemainingTotalSustainPoints = 0;
        }

        int remainingStreak = scoreState.RemainingStreak;

        if (remainingStreak == 0)
            return anyRemainingPoints;
        
        maxPossibleStreakSoFar += remainingStreak;
        scoreState.RemainingStreak = 0;

        return true;
    }

    private string GetRank(int score, int maxScore) {
        if (score > maxScore - sPlusThreshold)
            return "S+";
        
        float scoreRatio = (float) score / maxScore;

        foreach (var pair in rankThresholds) {
            if (scoreRatio < pair.Threshold)
                continue;

            return pair.Rank;
        }

        return rankThresholds[rankThresholds.Length - 1].Rank;
    }

    private CustomTimingAccuracy GetTimingAccuracy(float timeOffset, TimingWindow[] timingWindows) {
        foreach (var window in timingWindows) {
            if (timeOffset < window.UpperBound)
                return window.TimingAccuracy;
        }

        return timingWindows[timingWindows.Length - 1].TimingAccuracy;
    }

    #endregion

    private HighScoreInfo GetHighScoreInfoForTrack(TrackInfoAssetReference trackInfoRef, TrackData.DifficultyType difficultyType) {
        var savedInfo = HighScoresContainer.GetHighScore(trackInfoRef, difficultyType, Id, string.Empty);

        return new HighScoreInfo(
            savedInfo.Score,
            savedInfo.Streak,
            savedInfo.SecondaryScore,
            GetRank(savedInfo.Score, savedInfo.MaxScore),
            savedInfo.Score == savedInfo.MaxScore ? FullComboState.PerfectFullCombo : savedInfo.Streak == savedInfo.MaxStreak ? FullComboState.FullCombo : FullComboState.None);
    }

    private class NoteScoreState {
        public int AvailableBasePoints { get; }
        public int AvailableBaseSustainPoints { get; }
        public int AvailableTotalPoints { get; }
        public int AvailableTotalSustainPoints { get; }
        public int AvailableStreak { get; }
        public int GainedBasePoints { get; set; }
        public int GainedBaseSustainPoints { get; set; }
        public int GainedTotalPoints { get; set; }
        public int GainedTotalSustainPoints { get; set; }
        public int RemainingTotalPoints { get; set; }
        public int RemainingTotalSustainPoints { get; set; }
        public int RemainingStreak { get; set; }
        public bool LostMultiplier { get; set; }
        public CustomTimingAccuracy TimingAccuracy { get; set; }

        public NoteScoreState(int availableBasePoints, int availableBaseSustainPoints, int availableTotalPoints, int availableTotalSustainPoints, int availableStreak) {
            AvailableBasePoints = availableBasePoints;
            AvailableBaseSustainPoints = availableBaseSustainPoints;
            AvailableTotalPoints = availableTotalPoints;
            AvailableTotalSustainPoints = availableTotalSustainPoints;
            AvailableStreak = availableStreak;
            GainedBasePoints = 0;
            GainedBaseSustainPoints = 0;
            GainedTotalPoints = 0;
            GainedTotalSustainPoints = 0;
            RemainingTotalPoints = availableTotalPoints;
            RemainingTotalSustainPoints = availableTotalSustainPoints;
            RemainingStreak = availableStreak;
            LostMultiplier = false;
            TimingAccuracy = null;
        }
    }
}