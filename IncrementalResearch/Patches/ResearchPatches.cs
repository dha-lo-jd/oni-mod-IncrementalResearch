using HarmonyLib;
using IncrementalResearch.Core;
using UnityEngine;

namespace IncrementalResearch.Patches;

// Records every research point so that the colony's research output can be turned into knowledge.
[HarmonyPatch(typeof(Research), nameof(Research.AddResearchPoints))]
public static class Research_AddResearchPoints_Patch
{
    public static bool Prefix(string researchTypeID, float points)
    {
        var state = KnowledgeState.Instance;
        if (state == null)
        {
            return true;
        }

        state.RecordResearchPoints(researchTypeID, points);
        // In idle mode there is no active research to receive the points.
        return !state.IdleMode;
    }
}

// Keeps the research buildings working after the research tree is complete.
[HarmonyPatch(typeof(ResearchCenter), "UpdateWorkingState")]
public static class ResearchCenter_UpdateWorkingState_Patch
{
    public static bool Prefix(ResearchCenter __instance)
    {
        var state = KnowledgeState.Instance;
        if (state == null || !state.IdleMode)
        {
            return true;
        }

        var selectable = __instance.GetComponent<KSelectable>();
        selectable.RemoveStatusItem(Db.Get().BuildingStatusItems.NoResearchSelected);
        selectable.RemoveStatusItem(Db.Get().BuildingStatusItems.NoApplicableResearchSelected);
        __instance.GetComponent<Operational>().SetFlag(ResearchCenter.ResearchSelectedFlag, true);
        return false;
    }
}

// The Materials Study Terminal (NuclearResearchCenter) has its own state machine that needs a selected research.
// In idle mode it counts as having an applicable research.
[HarmonyPatch(typeof(NuclearResearchCenter.States), "IsResearchSelected")]
public static class NuclearResearchCenter_IsResearchSelected_Patch
{
    public static void Postfix(ref bool __result)
    {
        __result |= KnowledgeState.Instance != null && KnowledgeState.Instance.IdleMode;
    }
}

[HarmonyPatch(typeof(NuclearResearchCenter.States), "IsResearchApplicable")]
public static class NuclearResearchCenter_IsResearchApplicable_Patch
{
    public static void Postfix(ref bool __result)
    {
        __result |= KnowledgeState.Instance != null && KnowledgeState.Instance.IdleMode;
    }
}

// The work ends as soon as there is no active research; in idle mode it only ends when the particles run out.
[HarmonyPatch(typeof(NuclearResearchCenterWorkable), "OnWorkTick")]
public static class NuclearResearchCenterWorkable_OnWorkTick_Patch
{
    public static void Postfix(NuclearResearchCenterWorkable __instance, ref bool __result)
    {
        if (KnowledgeState.Instance == null || !KnowledgeState.Instance.IdleMode)
        {
            return;
        }

        var storage = Traverse.Create(__instance).Field("radiationStorage").GetValue<HighEnergyParticleStorage>();
        __result = storage == null || storage.IsEmpty();
    }
}
