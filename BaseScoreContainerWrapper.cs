using System;
using UnityEngine;

namespace SRXDScoreMod; 

internal class BaseScoreContainerWrapper : IScoreContainer {
    public int Score => scoreState.FinalisedScore > 0 ? scoreState.FinalisedScore : scoreState.totalNoteScore;

    public int HighSecondaryScore => 0;
        
    public int MaxScore => 0;
        
    public int MaxScoreSoFar => 0;

    public int HighScore => 0;

    public int SecondaryScore => 0;
        
    public int Multiplier => scoreState.Multiplier;
        
    public int Streak => scoreState.combo;

    public int BestStreak => 0;

    public FullComboState StarState => scoreState.fullComboState;
        
    public Color StarColor => Color.cyan;

    public FullComboState FullComboState => scoreState.fullComboState;
        
    public bool IsHighScore { get; private set; }
        
    public string Rank { get; private set; }

    public string PostGameInfo1Value => scoreState.AccuracyBonus > 0 ? scoreState.AccuracyBonus.ToString() : string.Empty;

    public string PostGameInfo2Value => string.Empty;

    public string PostGameInfo3Value => scoreState.fullComboState == FullComboState.PerfectFullCombo ? scoreState.PfcBonus.ToString() : string.Empty;

    private PlayState.ScoreState scoreState;

    public void SetScoreState(PlayState.ScoreState scoreState) => this.scoreState = scoreState;

    public void SetPostGameInfo(bool isHighScore, string rank) {
        IsHighScore = isHighScore;
        Rank = rank;
    }
}