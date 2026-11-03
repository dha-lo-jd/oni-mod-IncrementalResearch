using System;
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

    protected override float ExtraHeatKilowatts =>
        KnowledgeRules.VacuumLabExtraHeat(KnowledgeState.Instance?.Holdings(3) ?? 0);

    protected override string EnvironmentRequirement => string.Format(STRINGS.UI.TIERS.REQUIRE_MASS,
        NumberFormat.Format(MaxCellMass * 1000f), NumberFormat.Format(ExtraHeatKilowatts));

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

// Tier 4 facility. Draws more electric power the more it is expanded.
public sealed class ParticleAccelerator : TierFacility
{
    public override int Tier => 4;

    protected override float PowerKilowatts =>
        KnowledgeRules.AcceleratorPower(KnowledgeState.Instance?.Holdings(4) ?? 1);

    protected override string EnvironmentRequirement =>
        string.Format(STRINGS.UI.TIERS.REQUIRE_POWER, NumberFormat.Format(PowerKilowatts));

    protected override StatusItem FindProblem()
    {
        return null;
    }
}

// Tier 5 facility. Draws more electric power the more it is expanded.
public sealed class ConfinementStructure : TierFacility
{
    public override int Tier => 5;

    protected override float PowerKilowatts =>
        KnowledgeRules.ConfinementPower(KnowledgeState.Instance?.Holdings(5) ?? 1);

    protected override string EnvironmentRequirement =>
        string.Format(STRINGS.UI.TIERS.REQUIRE_POWER, NumberFormat.Format(PowerKilowatts));

    protected override StatusItem FindProblem()
    {
        return null;
    }
}

// Base of the tier 6 and 7 facilities: they need an area around them to be free of solid tiles.
// A cell is free when it holds no natural solid and no built tile; gas, liquid, wires, pipes and ladders are fine,
// and so is any building without a solid foundation. Cells outside the world count as blocked.
public abstract class ClearanceFacility : TierFacility
{
    private static StatusItem blockedProblem;

    [MyCmpGet] private Building building;
    private int ticksUntilScan;
    private int blockedCells;

    // Cells to keep free, given the holdings and the columns [xMin, xMax] and bottom row the facility occupies.
    protected abstract void GetArea(int holdings, int xMin, int xMax, int facilityHeight,
        out int left, out int right, out int height);

    protected int BlockedCells => blockedCells;

    protected override StatusItem FindProblem()
    {
        // The scan can cover hundreds of cells, so it runs once a second rather than on every tick.
        if (ticksUntilScan-- <= 0)
        {
            ticksUntilScan = 4;
            Scan();
        }

        if (blockedCells == 0)
        {
            return null;
        }

        return blockedProblem ??= CreateProblem("IR_AreaBlocked", STRINGS.UI.TIERS.BLOCKED_NAME,
            STRINGS.UI.TIERS.BLOCKED_TOOLTIP);
    }

    private void Scan()
    {
        var xMin = int.MaxValue;
        var xMax = int.MinValue;
        var yBottom = int.MaxValue;
        var yTop = int.MinValue;
        foreach (var cell in building.PlacementCells)
        {
            Grid.CellToXY(cell, out var x, out var y);
            xMin = Math.Min(xMin, x);
            xMax = Math.Max(xMax, x);
            yBottom = Math.Min(yBottom, y);
            yTop = Math.Max(yTop, y);
        }

        var holdings = KnowledgeState.Instance?.Holdings(Tier) ?? 1;
        GetArea(holdings, xMin, xMax, yTop - yBottom + 1, out var left, out var right, out var height);

        var worldId = this.GetMyWorldId();
        var blocked = 0;
        for (var y = yBottom; y < yBottom + height; y++)
        {
            for (var x = xMin - left; x <= xMax + right; x++)
            {
                if (x >= xMin && x <= xMax && y <= yTop)
                {
                    continue;
                }

                if (x < 0 || y < 0 || x >= Grid.WidthInCells || y >= Grid.HeightInCells)
                {
                    blocked++;
                    continue;
                }

                var cell = Grid.XYToCell(x, y);
                if (!Grid.IsValidCellInWorld(cell, worldId) || Grid.Solid[cell] || Grid.Foundation[cell])
                {
                    blocked++;
                }
            }
        }

        blockedCells = blocked;
    }
}

// Tier 6 facility: a clear band to its left and right, as tall as the facility.
public sealed class ObservationNet : ClearanceFacility
{
    public override int Tier => 6;

    protected override void GetArea(int holdings, int xMin, int xMax, int facilityHeight,
        out int left, out int right, out int height)
    {
        var length = KnowledgeRules.NetBandLength(holdings);
        left = length / 2;
        right = length - left;
        height = facilityHeight;
    }

    protected override string EnvironmentRequirement => string.Format(STRINGS.UI.TIERS.REQUIRE_BAND,
        KnowledgeRules.NetBandLength(KnowledgeState.Instance?.Holdings(6) ?? 1), BlockedCells);
}

// Tier 7 facility: a clear rectangle from its base upward, centered on it.
public sealed class ExtractionFurnace : ClearanceFacility
{
    public override int Tier => 7;

    protected override void GetArea(int holdings, int xMin, int xMax, int facilityHeight,
        out int left, out int right, out int height)
    {
        var extra = Math.Max(0, KnowledgeRules.FurnaceWidth(holdings) - (xMax - xMin + 1));
        left = extra / 2;
        right = extra - left;
        height = KnowledgeRules.FurnaceHeight(holdings);
    }

    protected override string EnvironmentRequirement => string.Format(STRINGS.UI.TIERS.REQUIRE_RECT,
        KnowledgeRules.FurnaceWidth(KnowledgeState.Instance?.Holdings(7) ?? 1),
        KnowledgeRules.FurnaceHeight(KnowledgeState.Instance?.Holdings(7) ?? 1), BlockedCells);
}
