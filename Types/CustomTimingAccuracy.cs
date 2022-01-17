using UnityEngine;

namespace SRXDScoreMod; 

public class CustomTimingAccuracy : IHashable {
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

    public int GetStableHash() => HashUtility.Combine(
        DisplayName,
        StatsName,
        Color,
        (int) BaseAccuracy);
}