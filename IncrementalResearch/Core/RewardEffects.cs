using System.Collections.Generic;
using Klei.AI;

namespace IncrementalResearch.Core;

// The parts of the rewards that touch Duplicants: attribute modifiers and interests (aptitudes).
internal static class RewardEffects
{
    private static string[] attributeIds;

    // Modifiers currently on each Duplicant, by KPrefabID instance id, so that they can be swapped for new values.
    private static readonly Dictionary<int, List<AttributeModifier>> Applied = new Dictionary<int, List<AttributeModifier>>();

    public static string[] AttributeIds
    {
        get
        {
            if (attributeIds == null)
            {
                var a = Db.Get().Attributes;
                attributeIds = new[]
                {
                    a.Athletics.Id, a.Strength.Id, a.Digging.Id, a.Construction.Id, a.Machinery.Id, a.Learning.Id,
                    a.Cooking.Id, a.Botanist.Id, a.Art.Id, a.Caring.Id, a.Ranching.Id
                };
            }

            return attributeIds;
        }
    }

    public static string AttributeName(string id)
    {
        var attribute = Db.Get().Attributes.TryGet(id);
        return attribute != null ? attribute.Name : id;
    }

    public static int MinionId(MinionIdentity minion)
    {
        return minion.GetComponent<KPrefabID>().InstanceID;
    }

    public static void ApplyAll()
    {
        foreach (var minion in Components.LiveMinionIdentities.Items)
        {
            ApplyAttributes(minion);
        }
    }

    // Replaces the reward modifiers on one Duplicant with the current bonuses. Safe to call repeatedly.
    public static void ApplyAttributes(MinionIdentity minion)
    {
        var state = RewardState.Instance;
        if (state == null || minion == null)
        {
            return;
        }

        var attributes = minion.GetAttributes();
        if (attributes == null)
        {
            return;
        }

        var id = MinionId(minion);
        if (Applied.TryGetValue(id, out var old))
        {
            foreach (var modifier in old)
            {
                attributes.Remove(modifier);
            }

            old.Clear();
        }
        else
        {
            old = new List<AttributeModifier>();
            Applied[id] = old;
        }

        foreach (var attributeId in AttributeIds)
        {
            var bonus = state.AttrBonus(attributeId);
            if (bonus <= 0f || attributes.Get(attributeId) == null)
            {
                continue;
            }

            var modifier = new AttributeModifier(attributeId, bonus, STRINGS.UI.REWARDS.MODIFIER_DESC, false, false, true);
            attributes.Add(modifier);
            old.Add(modifier);
        }
    }

    public static void ClearCache()
    {
        Applied.Clear();
    }

    public static MinionResume Resume(MinionIdentity minion)
    {
        return minion == null ? null : minion.GetComponent<MinionResume>();
    }

    public static float Aptitude(MinionResume resume, string groupId)
    {
        return resume.AptitudeBySkillGroup.TryGetValue(new HashedString(groupId), out var value) ? value : 0f;
    }

    public static void SetAptitude(MinionResume resume, string groupId, float value)
    {
        var hash = new HashedString(groupId);
        resume.SetAptitude(hash, value);
        if (value <= 0f)
        {
            resume.AptitudeBySkillGroup.Remove(hash);
        }
    }
}
