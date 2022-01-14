using System.Reflection.Emit;
using HarmonyLib;

namespace SRXDScoreMod; 

public static class CodeInstructionExtensions {
    public static bool LoadsLocalAtIndex(this CodeInstruction instruction, int index)
        => instruction.opcode == OpCodes.Ldloc_S && ((LocalBuilder) instruction.operand).LocalIndex == index;
    
    public static bool LoadsLocalAddressAtIndex(this CodeInstruction instruction, int index)
        => instruction.opcode == OpCodes.Ldloca_S && ((LocalBuilder) instruction.operand).LocalIndex == index;
    
    
    public static bool StoresLocalAtIndex(this CodeInstruction instruction, int index)
        => instruction.opcode == OpCodes.Stloc_S && ((LocalBuilder) instruction.operand).LocalIndex == index;
}