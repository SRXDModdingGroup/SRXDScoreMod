using System;
using UnityEngine;

namespace SRXDScoreMod; 

/// <summary>
/// A collection of modifiers
/// </summary>
public class ScoreModifierSet {
    private const int MAX_MODIFIERS = 32;
    
    internal string Id { get; }

    internal event Action ModifierChanged;

    private bool invokeModifierChanged = true;
    private ScoreModifier[] modifiers;
    private ScoreModifier[] modifiersByIndex;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="id">The unique identifier for the set. This value should not be changed after the modifier set is first introduced</param>
    /// <param name="modifiers">The modifiers used by the set</param>
    public ScoreModifierSet(string id, params ScoreModifier[] modifiers) {
        Id = id;
        this.modifiers = modifiers;
        modifiersByIndex = new ScoreModifier[MAX_MODIFIERS];

        foreach (var modifier in modifiers) {
            if (modifiersByIndex[modifier.Index] != null)
                throw new ArgumentException("Two score modifiers can not have the same index");
            
            modifier.EnabledInternal.Bind(_ => {
                if (invokeModifierChanged)
                    ModifierChanged?.Invoke();
            });
            modifiersByIndex[modifier.Index] = modifier;
        }
    }

    /// <summary>
    /// Returns true if any modifier in the set is enabled
    /// </summary>
    /// <returns>True if any modifier in the set is enabled</returns>
    public bool GetAnyEnabled() {
        foreach (var modifier in modifiers) {
            if (modifier.Enabled.Value)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if any enabled modifier in the set blocks score submission
    /// </summary>
    /// <returns>True if any enabled modifier in the set blocks score submission</returns>
    public bool GetAnyBlocksSubmission() {
        foreach (var modifier in modifiers) {
            if (modifier.Enabled.Value && modifier.BlocksSubmission)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the overall multiplier percentage as an integer
    /// </summary>
    /// <returns>The overall multiplier percentage as an integer</returns>
    public int GetOverallMultiplier() {
        int multiplier = 100;
        int negativeOnly = 100;

        foreach (var modifier in modifiers) {
            if (!modifier.Enabled.Value)
                continue;

            int value = modifier.Value;
            
            multiplier += value;

            if (value < 0)
                negativeOnly += value;
        }

        if (negativeOnly < 100)
            return Mathf.Max(0, negativeOnly);

        return multiplier;
    }

    internal void DisableAll() {
        invokeModifierChanged = false;
        
        foreach (var modifier in modifiers)
            modifier.EnabledInternal.Value = false;

        invokeModifierChanged = true;
        ModifierChanged?.Invoke();
    }

    internal bool GetBlocksSubmissionGivenActiveFlags(uint flags) {
        for (int i = 0, j = 1; i < MAX_MODIFIERS; i++, j <<= 1) {
            if ((flags & j) == 0u)
                continue;
            
            var modifier = modifiersByIndex[i];

            if (modifier != null && modifier.Enabled.Value && modifier.BlocksSubmission)
                return true;
        }

        return false;
    }

    internal int GetMultiplierGivenActiveFlags(uint flags) {
        int multiplier = 100;
        int negativeOnly = 100;
        
        for (int i = 0, j = 1; i < MAX_MODIFIERS; i++, j <<= 1) {
            if ((flags & j) == 0u)
                continue;
            
            var modifier = modifiersByIndex[i];

            if (modifier == null)
                continue;

            int value = modifier.Value;
            
            multiplier += value;
            
            if (value < 0)
                negativeOnly += value;
        }
        
        if (negativeOnly < 100)
            return Mathf.Max(0, negativeOnly);

        return multiplier;
    }
    
    internal uint GetActiveModifierFlags() {
        uint bits = 0u;

        for (int i = 0; i < MAX_MODIFIERS; i++) {
            var modifier = modifiersByIndex[i];

            if (modifier != null && modifier.Enabled.Value)
                bits |= 1u << i;
        }

        return bits;
    }
}
