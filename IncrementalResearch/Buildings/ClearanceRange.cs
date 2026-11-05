using System;
using IncrementalResearch.Core;

namespace IncrementalResearch;

// The free area of the tier 6 and 7 facilities, and its outline. The outline reuses the game's range display (the one
// of the mining robot and the space scanner), which is shown while the building is selected or being placed.
internal static class ClearanceArea
{
    // Cells to keep free on each side of the facility's columns, and the height counted from its bottom row.
    public static void Get(int tier, int holdings, int facilityWidth, int facilityHeight,
        out int left, out int right, out int height)
    {
        if (tier == 6)
        {
            var length = KnowledgeRules.NetBandLength(holdings);
            left = length / 2;
            right = length - left;
            height = facilityHeight;
            return;
        }

        var extra = Math.Max(0, KnowledgeRules.FurnaceWidth(holdings) - facilityWidth);
        left = extra / 2;
        right = extra - left;
        height = KnowledgeRules.FurnaceHeight(holdings);
    }
}

// Keeps the game's RangeVisualizer on a facility (also on its placement preview and construction site) matched to the
// area that the current holdings require.
public sealed class ClearanceRangeSync : KMonoBehaviour
{
    public int Tier;
    public int FacilityWidth;
    public int FacilityHeight;

    private RangeVisualizer visualizer;
    private int shownHoldings = -1;

    public static void Add(UnityEngine.GameObject go, int tier, int width, int height)
    {
        go.AddOrGet<RangeVisualizer>();
        var sync = go.AddOrGet<ClearanceRangeSync>();
        sync.Tier = tier;
        sync.FacilityWidth = width;
        sync.FacilityHeight = height;
        sync.Refresh(1);
    }

    protected override void OnSpawn()
    {
        base.OnSpawn();
        Refresh(CurrentHoldings());
    }

    private void Update()
    {
        var holdings = CurrentHoldings();
        if (holdings != shownHoldings)
        {
            Refresh(holdings);
        }
    }

    private int CurrentHoldings()
    {
        return KnowledgeState.Instance?.Holdings(Tier) ?? 1;
    }

    private void Refresh(int holdings)
    {
        visualizer ??= GetComponent<RangeVisualizer>();
        if (visualizer == null)
        {
            return;
        }

        shownHoldings = holdings;
        ClearanceArea.Get(Tier, holdings, FacilityWidth, FacilityHeight, out var left, out var right, out var height);

        // The origin is the middle column of the bottom row; the facilities are an odd number of cells wide.
        var half = FacilityWidth / 2;
        visualizer.OriginOffset = new Vector2I(0, 0);
        visualizer.RangeMin = new Vector2I(-half - left, 0);
        visualizer.RangeMax = new Vector2I(FacilityWidth - 1 - half + right, height - 1);
        visualizer.TestLineOfSight = false;
        visualizer.BlockingTileVisible = true;
        visualizer.BlockingCb = cell => Grid.IsValidCell(cell) && (Grid.Solid[cell] || Grid.Foundation[cell]);
        visualizer.BlockingVisibleCb = visualizer.BlockingCb;
    }
}
