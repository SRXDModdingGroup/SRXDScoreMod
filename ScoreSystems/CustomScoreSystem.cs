using System;
using System.Collections.Generic;
using UnityEngine;

namespace SRXDScoreMod; 

internal class CustomScoreSystem : IScoreSystemInternal {
    #region IScoreSystemProperties

    public string Name { get; }
    
    public string Id { get; }
    
    public string Key { get; }

    public int Score => ScoreMod.GetModifiedScore(score);

    public int SecondaryScore { get; private set; }

    public int HighScore { get; private set; }

    public int HighSecondaryScore { get; private set; }

    public int MaxPossibleScore => ScoreMod.GetModifiedScore(maxPossibleScore);

    public int MaxPossibleScoreSoFar => ScoreMod.GetModifiedScore(maxPossibleScoreSoFar);

    public int Streak { get; private set; }
    
    public int MaxStreak { get; private set; }

    public int BestStreak { get; private set; }

    public bool IsHighScore { get; private set; }
    
    public int Multiplier { get; private set; }

    public FullComboState FullComboState { get; private set; }

    public FullComboState StarState { get; private set; }

    public Color StarColor { get; private set; }
    
    public string Rank { get; private set; }

    public bool ImplementsScorePrediction => true;

    public TimingWindow[] TimingWindowsForDisplay => tapTimingWindows;

    public bool ImplementsSecondaryScore => true;

    public string PostGameInfo1Name => "Performance";

    public string PostGameInfo2Name => "Accuracy";

    public string PostGameInfo3Name => "Early / Late";

    public List<ColoredGraphValue> PerformanceGraphValues { get; }
    
    public PieGraphValue[] PieGraphValues { get; }

    public string PostGameInfo1Value { get; private set; }
    
    public string PostGameInfo2Value { get; private set; }
    
    public string PostGameInfo3Value { get; private set; }

    #endregion

    #region ScoreSystemProfileValues

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
    private int greatSectionThreshold;
    private int sPlusThreshold;
    private float holdTickRate;
    private float beatHoldTickRate;
    private float spinTickRate;
    private float scratchTickRate;
    private int[] pointsPerMultiplier;
    private RankThreshold[] rankThresholds;
    private TimingWindow[] tapTimingWindows;
    private TimingWindow[] beatTimingWindows;
    private TimingWindow[] liftoffTimingWindows;
    private TimingWindow[] beatReleaseTimingWindows;

    #endregion

    private int startIndex;
    private int endIndex;
    private int score;
    private int maxPossibleScore;
    private int maxPossibleScoreSoFar;
    private int maxPossibleStreak;
    private int maxPossibleStreakSoFar;
    private int pointsToNextMultiplier;
    private int earlies;
    private int lates;
    private NoteScoreState[] scoreStates;
    private List<float> overbeatTimes;
    private PlayableTrackData trackData;

    internal CustomScoreSystem(ScoreSystemProfile profile) {
        Name = profile.Name;
        Id = profile.Id;
        Key = profile.Key;
        Multiplier = 1;
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
        greatSectionThreshold = profile.GreatSectionThreshold;
        sPlusThreshold = profile.SPlusThreshold;
        holdTickRate = profile.HoldTickRate;
        beatHoldTickRate = profile.BeatHoldTickRate;
        spinTickRate = profile.SpinTickRate;
        scratchTickRate = profile.ScratchTickRate;
        pointsPerMultiplier = profile.PointsPerMultiplier;
        rankThresholds = profile.RankThresholds;
        tapTimingWindows = profile.TapTimingWindows;
        beatTimingWindows = profile.BeatTimingWindows;
        liftoffTimingWindows = profile.LiftoffTimingWindows;
        beatReleaseTimingWindows = profile.BeatReleaseTimingWindows;
        overbeatTimes = new List<float>();
        PerformanceGraphValues = new List<ColoredGraphValue>();
        PieGraphValues = new PieGraphValue[7];
    }

    #region IScoreSystemFunctions

