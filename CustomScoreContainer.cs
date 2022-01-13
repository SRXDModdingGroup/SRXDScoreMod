using UnityEngine;

namespace SRXDScoreMod; 

internal abstract class CustomScoreContainer : IScoreContainer {
    public int Score { get; internal set; }
    
    public int SecondaryScore { get; internal set; }
    
    public int HighScore { get; internal set; }
    
    public int HighSecondaryScore { get; internal set; }
    
    public int MaxScore { get; internal set; }
    
    public int MaxScoreSoFar { get; internal set; }
    
    public int Streak { get; internal set; }
    
    public int BestStreak { get; internal set; }
    
    public bool IsHighScore { get; internal set; }
    
    public int Multiplier { get; internal set; }
    
    public FullComboState FullComboState { get; internal set; }
    
    public FullComboState StarState { get; internal set; }
    
    public Color StarColor { get; internal set; }
    
    public string Rank { get; internal set; }
    
    public string PostGameInfo1Value { get; internal set; }
    
    public string PostGameInfo2Value { get; internal set; }
    
    public string PostGameInfo3Value { get; internal set; }
}