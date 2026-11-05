using System;
using Database;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using IncrementalResearch.Ui;
using KSerialization;

namespace IncrementalResearch.Core;

// Rewards bought with Critical Constant, and the paradigm shift milestones. Stored in the save game.
[SerializationConfig(MemberSerialization.OptIn)]
public sealed class RewardState : KMonoBehaviour, ISim200ms
{
    public static RewardState Instance { get; private set; }

    // "AttributeId=steps;..." and "minionId:skillGroupId:previousAptitude;..." as text.
    [Serialize] private string attrStepsText = "";
    [Serialize] private string grantsText = "";
    [Serialize] private int attrTickets;
    // Attribute steps bought with free draws; they are given back as free draws when the bonuses are reset.
    [Serialize] private int attrTicketSteps;
    [Serialize] private int interestTickets;
    [Serialize] private int lifetimeShifts;
    [Serialize] private int milestonesClaimed;
    [Serialize] private int milestonesShown;

    private readonly Dictionary<string, int> attrSteps = new Dictionary<string, int>();
    private readonly List<InterestGrant> grants = new List<InterestGrant>();
    private UnityEngine.Object milestoneDialog;

    private struct InterestGrant
    {
        public int MinionId;
        public string GroupId;
        public float Previous;
        public bool UsedTicket;
    }

    public int AttrTickets => attrTickets;
    public int InterestTickets => interestTickets;
    public int LifetimeShifts => lifetimeShifts;

    public int TotalAttrSteps
    {
        get
        {
            var total = 0;
            foreach (var steps in attrSteps.Values)
            {
                total += steps;
            }

            return total;
        }
    }

    public int AttrSteps(string attributeId)
    {
        return attrSteps.TryGetValue(attributeId, out var steps) ? steps : 0;
    }

    public float AttrBonus(string attributeId)
    {
        return AttrSteps(attributeId) * RewardRules.AttrStep;
    }

    public int NextAttrCost => RewardRules.AttrCost(TotalAttrSteps);
    public int NextInterestCost => RewardRules.InterestCost(grants.Count);
    public bool AttrAtLimit => TotalAttrSteps >= RewardRules.AttrMaxStepsTotal;

    protected override void OnPrefabInit()
    {
        base.OnPrefabInit();
        Instance = this;
    }

    protected override void OnSpawn()
    {
        base.OnSpawn();
        var knowledge = KnowledgeState.Instance;
        if (knowledge != null && lifetimeShifts < knowledge.ShiftCount)
        {
            // Saves from before the rewards existed: count the shifts already made.
            lifetimeShifts = knowledge.ShiftCount;
        }
    }

    protected override void OnCleanUp()
    {
        if (Instance == this)
        {
            Instance = null;
            RewardEffects.ClearCache();
        }

        base.OnCleanUp();
    }