    public void Init(PlayState playState, int startIndex, int endIndex) {
        trackData = playState.trackData;
        this.startIndex = startIndex;
        this.endIndex = endIndex;
        ResetScore();

        var highScoreInfo = HighScoresContainer.GetHighScore(playState.TrackInfoRef, playState.trackData.Difficulty, this, ScoreMod.CurrentModifierSet);

        HighScore = highScoreInfo.Score;
        HighSecondaryScore = highScoreInfo.SecondaryScore;
        BestStreak = highScoreInfo.Streak;
    }

    public void ResetScore() {
        score = 0;
        SecondaryScore = 0;
        maxPossibleScore = 0;
        maxPossibleScoreSoFar = 0;
        Streak = 0;
        MaxStreak = 0;
        Multiplier = maxMultiplier;
        FullComboState = FullComboState.PerfectFullCombo;
        StarState = FullComboState.PerfectFullCombo;
        StarColor = Color.cyan;
        maxPossibleStreak = 0;
        maxPossibleStreakSoFar = 0;
        pointsToNextMultiplier = GetPointsToNextMultiplier(maxMultiplier);
        overbeatTimes.Clear();
        
        InitScoreStates();
    }

    public void Complete(PlayState playState) {
        Rank = GetRank(Score, maxPossibleScore);
        UpdateFullComboState();

        var modifierSet = ScoreMod.CurrentModifierSet;
        
        IsHighScore = HighScoresContainer.TrySetHighScore(playState.TrackInfoRef, playState.CurrentDifficulty, this, modifierSet, new SavedHighScoreInfo(
            string.Empty,
            Score,
            MaxStreak,
            maxPossibleScore,
            maxPossibleStreak,
            SecondaryScore,
            modifierSet));

        if (maxPossibleScore <= 0)
            PostGameInfo1Value = "100%";
        else
            PostGameInfo1Value = ((float) Score / MaxPossibleScore).ToString("P");

        GetValuesFromScoreStates();
        PostGameInfo2Value = GetAccuracy().ToString("P");
        PostGameInfo3Value = GetEarlyLateRatio();
    }

    public CustomTimingAccuracy GetTimingAccuracyForTap(float timeOffset) => GetTimingWindow(timeOffset, tapTimingWindows).TimingAccuracy;

    public CustomTimingAccuracy GetTimingAccuracyForBeat(float timeOffset) => GetTimingWindow(timeOffset, beatTimingWindows).TimingAccuracy;

    public CustomTimingAccuracy GetTimingAccuracyForLiftoff(float timeOffset) => GetTimingWindow(timeOffset, liftoffTimingWindows).TimingAccuracy;

    public CustomTimingAccuracy GetTimingAccuracyForBeatRelease(float timeOffset) => GetTimingWindow(timeOffset, beatReleaseTimingWindows).TimingAccuracy;

    public DomeHudFilledBar.BarState GetMultiplierBarState() {
        if (Multiplier == maxMultiplier) {
            return new DomeHudFilledBar.BarState {
                amount = maxMultiplier - 1,
                count = maxMultiplier - 1
            };
        }

        int multiplierSize = GetPointsToNextMultiplier(Multiplier);

        return new DomeHudFilledBar.BarState {
            amount = Multiplier - 1 + (float) (multiplierSize - pointsToNextMultiplier) / multiplierSize,
            count = maxMultiplier - 1
        };
    }

    public HighScoreInfo GetHighScoreInfoForTrack(MetadataHandle handle, TrackData.DifficultyType difficultyType)
        => GetHighScoreInfoForTrack(handle.TrackInfoRef, difficultyType);

    public HighScoreInfo GetHighScoreInfoForTrack(TrackInfoAssetReference trackInfoRef, TrackDataMetadata metadata)
        => GetHighScoreInfoForTrack(trackInfoRef, metadata.DifficultyType);
    
    #endregion

    #region NoteEvents

    internal void HitMatch(int noteIndex) => AddScore(noteIndex, matchPointValue, matchPointValue, 0, 0, 1, null, false);

    internal void HitTap(int noteIndex, float timeOffset)
        => AddTimedNoteScore(noteIndex, timeOffset, tapTimingWindows);
    
