using Database;
using HarmonyLib;
using IncrementalResearch.Core;

namespace IncrementalResearch.Patches;

// The research status items name the active tech; in idle mode there is none, so they name the Insight instead.
[HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
public static class Db_Initialize_StatusItems_Patch
{
    public static void Postfix()
    {
        var duplicantItem = Db.Get().DuplicantStatusItems.Researching;
        var originalDuplicant = duplicantItem.resolveStringCallback;
        duplicantItem.resolveStringCallback = (str, data) =>
        {
            var result = originalDuplicant(str, data);
            return IsIdle() ? result.Replace("{Tech}", STRINGS.UI.TAB.OPEN) : result;
        };

        var buildingItem = Db.Get().BuildingStatusItems.ComplexFabricatorResearching;
        var originalBuilding = buildingItem.resolveStringCallback;
        buildingItem.resolveStringCallback = (str, data) =>
        {
            // The original falls through to a GameObject cast of the research center when no tech is active.
            return IsIdle() && data is IResearchCenter ? str.Replace("{Item}", STRINGS.UI.TAB.OPEN) : originalBuilding(str, data);
        };
    }

    private static bool IsIdle()
    {
        return KnowledgeState.Instance != null && KnowledgeState.Instance.IdleMode;
    }
}
