using UnityEngine;

namespace SRXDScoreMod; 

internal readonly struct ColoredGraphValue {
    public float Value { get; }

    public Color Color { get; }

    public ColoredGraphValue(float value, Color color) {
        Value = value;
        Color = color;
    }
}