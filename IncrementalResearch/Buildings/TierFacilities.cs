using IncrementalResearch.Core;
using UnityEngine;

namespace IncrementalResearch;

// Tier 2 facility. Runs while the room around it stays cool.
public sealed class ComputeRoom : TierFacility
{
    private static StatusItem tooHot;

    public override int Tier => 2;

    // Heat that grows with the expansions replaces a stricter temperature limit.
    protected override float ExtraHeatKilowatts =>
        KnowledgeRules.ComputeRoomExtraHeat(KnowledgeState.Instance?.Holdings(2) ?? 0);

    protected override string EnvironmentRequirement => string.Format(STRINGS.UI.TIERS.REQUIRE_TEMPERATURE,
        NumberFormat.Format(KnowledgeRules.ComputeRoomMaxTemperature - 273.15f),
        NumberFormat.Format(ExtraHeatKilowatts));

    protected override StatusItem FindProblem()
    {
        var temperature = Grid.Temperature[Grid.PosToCell(this)];
        if (temperature <= KnowledgeRules.ComputeRoomMaxTemperature)
        {
            return null;
        }

        return tooHot ??= CreateProblem("IR_ComputeRoomTooHot", STRINGS.UI.TIERS.TOO_HOT_NAME,
            STRINGS.UI.TIERS.TOO_HOT_TOOLTIP);
    }
}

// Tier 3 facility. Runs only in a vacuum; overheating is left to the game's ordinary overheat mechanism.
public sealed class VacuumLab : TierFacility
{
    private static StatusItem notVacuum;

    [MyCmpGet] private Building building;

    public override int Tier => 3;

    private const float MaxCellMass = KnowledgeRules.VacuumLabMaxCellMass;

    protected override string EnvironmentRequirement => string.Format(STRINGS.UI.TIERS.REQUIRE_MASS,
        NumberFormat.Format(MaxCellMass * 1000f));

    protected override StatusItem FindProblem()
    {
        foreach (var cell in building.PlacementCells)
        {
            if (Grid.Mass[cell] > MaxCellMass)
            {
                return notVacuum ??= CreateProblem("IR_VacuumLabNotVacuum", STRINGS.UI.TIERS.NOT_VACUUM_NAME,
                    STRINGS.UI.TIERS.NOT_VACUUM_TOOLTIP);
            }
        }

        return null;
    }
}
