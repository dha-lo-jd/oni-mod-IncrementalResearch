using HarmonyLib;
using IncrementalResearch.Core;

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
