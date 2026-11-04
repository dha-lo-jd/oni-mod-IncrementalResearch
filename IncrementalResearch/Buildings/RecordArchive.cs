using System.Collections.Generic;
using IncrementalResearch.Core;

namespace IncrementalResearch;

// Tier 1 facility. Needs no worker; it only has to stay powered.
public sealed class RecordArchive : KMonoBehaviour, ISim200ms, ISidescreenButtonControl
{
    private static readonly List<RecordArchive> Instances = new List<RecordArchive>();

    [MyCmpGet] private Operational operational;
    [MyCmpGet] private Storage storage;
    [MyCmpGet] private ManualDeliveryKG delivery;

    public static bool AnyExists => Instances.Count > 0;

    public static bool AnyOperational
    {
        get
        {
            foreach (var archive in Instances)
            {
                if (archive.operational != null && archive.operational.IsOperational)
                {
                    return true;
                }
            }

            return false;
        }
    }

    // Data Banks delivered to the archive, which a singularity consumes.
    public static int DatabanksStored
    {
        get
        {
            var total = 0;
            foreach (var archive in Instances)
            {
                total += archive.StoredCount;
            }

            return total;
        }
    }

    public static bool ConsumeDatabanks(int count)
    {
        if (DatabanksStored < count)
        {
            return false;
        }

        foreach (var archive in Instances)
        {
            var take = System.Math.Min(count, archive.StoredCount);
            if (take > 0)
            {
                archive.storage.ConsumeIgnoringDisease(DatabankHelper.TAG, take);
                count -= take;
            }
        }

        return count <= 0;
    }

    private int StoredCount =>
        storage == null ? 0 : (int)System.Math.Floor(storage.GetAmountAvailable(DatabankHelper.TAG) + 0.001f);

    protected override void OnSpawn()
    {
        base.OnSpawn();
        Instances.Add(this);
    }

    protected override void OnCleanUp()
    {
        Instances.Remove(this);
        base.OnCleanUp();
    }

    public void Sim200ms(float dt)
    {
        // Power is only drawn while the building is active.
        operational.SetActive(operational.IsOperational);

        // Duplicants only fetch Data Banks while a singularity is in sight, and no more than it needs.
        if (delivery != null)
        {
            var state = KnowledgeState.Instance;
            var wanted = state != null && state.SingularityVisible ? (float)state.DatabanksRequired : 0f;
            delivery.capacity = wanted;
            delivery.refillMass = wanted;
        }
    }

    public string SidescreenButtonText
    {
        get
        {
            var state = KnowledgeState.Instance;
            return state == null
                ? ""
                : string.Format(STRINGS.UI.ARCHIVE.EXPAND_BUTTON, STRINGS.UI.ARCHIVE.UNIT,
                    NumberFormat.FormatScaled(state.NextExpansionCostScaled));
        }
    }

    public string SidescreenButtonTooltip
    {
        get
        {
            var state = KnowledgeState.Instance;
            return state == null
                ? ""
                : string.Format(STRINGS.UI.ARCHIVE.STATUS_TOOLTIP, NumberFormat.FormatScaled(state.KnowledgeScaled),
                    NumberFormat.Format(state.BaseRate), state.HoldingsTier1, STRINGS.UI.ARCHIVE.UNIT,
                    NumberFormat.Format(state.ProductionTier1)) + AffordableUpgradesLine(state);
        }
    }

    private static string AffordableUpgradesLine(KnowledgeState state)
    {
        var count = state.AffordableUpgradeCount();
        return count > 0 ? "\n" + string.Format(STRINGS.UI.UPGRADES.AVAILABLE_TOOLTIP, count) : "";
    }

    public string SidescreenTitle => STRINGS.UI.ARCHIVE.SIDESCREEN_TITLE;

    public void SetButtonTextOverride(ButtonMenuTextOverride textOverride)
    {
    }

    public bool SidescreenEnabled()
    {
        return true;
    }

    public bool SidescreenButtonInteractable()
    {
        return KnowledgeState.Instance != null && KnowledgeState.Instance.CanExpandTier1();
    }

    public void OnSidescreenButtonPressed()
    {
        KnowledgeState.Instance?.TryExpandTier1();
    }

    public int HorizontalGroupID()
    {
        return -1;
    }

    public int ButtonSideScreenSortOrder()
    {
        return 20;
    }
}
