using HarmonyLib;
using IncrementalResearch.Core;

namespace IncrementalResearch.Patches;

// Our buildings are locked through TechItems.IsTechItemComplete but have no TechItem of their own. Since U60 the build
// menu asks for the locked button's tooltip as soon as it creates the button (PlanBuildingToggle.Refresh), and the vanilla
// text for a research lock reads the TechItem's parent tech: that threw a NullReferenceException inside PlanScreen.OnSpawn
// and broke the whole build menu. Give our buildings their own text, and any other TechItem-less building an empty one.
[HarmonyPatch(typeof(PlanScreen), nameof(PlanScreen.GetTooltipForRequirementsState))]
public static class PlanScreen_GetTooltipForRequirementsState_Patch
{
    public static bool Prefix(BuildingDef def, PlanScreen.RequirementsState state, ref string __result)
    {
        if (state != PlanScreen.RequirementsState.Tech || def == null || Db.Get().TechItems.TryGet(def.PrefabID) != null)
        {
            return true;
        }

        __result = def.PrefabID == RecordArchiveConfig.ID ? STRINGS.UI.TIERS.LOCKED_ARCHIVE
            : IsOurs(def.PrefabID) ? STRINGS.UI.TIERS.LOCKED_TIER
            : "";
        return false;
    }

    private static bool IsOurs(string id) =>
        id == ComputeRoomConfig.ID || id == VacuumLabConfig.ID || id == AcceleratorConfig.ID
        || id == ConfinementConfig.ID || id == ObservationNetConfig.ID || id == ExtractionFurnaceConfig.ID;
}