    internal void HitBeat(int noteIndex, float timeOffset)
        => AddTimedNoteScore(noteIndex, timeOffset, beatTimingWindows);

    internal void HitLiftoff(int noteIndex, float timeOffset)
        => AddTimedNoteScore(noteIndex, timeOffset, liftoffTimingWindows);

    internal void HitBeatRelease(int noteIndex, float timeOffset)
        => AddTimedNoteScore(noteIndex, timeOffset, beatReleaseTimingWindows);

    internal void HitSpin(int noteIndex) => AddScore(noteIndex, spinStartPointValue, spinStartPointValue, 0, 0, 1, null, false);

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

    internal void UpdateScratch(int noteIndex, float heldTime) {
        if (scoreStates[noteIndex].RemainingStreak > 0)
            AddScore(noteIndex, 0, 0, 0, 0, 1, null, false);
        
        UpdateSustainedNoteValue(noteIndex, heldTime, scratchTickRate);
    }

    internal void CompleteNote(int noteIndex) {
        if (CompleteScoreState(noteIndex))
            UpdateFullComboState();
    }

    #endregion

    #region InternalLogic

    private void AddScore(int noteIndex, int amount, int maxAmount, int secondaryAmount, int maxSecondaryAmount, int addStreak, CustomTimingAccuracy timingAccuracy, bool fromSustain) {
        var scoreState = scoreStates[noteIndex];

        if (fromSustain) {
            maxAmount = Math.Min(maxAmount, scoreState.RemainingBaseSustainPoints);
            amount = Math.Min(amount, maxAmount);
            scoreState.RemainingBaseSustainPoints -= maxAmount;
            scoreState.GainedBaseSustainPoints += amount;
        }
        else {
            maxAmount = Math.Min(maxAmount, scoreState.RemainingBasePoints);
            amount = Math.Min(amount, maxAmount);
            scoreState.RemainingBasePoints -= maxAmount;
            scoreState.GainedBasePoints += amount;
            scoreState.TimingAccuracy = timingAccuracy;
        }

        maxPossibleScoreSoFar += maxMultiplier * maxAmount;

        maxSecondaryAmount = Math.Min(maxSecondaryAmount, scoreState.RemainingSecondaryScore);
        secondaryAmount = Math.Min(secondaryAmount, maxSecondaryAmount);
        scoreState.RemainingSecondaryScore -= maxSecondaryAmount;
        scoreState.GainedSecondaryScore += secondaryAmount;
        SecondaryScore += secondaryAmount;
        
        addStreak = Math.Min(addStreak, scoreState.RemainingStreak);
        scoreState.RemainingStreak -= addStreak;
        scoreState.GainedStreak += addStreak;
        Streak += addStreak;
        maxPossibleStreakSoFar += addStreak;

        if (Streak > MaxStreak)
            MaxStreak = Streak;

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
        score += scoreAdded;

        if (fromSustain)
            scoreState.GainedTotalSustainPoints += scoreAdded;
        else
            scoreState.GainedTotalPoints += scoreAdded;

        if (scoreState.RemainingBasePoints == 0
            && scoreState.RemainingBaseSustainPoints == 0
            && scoreState.RemainingSecondaryScore == 0
            && scoreState.RemainingStreak == 0)
            scoreState.Completed = true;

        UpdateFullComboState();
    }

    private void AddTimedNoteScore(int noteIndex, float timeOffset, TimingWindow[] timingWindows) {
        var timingWindow = GetTimingWindow(timeOffset, timingWindows);
        
        AddScore(noteIndex, timingWindow.PointValue, GetMaxPointValue(timingWindows), timingWindow.SecondaryPointValue, GetMaxSecondaryPointValue(timingWindows), 1, timingWindow.TimingAccuracy, false);
    }

    private void MissNote(int noteIndex, int multiplierChange) {
        if (!CompleteScoreState(noteIndex))
            return;
        
        ChangeMultiplier(noteIndex, multiplierChange);
        UpdateFullComboState();
    }
    
