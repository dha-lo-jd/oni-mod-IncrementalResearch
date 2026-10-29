using TUNING;
using UnityEngine;

namespace IncrementalResearch;

public sealed class RecordArchiveConfig : IBuildingConfig
{
    public const string ID = "IR_RecordArchive";

    public override BuildingDef CreateBuildingDef()
    {
        // Placeholder animation: reuses the Super Computer's.
        var def = BuildingTemplates.CreateBuildingDef(
            ID,
            3,
            3,
            "research_center2_kanim",
            30,
            30f,
            BUILDINGS.CONSTRUCTION_MASS_KG.TIER3,
            MATERIALS.ALL_METALS,
            1600f,
            BuildLocationRule.OnFloor,
            DECOR.NONE,
            NOISE_POLLUTION.NONE);
        def.RequiresPowerInput = true;
        def.EnergyConsumptionWhenActive = 120f;
        def.SelfHeatKilowattsWhenActive = 1f;
        def.ViewMode = OverlayModes.Power.ID;
        def.AudioCategory = "Metal";
        def.AudioSize = "large";
        return def;
    }

    public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
    {
        // One per world.
        go.GetComponent<KPrefabID>().AddTag(GameTags.UniquePerWorld);
    }

    public override void DoPostConfigureComplete(GameObject go)
    {
        go.AddOrGet<BuildingEnabledButton>();
        go.AddOrGet<RecordArchive>();
    }
}