    [OnSerializing]
    private void OnSerializing()
    {
        var parts = new List<string>();
        foreach (var pair in attrSteps)
        {
            if (pair.Value > 0)
            {
                parts.Add(pair.Key + "=" + pair.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        attrStepsText = string.Join(";", parts);
        parts.Clear();
        foreach (var grant in grants)
        {
            parts.Add(grant.MinionId.ToString(CultureInfo.InvariantCulture) + ":" + grant.GroupId + ":" +
                      grant.Previous.ToString(CultureInfo.InvariantCulture) + ":" + (grant.UsedTicket ? "1" : "0"));
        }

        grantsText = string.Join(";", parts);
    }

    [OnDeserialized]
    private void OnDeserialized()
    {
        attrSteps.Clear();
        foreach (var part in (attrStepsText ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=');
            if (pair.Length == 2 && int.TryParse(pair[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var steps))
            {
                attrSteps[pair[0]] = steps;
            }
        }

        grants.Clear();
        foreach (var part in (grantsText ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = part.Split(':');
            if (fields.Length >= 3 &&
                int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) &&
                float.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var previous))
            {
                grants.Add(new InterestGrant
                {
                    MinionId = id, GroupId = fields[1], Previous = previous,
                    UsedTicket = fields.Length > 3 && fields[3] == "1"
                });
            }
        }
    }

    // ---- attribute bonus ----

    public bool CanBuyAttr =>
        !AttrAtLimit && (attrTickets > 0 || CriticalConstant() >= NextAttrCost);

    // Buys one step on a random attribute that is not at its limit. Free draws are used before CC.
    public bool TryBuyAttr()
    {
        if (!CanBuyAttr)
        {
            return false;
        }

        var open = new List<string>();
        foreach (var id in RewardEffects.AttributeIds)
        {
            if (AttrSteps(id) < RewardRules.AttrMaxStepsPerAttribute)
            {
                open.Add(id);
            }
        }

        if (open.Count == 0)
        {
            return false;
        }

        if (attrTickets > 0)
        {
            attrTickets--;
            attrTicketSteps++;
        }
        else if (!KnowledgeState.Instance.TrySpendCriticalConstant(NextAttrCost))
        {
            return false;
        }

        var picked = open[UnityEngine.Random.Range(0, open.Count)];
        attrSteps[picked] = AttrSteps(picked) + 1;
        RewardEffects.ApplyAll();
        return true;
    }

    public bool CanResetAttr => TotalAttrSteps > 0 && CriticalConstant() >= RewardRules.AttrResetCost;

    // Clears the bonuses and the purchase history of the whole colony.
    public bool TryResetAttr()
    {
        if (!CanResetAttr || !KnowledgeState.Instance.TrySpendCriticalConstant(RewardRules.AttrResetCost))
        {
            return false;
        }

        attrSteps.Clear();
        attrTickets += attrTicketSteps;
        attrTicketSteps = 0;
        RewardEffects.ApplyAll();
        return true;
    }

    // ---- interests ----

    public int GrantCount(int minionId)
    {
        var count = 0;
        foreach (var grant in grants)
        {
            if (grant.MinionId == minionId)
            {
                count++;
            }
        }

        return count;
    }

    public List<string> GrantedGroups(int minionId)
    {
        var result = new List<string>();
        foreach (var grant in grants)
        {
            if (grant.MinionId == minionId)
            {
                result.Add(grant.GroupId);
            }
        }

        return result;
    }

    // Skill groups the Duplicant has no interest in yet.
    private static List<string> OpenGroups(MinionResume resume)
    {
        var open = new List<string>();
        foreach (SkillGroup group in Db.Get().SkillGroups.resources)
        {
            // Only groups that show up as an interest: they have a name and relevant attributes.
            if (!string.IsNullOrEmpty(group.Name) && group.relevantAttributes != null &&
                group.relevantAttributes.Count > 0 && RewardEffects.Aptitude(resume, group.Id) <= 0f)
            {
                open.Add(group.Id);
            }
        }

        return open;
    }

    public bool CanGrantInterest(MinionIdentity minion)
    {
        var resume = RewardEffects.Resume(minion);
        if (resume == null || GrantCount(RewardEffects.MinionId(minion)) >= RewardRules.InterestMaxPerMinion ||
            OpenGroups(resume).Count == 0)
        {
            return false;
        }

        return interestTickets > 0 || CriticalConstant() >= NextInterestCost;
    }

    // Gives the Duplicant a random interest it does not have. Free draws are used before CC.
    public bool TryGrantInterest(MinionIdentity minion)
    {
        if (!CanGrantInterest(minion))
        {
            return false;
        }

        var resume = RewardEffects.Resume(minion);
        var open = OpenGroups(resume);
        var usedTicket = interestTickets > 0;
        if (usedTicket)
        {
            interestTickets--;
        }
        else if (!KnowledgeState.Instance.TrySpendCriticalConstant(NextInterestCost))
        {
            return false;
        }

        var group = open[UnityEngine.Random.Range(0, open.Count)];
        grants.Add(new InterestGrant
        {
            MinionId = RewardEffects.MinionId(minion),
            GroupId = group,
            Previous = RewardEffects.Aptitude(resume, group),
            UsedTicket = usedTicket
        });
        RewardEffects.SetAptitude(resume, group, RewardRules.InterestAmount);
        return true;
    }

    public bool CanResetInterest(MinionIdentity minion)
    {
        return minion != null && GrantCount(RewardEffects.MinionId(minion)) > 0 &&
               CriticalConstant() >= RewardRules.InterestResetCost;
    }

    // Removes the granted interests of one Duplicant and puts back what it had before.
    public bool TryResetInterest(MinionIdentity minion)
    {
        if (!CanResetInterest(minion) ||
            !KnowledgeState.Instance.TrySpendCriticalConstant(RewardRules.InterestResetCost))
        {
            return false;
        }

        var id = RewardEffects.MinionId(minion);
        var resume = RewardEffects.Resume(minion);
        for (var i = grants.Count - 1; i >= 0; i--)
        {
            if (grants[i].MinionId != id)
            {
                continue;
            }

            if (resume != null)
            {
                RewardEffects.SetAptitude(resume, grants[i].GroupId, grants[i].Previous);
            }

            if (grants[i].UsedTicket)
            {
                interestTickets++;
            }

            grants.RemoveAt(i);
        }

        return true;
    }

    // ---- milestones ----

    // Called by KnowledgeState after every paradigm shift.
    public void OnShifted()
    {
        lifetimeShifts++;
        for (var i = 0; i < RewardRules.MilestoneShifts.Length; i++)
        {
            if (lifetimeShifts >= RewardRules.MilestoneShifts[i] && (milestonesClaimed & (1 << i)) == 0)
            {
                milestonesClaimed |= 1 << i;
                attrTickets += RewardRules.MilestoneAttrTickets[i];
                interestTickets += RewardRules.MilestoneInterestTickets[i];
            }
        }
    }

    public void Sim200ms(float dt)
    {
        // Wait while the previous notice is still on screen. Checked through the dialog object itself, not a close
        // callback, so a notice that is closed in an unexpected way cannot block the later ones.
        if (milestoneDialog != null)
        {
            return;
        }

        for (var i = 0; i < RewardRules.MilestoneShifts.Length; i++)
        {
            if ((milestonesClaimed & (1 << i)) != 0 && (milestonesShown & (1 << i)) == 0)
            {
                milestonesShown |= 1 << i;
                milestoneDialog = RewardDialog.ShowMilestone(RewardRules.MilestoneShifts[i],
                    RewardRules.MilestoneAttrTickets[i], RewardRules.MilestoneInterestTickets[i]);
                return;
            }
        }
    }

    private static int CriticalConstant()
    {
        return KnowledgeState.Instance == null ? 0 : KnowledgeState.Instance.CriticalConstant;
    }

    public void DebugAddTickets(int attr, int interest)
    {
        attrTickets += attr;
        interestTickets += interest;
    }
}
