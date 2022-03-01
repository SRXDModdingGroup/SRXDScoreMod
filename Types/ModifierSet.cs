using System;
using UnityEngine;

namespace SRXDScoreMod; 

/// <summary>
/// A collection of modifiers
/// </summary>
public class ModifierSet {
    private const int MAX_MODIFIERS = 32;
    
    internal string Id { get; }

    internal event Action ModifierChanged;

    private Modifier[] modifiers;
    private Modifier[] modifiersByIndex;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="id">The unique identifier for the set. This value should not be changed after the modifier set is first introduced</param>
    /// <param name="modifiers">The modifiers used by the set</param>
    public ModifierSet(string id, params Modifier[] modifiers) {
        Id = id;
        this.modifiers = modifiers;
        modifiersByIndex = new Modifier[MAX_MODIFIERS];

        foreach (var modifier in modifiers)
            modifiersByIndex[modifier.Index] = modifier;
    }

    internal void DisableAll() {
        foreach (var modifier in modifiers)
            modifier.EnabledInternal.Value = false;
        
        ModifierChanged?.Invoke();
    }

    internal bool GetAnyEnabled() {
        foreach (var modifier in modifiers) {
            if (modifier.Enabled.Value)
                return true;
        }

        return false;
    }

    internal bool GetAnyBlocksSubmission() {
        foreach (var modifier in modifiers) {
            if (modifier.BlocksSubmission)
                return true;
        }

        return false;
    }

    internal bool GetBlocksSubmissionGivenActiveFlags(uint flags) {
        for (int i = 0, j = 1; i < MAX_MODIFIERS; i++, j <<= 1) {
            if ((flags & j) == 0u)
                continue;
            
            var modifier = modifiersByIndex[i];

            if (modifier != null && modifier.BlocksSubmission)
                return true;
        }

        return false;
    }

    internal int GetOverallMultiplier() {
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
