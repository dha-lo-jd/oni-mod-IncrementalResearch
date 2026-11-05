using HarmonyLib;
using IncrementalResearch.Core;

namespace IncrementalResearch.Patches;

// New and loaded Duplicants get the colony's attribute bonuses.
[HarmonyPatch(typeof(MinionIdentity), "OnSpawn")]
public static class MinionIdentity_OnSpawn_Patch
{
    public static void Postfix(MinionIdentity __instance)
    {
        RewardEffects.ApplyAttributes(__instance);
    }
}
