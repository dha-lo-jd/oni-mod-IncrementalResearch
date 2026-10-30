using System.Collections.Generic;
using IncrementalResearch.Core;

namespace IncrementalResearch;

// Base of the tier 2 and 3 facilities. It runs only while its environment is fine, and shows the reason when not.
// See docs/spec-tiers-2-3.md.
public abstract class TierFacility : KMonoBehaviour, ISim200ms, ISidescreenButtonControl
{
    private static readonly Dictionary<int, List<TierFacility>> Facilities = new Dictionary<int, List<TierFacility>>
    {
        { 2, new List<TierFacility>() },
        { 3, new List<TierFacility>() },
    };

    private static readonly Operational.Flag EnvironmentFlag =
        new Operational.Flag("IncrementalResearch.Environment", Operational.Flag.Type.Requirement);

    [MyCmpGet] private Operational operational;
    [MyCmpGet] private KSelectable selectable;
    private StatusItem shownProblem;

    public abstract int Tier { get; }

    // Returns the status item that explains what is wrong with the environment, or null when it is fine.
    protected abstract StatusItem FindProblem();

    // The environment condition as it stands at the current expansion level.
    protected abstract string EnvironmentRequirement { get; }

    // Heat given off while running, on top of the building's own self heat. Grows with the expansions.
    protected virtual float ExtraHeatKilowatts => 0f;

    public static bool AnyOperational(int tier)
    {
        if (!Facilities.TryGetValue(tier, out var list))
        {
            return false;
        }

        foreach (var facility in list)
        {
            if (facility.operational != null && facility.operational.IsOperational)
            {
                return true;
            }
        }

        return false;
    }

    public static bool AnyExists(int tier)
    {
        return Facilities.TryGetValue(tier, out var list) && list.Count > 0;
    }

    protected override void OnSpawn()
    {
        base.OnSpawn();
        Facilities[Tier].Add(this);
    }

    protected override void OnCleanUp()
    {
        Facilities[Tier].Remove(this);
        base.OnCleanUp();
    }

    public void Sim200ms(float dt)
    {
        var problem = FindProblem();
        operational.SetFlag(EnvironmentFlag, problem == null);
        if (problem != shownProblem)
        {
            if (shownProblem != null)
            {
                selectable.RemoveStatusItem(shownProblem);
            }

            if (problem != null)
            {
                selectable.AddStatusItem(problem);
            }

            shownProblem = problem;
        }

        // Power is only drawn while the building is active.
        operational.SetActive(operational.IsOperational);

        var extraHeat = ExtraHeatKilowatts;
        if (operational.IsOperational && extraHeat > 0f)
        {
            SimMessages.ModifyEnergy(Grid.PosToCell(this), extraHeat * dt, 10000f,
                SimMessages.EnergySourceID.StructureTemperature);
        }
    }

    public string SidescreenButtonText
    {
        get
        {
            var state = KnowledgeState.Instance;
            return state == null
                ? ""
                : string.Format(STRINGS.UI.ARCHIVE.EXPAND_BUTTON, Tiers.UnitName(Tier),
                    NumberFormat.FormatScaled(state.NextExpansionCost(Tier)));
        }
    }

    public string SidescreenButtonTooltip
    {
        get
        {
            var state = KnowledgeState.Instance;
            return state == null
                ? ""
                : string.Format(STRINGS.UI.TIERS.STATUS_TOOLTIP, NumberFormat.FormatScaled(state.KnowledgeScaled),
                      Tiers.UnitName(Tier), state.Holdings(Tier), NumberFormat.Format(state.Activity(Tier) * 100d),
                      NumberFormat.Format(state.Performance(Tier - 1))) + "\n" + EnvironmentRequirement;
        }
    }

    public string SidescreenTitle => Tiers.FacilityName(Tier);

    public void SetButtonTextOverride(ButtonMenuTextOverride textOverride)
    {
    }

    public bool SidescreenEnabled()
    {
        return true;
    }

    public bool SidescreenButtonInteractable()
    {
        return KnowledgeState.Instance != null && KnowledgeState.Instance.CanExpand(Tier);
    }

    public void OnSidescreenButtonPressed()
    {
        KnowledgeState.Instance?.TryExpand(Tier);
    }

    public int HorizontalGroupID()
    {
        return -1;
    }

    public int ButtonSideScreenSortOrder()
    {
        return 20;
    }

    protected static StatusItem CreateProblem(string id, LocString name, LocString tooltip)
    {
        return new StatusItem(id, name.text, tooltip.text, "", StatusItem.IconType.Exclamation,
            NotificationType.BadMinor, false, OverlayModes.None.ID);
    }
}