    private void MissPairedNote(int noteIndex, int endNoteIndex, int multiplierChange) {
        bool anyRemaining = CompleteScoreState(noteIndex);

        if (CompleteScoreState(endNoteIndex))
            anyRemaining = true;
        
        if (!anyRemaining)
            return;
        
        ChangeMultiplier(noteIndex, multiplierChange);
        UpdateFullComboState();
    }

    private void UpdateSustainedNoteValue(int noteIndex, float heldTime, float tickRate) {
        var scoreState = scoreStates[noteIndex];

        if (scoreState.AvailableBaseSustainPoints == 0)
            return;
        
        int valueChange = Mathf.Clamp(Mathf.FloorToInt(tickRate * heldTime), 0, scoreState.AvailableBaseSustainPoints)
            - scoreState.GainedBaseSustainPoints;
        
        if (valueChange > 0)
            AddScore(noteIndex, valueChange, valueChange, 0, 0, 0, null, true);
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
        if (Streak > maxPossibleStreakSoFar || score > maxPossibleScoreSoFar) {
            FullComboState = FullComboState.PerfectFullCombo;
            StarState = FullComboState.PerfectFullCombo;
            StarColor = Color.red;
            
            return;
        }
        
        if (Streak < maxPossibleStreakSoFar && MaxStreak < maxPossibleStreak) {
            FullComboState = FullComboState.None;
            StarState = FullComboState.None;

            return;
        }

        if (score <= maxPossibleScoreSoFar - sPlusThreshold) {
            FullComboState = FullComboState.FullCombo;
            StarState = FullComboState.FullCombo;
            StarColor = Color.green;

            return;
        }
        
        if (score < maxPossibleScoreSoFar) {
            FullComboState = FullComboState.FullCombo;
            StarState = FullComboState.PerfectFullCombo;
            StarColor = Color.green;
            
            return;
        }
        
        FullComboState = FullComboState.PerfectFullCombo;
        StarState = FullComboState.PerfectFullCombo;
        StarColor = Color.cyan;
    }

