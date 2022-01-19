using UnityEngine;

namespace SRXDScoreMod; 

public class CustomTimingAccuracy {
    public string DisplayName { get; }
    
    public string StatsName { get; }
    
    public Color Color { get; }
    
    public NoteTimingAccuracy BaseAccuracy { get; }

    public CustomTimingAccuracy(string displayName, string statsName, Color color, NoteTimingAccuracy baseAccuracy) {
        DisplayName = displayName;
        StatsName = statsName;
        Color = color;
        BaseAccuracy = baseAccuracy;
    }
}