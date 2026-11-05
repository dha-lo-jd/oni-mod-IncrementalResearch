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

public sealed class AcceleratorConfig : IBuildingConfig
{
    public const string ID = "IR_Accelerator";

    public override BuildingDef CreateBuildingDef()
    {
        // Placeholder animation: reuses the Super Computer one.
        var def = BuildingTemplates.CreateBuildingDef(
            ID,
            4,
            3,
            "research_center2_kanim",
            30,
            120f,
            new[] { 400f, 200f },
            new[] { SimHashes.Steel.ToString(), MATERIALS.PLASTIC },
            1600f,
            BuildLocationRule.OnFloor,
            DECOR.NONE,
            NOISE_POLLUTION.NONE);
        def.RequiresPowerInput = true;
        def.EnergyConsumptionWhenActive = 1600f;
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
        go.AddOrGet<ParticleAccelerator>();
    }
}

public sealed class ConfinementConfig : IBuildingConfig
{
    public const string ID = "IR_Confinement";

    public override BuildingDef CreateBuildingDef()
    {
        // Placeholder animation: reuses the Orbital Research Station one.
        var def = BuildingTemplates.CreateBuildingDef(
            ID,
            4,
            4,
            "research_space_kanim",
            30,
            120f,
            new[] { 600f, 400f },
            new[] { MATERIALS.REFINED_METAL, MATERIALS.GLASS },
            1600f,
            BuildLocationRule.OnFloor,
            DECOR.NONE,
            NOISE_POLLUTION.NONE);
        def.RequiresPowerInput = true;
        def.EnergyConsumptionWhenActive = 3200f;
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
        go.AddOrGet<ConfinementStructure>();
    }
}

public sealed class ObservationNetConfig : IBuildingConfig
{
    public const string ID = "IR_ObservationNet";

    public override BuildingDef CreateBuildingDef()
    {
        // Placeholder animation: reuses the Research Station one.
        var def = BuildingTemplates.CreateBuildingDef(
            ID,
            5,
            3,
            "research_center_kanim",
            30,
            120f,
            new[] { 600f, 300f },
            new[] { MATERIALS.REFINED_METAL, GameTags.BuildingFiber.ToString() },
            1600f,
            BuildLocationRule.OnFloor,
            DECOR.NONE,
            NOISE_POLLUTION.NONE);
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
        go.AddOrGet<ObservationNet>();
        ClearanceRangeSync.Add(go, 6, 5, 3);
    }

    public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
    {
        ClearanceRangeSync.Add(go, 6, 5, 3);
    }

    public override void DoPostConfigureUnderConstruction(GameObject go)
    {
        ClearanceRangeSync.Add(go, 6, 5, 3);
    }
}

public sealed class ExtractionFurnaceConfig : IBuildingConfig
{
    public const string ID = "IR_ExtractionFurnace";

    public override BuildingDef CreateBuildingDef()
    {
        // Placeholder animation: reuses the Orbital Research Station one.
        // The build menu supports at most three material choices, so plastic is left out.
        var def = BuildingTemplates.CreateBuildingDef(
            ID,
            5,
            4,
            "research_space_kanim",
            30,
            120f,
            new[] { 800f, 400f, 400f },
            new[] { SimHashes.Steel.ToString(), MATERIALS.GLASS, SimHashes.Ceramic.ToString() },
            1600f,
            BuildLocationRule.OnFloor,
            DECOR.NONE,
            NOISE_POLLUTION.NONE);
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
        go.AddOrGet<ExtractionFurnace>();
        ClearanceRangeSync.Add(go, 7, 5, 4);
    }

    public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
    {
        ClearanceRangeSync.Add(go, 7, 5, 4);
    }

    public override void DoPostConfigureUnderConstruction(GameObject go)
    {
        ClearanceRangeSync.Add(go, 7, 5, 4);
    }
}
