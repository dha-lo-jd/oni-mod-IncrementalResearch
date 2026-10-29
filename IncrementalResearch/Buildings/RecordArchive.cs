using System.Collections.Generic;
using IncrementalResearch.Core;

namespace IncrementalResearch;

// Tier 1 facility. Needs no worker; it only has to stay powered.
public sealed class RecordArchive : KMonoBehaviour, ISim200ms, ISidescreenButtonControl
{
    private static readonly List<RecordArchive> Instances = new List<RecordArchive>();

    [MyCmpGet] private Operational operational;

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
    }

    public string SidescreenButtonText
    {
        get
        {
            var state = KnowledgeState.Instance;
            return state == null
                ? ""
                : string.Format(STRINGS.UI.ARCHIVE.EXPAND_BUTTON, STRINGS.UI.ARCHIVE.UNIT,
                    FormatNumber(state.NextScaleCostTier1));
        }
    }

    public string SidescreenButtonTooltip
    {
        get
        {
            var state = KnowledgeState.Instance;
            return state == null
                ? ""
                : string.Format(STRINGS.UI.ARCHIVE.STATUS_TOOLTIP, FormatNumber(state.Knowledge),
                    FormatNumber(state.BaseRate), state.ScaleTier1, STRINGS.UI.ARCHIVE.UNIT,
                    FormatNumber(state.ProductionTier1));
        }
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

    private static string FormatNumber(double value)
    {
        return value >= 1e6 ? value.ToString("0.###e+0") : value.ToString("0.##");
    }
}
