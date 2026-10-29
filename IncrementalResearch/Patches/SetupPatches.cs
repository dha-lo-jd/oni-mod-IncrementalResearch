using Database;
using HarmonyLib;
using IncrementalResearch.Core;

namespace IncrementalResearch.Patches;

[HarmonyPatch(typeof(SaveGame), "OnPrefabInit")]
public static class SaveGame_OnPrefabInit_Patch
{
    public static void Postfix(SaveGame __instance)
    {
        __instance.gameObject.AddOrGet<KnowledgeState>();
    }
}

[HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
public static class GeneratedBuildings_LoadGeneratedBuildings_Patch
{
    private static bool added;

    public static void Prefix()
    {
        // LoadGeneratedBuildings can run more than once; registering twice crashes the Codex.
        if (added)
        {
            return;
        }

        added = true;
        ModUtil.AddBuildingToPlanScreen("Equipment", RecordArchiveConfig.ID, "research");
    }
}

// The archive is unlocked once the whole research tree is complete.
[HarmonyPatch(typeof(TechItems), nameof(TechItems.IsTechItemComplete))]
public static class TechItems_IsTechItemComplete_Patch
{
    public static void Postfix(string id, ref bool __result)
    {
        if (id != RecordArchiveConfig.ID)
        {
            return;
        }

        __result = true;
        foreach (Tech tech in Db.Get().Techs.resources)
        {
            if (!tech.IsComplete())
            {
                __result = false;
                return;
            }
        }
    }
}