    private void InitScoreStates() {
        if (trackData == null) {
            scoreStates = Array.Empty<NoteScoreState>();
            
            return;
        }
        
        scoreStates = new NoteScoreState[trackData.NoteCount];

        int maxTapValue = GetMaxPointValue(tapTimingWindows);
        int maxBeatValue = GetMaxPointValue(beatTimingWindows);
        int maxLiftoffValue = GetMaxPointValue(liftoffTimingWindows);
        int maxBeatReleaseValue = GetMaxPointValue(beatReleaseTimingWindows);

        for (int i = 0; i < scoreStates.Length; i++) {
            if (i < startIndex || i >= endIndex) {
                scoreStates[i] = new NoteScoreState(0, 0, 0, 0);
                
                continue;
            }
            
            var note = trackData.GetNote(i);
            int availablePoints = 0;
            int availableSustainPoints = 0;
            int availableSecondaryScore = 0;
            int availableStreak = 0;

            switch (note.NoteType) {
                case NoteType.Match: {
                    availablePoints = matchPointValue;
                    availableStreak = 1;

                    break;
                }
                case NoteType.DrumStart: {
                    availablePoints = maxBeatValue;
                    availableSecondaryScore = 1;
                    availableStreak = 1;

                    if (note.length > 0f)
                        availableSustainPoints = Mathf.FloorToInt(beatHoldTickRate * note.length);

                    break;
                }
                case NoteType.SpinRightStart:
                case NoteType.SpinLeftStart: {
                    int index = trackData.SpinnerSectionIndexForNoteIndex[i];

                    if (index < 0 || index >= trackData.SpinnerSections.Count)
                        break;
                    
                    var spinnerSection = trackData.SpinnerSections[index];

                    availablePoints = spinStartPointValue;
                    availableSustainPoints = Mathf.FloorToInt(spinTickRate * (spinnerSection.endsAtTime - spinnerSection.startsAtTime));
                    availableStreak = 1;

                    break;
                }
                case NoteType.HoldStart: {
                    int index = trackData.FreestyleSectionIndexForNoteIndex[i];

                    if (index < 0 || index >= trackData.FreestyleSections.Count)
                        break;

                    var freestyleSection = trackData.FreestyleSections[index];

                    availablePoints = maxTapValue;
                    availableSustainPoints = Mathf.FloorToInt(holdTickRate * (freestyleSection.EndTime - freestyleSection.Time));
                    availableSecondaryScore = 1;
                    availableStreak = 1;

                    break;
                }
                case NoteType.SectionContinuationOrEnd: {
                    if (note.FreestyleEndType != FreestyleSection.EndType.Release)
                        break;
                    
                    int index = trackData.FreestyleSectionIndexForNoteIndex[i];

                    if (index < 0 || index >= trackData.FreestyleSections.Count)
                        break;

                    var freestyleSection = trackData.FreestyleSections[index];

                    if (freestyleSection.endNoteIndex != i)
                        break;

                    availablePoints = maxLiftoffValue;
                    availableSecondaryScore = 1;
                    availableStreak = 1;

                    break;
                }
                case NoteType.Tap: {
                    availablePoints = maxTapValue;
                    availableSecondaryScore = 1;
                    availableStreak = 1;

                    break;
                }
                case NoteType.DrumEnd: {
                    if (note.DrumEndType != DrumSection.EndType.Release)
                        break;

                    int index = trackData.DrumIndexForNoteIndex[i];

                    if (index < 0 || index >= trackData.NoteCount || trackData.GetNote(index).endNoteIndex != i)
                        break;

                    availablePoints = maxBeatReleaseValue;
                    availableSecondaryScore = 1;
                    availableStreak = 1;

                    break;
                }
                case NoteType.ScratchStart: {
                    int index = trackData.ScratchSectionIndexForNoteIndex[i];

                    if (index < 0 || index >= trackData.ScratchSections.Count)
                        break;

                    var scratchSection = trackData.ScratchSections[index];

                    if (scratchSection.IsEmpty)
                        break;

                    availableSustainPoints = Mathf.FloorToInt(scratchTickRate * (scratchSection.endsAtTime - scratchSection.startsAtTime));
                    availableStreak = 1;

                    break;
                }
            }

            maxPossibleScore += maxMultiplier * (availablePoints + availableSustainPoints);
            maxPossibleStreak += availableStreak;
            scoreStates[i] = new NoteScoreState(
                availablePoints,
                availableSustainPoints,
                availableSecondaryScore,
                availableStreak);
        }
    }

