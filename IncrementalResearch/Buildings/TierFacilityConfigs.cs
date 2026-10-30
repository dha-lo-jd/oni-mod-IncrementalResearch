using TUNING;
using UnityEngine;

namespace IncrementalResearch;

public sealed class ComputeRoomConfig : IBuildingConfig
{
    public const string ID = "IR_ComputeRoom";

    public override BuildingDef CreateBuildingDef()
    {
        // Placeholder animation: reuses the Super Computer's.
        var def = BuildingTemplates.CreateBuildingDef(
            ID,
            3,
            3,
            "research_center2_kanim",
            30,
            60f,
            new[] { 800f },
            new[] { SimHashes.Steel.ToString() },
            1600f,
            BuildLocationRule.OnFloor,
            DECOR.NONE,
            NOISE_POLLUTION.NONE);
        def.RequiresPowerInput = true;
        def.EnergyConsumptionWhenActive = 480f;
        def.SelfHeatKilowattsWhenActive = 8f;
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
        go.AddOrGet<ComputeRoom>();
    }
}

public sealed class VacuumLabConfig : IBuildingConfig
{
    public const string ID = "IR_VacuumLab";

    public override BuildingDef CreateBuildingDef()
    {
        // Placeholder animation: reuses the Orbital Research Station's.
        var def = BuildingTemplates.CreateBuildingDef(
            ID,
            4,
            4,
            "research_space_kanim",
            30,
            90f,
            new[] { 500f, 500f },
            new[] { MATERIALS.REFINED_METAL, MATERIALS.GLASS },
            1600f,
            BuildLocationRule.OnFloor,
            DECOR.NONE,
            NOISE_POLLUTION.NONE);
        def.RequiresPowerInput = true;
        def.EnergyConsumptionWhenActive = 960f;
        def.SelfHeatKilowattsWhenActive = 12f;
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
        go.AddOrGet<VacuumLab>();
    }
}
