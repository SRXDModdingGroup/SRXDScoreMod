using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace SRXDScoreMod; 

public class ModifierSet {
    /// <summary>
    /// The name of the modifier set
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// The unique identifier for the set. This value should not be changed after the modifier is first introduced
    /// </summary>
    public string Id { get; }
    /// <summary>
    /// The set of available modifiers
    /// </summary>
    public ReadOnlyDictionary<string, Modifier> Modifiers { get; }

    internal Modifier[] ModifiersArray { get; }
    
    private Modifier[] modifiersByIndex;

    public ModifierSet(string name, string id, params Modifier[] modifiersArray) {
        Name = name;
        Id = id;
        this.ModifiersArray = modifiersArray;

        var modifiersDict = new Dictionary<string, Modifier>();

        modifiersByIndex = new Modifier[32];

        foreach (var modifier in modifiersArray) {
            modifiersDict.Add(modifier.Name, modifier);
            modifiersByIndex[modifier.Index] = modifier;
        }

        Modifiers = new ReadOnlyDictionary<string, Modifier>(modifiersDict);
    }

    internal bool ToggleModifier(int index) {
        if (index >= ModifiersArray.Length)
            return false;

        var enabled = ModifiersArray[index].EnabledInternal;

        enabled.Value = !enabled.Value;

        return true;
    }

    internal bool GetAnyEnabled() {
        foreach (var pair in Modifiers) {
            var modifier = pair.Value;

            if (modifier.Enabled.Value)
                return true;
        }

        return false;
    }

    internal bool GetAnyBlocksSubmission() {
        foreach (var pair in Modifiers) {
            var modifier = pair.Value;

            if (modifier.BlocksSubmission)
                return true;
        }

        return false;
    }

    internal bool GetBlocksSubmissionGivenActiveFlags(uint flags) {
        for (int i = 0, j = 1; i < 31; i++, j <<= 1) {
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

        foreach (var modifier in modifiersByIndex) {
            if (modifier == null || !modifier.Enabled.Value)
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
        
        for (int i = 0, j = 1; i < 31; i++, j <<= 1) {
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

        for (int i = 0; i < 31; i++) {
            var modifier = modifiersByIndex[i];

            if (modifier != null && modifier.Enabled.Value)
                bits |= 1u << i;
        }

        return bits;
    }
}