    private void GetValuesFromScoreStates() {
        earlies = 0;
        lates = 0;
        PerformanceGraphValues.Clear();
        overbeatTimes.Sort();

        for (int i = 0; i < PieGraphValues.Length; i++)
            PieGraphValues[i] = new PieGraphValue(0, 0, 0);

        int firstNoteIndex;

        for (firstNoteIndex = 0; firstNoteIndex < scoreStates.Length; firstNoteIndex++) {
            var scoreState = scoreStates[firstNoteIndex];
            
            if (scoreState.AvailableBasePoints > 0 || scoreState.AvailableBaseSustainPoints > 0)
                break;
        }
        
        float firstNoteTime = trackData.GetNote(firstNoteIndex).time;
        float sectionLength = (trackData.GameplayEndTime - firstNoteTime) / 60f;
        float sectionEndTime = firstNoteTime + sectionLength;
        int counter = 1;
        int totalValueForSection = 0;
        int maxValueForSection = 0;
        bool currentSectionLostMultiplier = false;

        for (int i = firstNoteIndex; i < trackData.NoteCount; i++) {
            var scoreState = scoreStates[i];
            int availableBasePoints = scoreState.AvailableBasePoints + scoreState.AvailableBaseSustainPoints;
            
            if (availableBasePoints == 0)
                continue;
            
            var note = trackData.GetNote(i);
            float time = note.time;
            
            if (time > sectionEndTime) {
                while (sectionEndTime < time) {
                    counter++;
                    sectionEndTime = firstNoteTime + counter * sectionLength;
                }
                
                PopSection(firstNoteTime + (counter - 1) * sectionLength);
            }

            totalValueForSection += scoreState.GainedTotalPoints + scoreState.GainedTotalSustainPoints;
            maxValueForSection += maxMultiplier * (scoreState.AvailableBasePoints + scoreState.AvailableBaseSustainPoints);

            if (scoreState.LostMultiplier)
                currentSectionLostMultiplier = true;
            
            var timingAccuracy = scoreState.TimingAccuracy;

            if (timingAccuracy != null) {
                var baseTimingAccuracy = timingAccuracy.BaseAccuracy;

                if (baseTimingAccuracy == NoteTimingAccuracy.Early)
                    earlies++;
                else if (baseTimingAccuracy == NoteTimingAccuracy.Late)
                    lates++;
            }

            int gainedBasePoints = scoreState.GainedBasePoints + scoreState.GainedBaseSustainPoints;
            int pieIndex = note.NoteType switch {
                NoteType.Match => 0,
                NoteType.HoldStart => 1,
                NoteType.Tap => 2,
                NoteType.DrumStart => 3,
                NoteType.SectionContinuationOrEnd => 4,
                NoteType.DrumEnd => 4,
                NoteType.SpinRightStart => 5,
                NoteType.SpinLeftStart => 5,
                NoteType.ScratchStart => 6,
                _ => 0
            };

            var pieValue = PieGraphValues[pieIndex];

            if (gainedBasePoints == 0)
                pieValue.Missed += availableBasePoints;
            else {
                pieValue.Perfect += gainedBasePoints;
                pieValue.Good += availableBasePoints - gainedBasePoints;
            }
        }
        
        PopSection(trackData.GameplayEndTime);

        void PopSection(float endTime) {
            if (maxValueForSection == 0)
                return;
            
            while (overbeatTimes.Count > 0 && overbeatTimes[0] < endTime) {
                currentSectionLostMultiplier = true;
                overbeatTimes.RemoveAt(0);
            }
                
            float value = Mathf.Clamp((float) totalValueForSection / maxValueForSection, 0.05f, 1f);
            Color color;

            if (currentSectionLostMultiplier || totalValueForSection == 0)
                color = Color.red;
            else if (totalValueForSection == maxValueForSection)
                color = Color.cyan;
            else if (totalValueForSection > maxValueForSection - greatSectionThreshold)
                color = new Color(0f, 0.9f, 0f);
            else
                color = Color.yellow;
                
            PerformanceGraphValues.Add(new ColoredGraphValue(value, color));
            totalValueForSection = 0;
            maxValueForSection = 0;
            currentSectionLostMultiplier = false;
        }
    }

    private bool CompleteScoreState(int noteIndex) {
        var scoreState = scoreStates[noteIndex];
        
        if (scoreState.Completed)
            return false;

        maxPossibleScoreSoFar += maxMultiplier * (scoreState.RemainingBasePoints + scoreState.RemainingBaseSustainPoints);
        maxPossibleStreakSoFar += scoreState.RemainingStreak;
        scoreState.RemainingBasePoints = 0;
        scoreState.RemainingBaseSustainPoints = 0;
        scoreState.RemainingSecondaryScore = 0;
        scoreState.RemainingStreak = 0;
        scoreState.Completed = true;

        return true;
    }

    private int GetPointsToNextMultiplier(int currentMultiplier) => pointsPerMultiplier[Mathf.Min(currentMultiplier, pointsPerMultiplier.Length) - 1];

    private float GetAccuracy() {
        int totalGained = 0;
        int totalPossible = 0;

        foreach (var scoreState in scoreStates) {
            int gained = scoreState.GainedBasePoints;
            
            if (gained == 0)
                continue;

            totalGained += gained;
            totalPossible += scoreState.AvailableBasePoints;
        }

        if (totalPossible == 0f)
            return 1f;

        return (float) totalGained / totalPossible;
    }

    private string GetRank(int score, int maxScore) {
        if (score == 0)
            return "-";
        
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

    private string GetEarlyLateRatio() {
        if (earlies == 0 && lates == 0)
            return "0 : 0";

        int sum = earlies + lates;
        int early = Mathf.RoundToInt((float) earlies / sum * 100f);
        int late = Mathf.RoundToInt((float) lates / sum * 100f);

        if (early + late > 100)
            late = 100 - early;

        if (early == 0 && earlies > 0) {
            early++;
            late--;
        }
            
        if (late == 0 && lates > 0) {
            late++;
            early--;
        }

        return $"{early} : {late}";
    }

    private HighScoreInfo GetHighScoreInfoForTrack(TrackInfoAssetReference trackInfoRef, TrackData.DifficultyType difficultyType) {
        var savedInfo = HighScoresContainer.GetHighScore(trackInfoRef, difficultyType, this, ScoreMod.CurrentModifierSet);

        return new HighScoreInfo(
            savedInfo.Score,
            savedInfo.Streak,
            savedInfo.SecondaryScore,
            GetRank(savedInfo.Score, savedInfo.MaxScore),
            GetFullComboState(savedInfo.Score, savedInfo.MaxScore, savedInfo.Streak, savedInfo.MaxStreak));
    }

    private static int GetMaxPointValue(TimingWindow[] timingWindows) {
        int max = 0;

        foreach (var window in timingWindows) {
            int pointValue = window.PointValue;

            if (pointValue > max)
                max = pointValue;
        }

        return max;
    }

    private static int GetMaxSecondaryPointValue(TimingWindow[] timingWindows) {
        int max = 0;

        foreach (var window in timingWindows) {
            int secondaryPointValue = window.SecondaryPointValue;

            if (secondaryPointValue > max)
                max = secondaryPointValue;
        }

        return max;
    }

    private static FullComboState GetFullComboState(int score, int maxScore, int streak, int maxStreak) {
        if (maxScore == 0 || maxStreak == 0)
            return FullComboState.None;

        if (score == maxScore)
            return FullComboState.PerfectFullCombo;

        if (streak == maxStreak)
            return FullComboState.FullCombo;

        return FullComboState.None;
    }

    private static TimingWindow GetTimingWindow(float timeOffset, TimingWindow[] timingWindows) {
        foreach (var window in timingWindows) {
            if (timeOffset < window.UpperBound)
                return window;
        }

        return timingWindows[timingWindows.Length - 1];
    }

    #endregion

    private class NoteScoreState {
        public int AvailableBasePoints { get; }
        public int AvailableBaseSustainPoints { get; }
        public int AvailableSecondaryScore { get; }
        public int AvailableStreak { get; }
        public int GainedBasePoints { get; set; }
        public int GainedBaseSustainPoints { get; set; }
        public int GainedTotalPoints { get; set; }
        public int GainedTotalSustainPoints { get; set; }
        public int GainedSecondaryScore { get; set; }
        public int GainedStreak { get; set; }
        public int RemainingBasePoints { get; set; }
        public int RemainingBaseSustainPoints { get; set; }
        public int RemainingSecondaryScore { get; set; }
        public int RemainingStreak { get; set; }
        public bool Completed { get; set; }
        public bool LostMultiplier { get; set; }
        public CustomTimingAccuracy TimingAccuracy { get; set; }

        public NoteScoreState(int availableBasePoints, int availableBaseSustainPoints, int availableSecondaryScore, int availableStreak) {
            Completed = false;
            AvailableBasePoints = availableBasePoints;
            AvailableBaseSustainPoints = availableBaseSustainPoints;
            AvailableSecondaryScore = availableSecondaryScore;
            AvailableStreak = availableStreak;
            GainedBasePoints = 0;
            GainedBaseSustainPoints = 0;
            GainedTotalPoints = 0;
            GainedTotalSustainPoints = 0;
            GainedStreak = 0;
            GainedSecondaryScore = 0;
            RemainingBasePoints = availableBasePoints;
            RemainingBaseSustainPoints = availableBaseSustainPoints;
            RemainingSecondaryScore = availableSecondaryScore;
            RemainingStreak = availableStreak;
            LostMultiplier = false;
            TimingAccuracy = null;
        }
    }
}