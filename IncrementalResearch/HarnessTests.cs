#if DEBUG
// In-game tests run by Gravitas.TestHarness:
//   python Gravitas.TestProbe/probe.py run --project <this repo> --tests '{"tags":["incrementalresearch"]}'
// Needs HarnessDllPath in Local.Build.props (Check / Wait come from the harness assembly).
// Run it with GRAVITAS_EXCLUSIVE=1 so that only this mod is enabled.
//
// The tests drive KnowledgeState and RewardState (the idle game's save data) directly and check the rules of
// docs/spec-singularity.md and docs/spec-rewards.md. Private state that the game would reach through hours of play
// (unlocked blueprints, expansions bought, upgrades owned) is set through reflection; every test puts the original
// values back (State), so the save the test runs on is left as it was.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Gravitas.TestHarness;
using HarmonyLib;
using IncrementalResearch.Core;
using Klei.AI;
using UnityEngine;

namespace IncrementalResearch;

internal static class HarnessTests
{
    // Copy of every instance field of an object, put back by Dispose.
    private sealed class Backup : IDisposable
    {
        private readonly object target;
        private readonly Dictionary<FieldInfo, object> values = new Dictionary<FieldInfo, object>();

        public Backup(object target)
        {
            this.target = target;
            foreach (var field in target.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                values[field] = Copy(field.GetValue(target));
        }

        private static object Copy(object value)
        {
            if (value == null || value is string || value.GetType().IsValueType || value is UnityEngine.Object) return value;
            if (value is Array array) return array.Clone();
            return Activator.CreateInstance(value.GetType(), value);
        }

        public void Dispose()
        {
            foreach (var pair in values) pair.Key.SetValue(target, Copy(pair.Value));
        }
    }

    // A clean idle game and reward state for one test; everything is restored afterwards.
    private sealed class State : IDisposable
    {
        private readonly Backup knowledgeBackup = new Backup(KnowledgeState.Instance);
        private readonly Backup rewardBackup = new Backup(RewardState.Instance);
        public readonly KnowledgeState K = KnowledgeState.Instance;
        public readonly RewardState R = RewardState.Instance;
        public readonly List<GameObject> Spawned = new List<GameObject>();

        public State()
        {
            Check.NotNull(K, "KnowledgeState is not part of the save");
            Check.NotNull(R, "RewardState is not part of the save");
            var knowledge = Traverse.Create(K);
            knowledge.Field("criticalConstant").SetValue(0);
            knowledge.Field("singularityCount").SetValue(0);
            knowledge.Field("scaleCcProduction").SetValue(0);
            knowledge.Field("scaleCcMi").SetValue(0);
            knowledge.Field("shiftCount").SetValue(0);
            knowledge.Field("scaleMi").SetValue(0);
            knowledge.Field("schoolId").SetValue("");
            knowledge.Field("tutorialFlags").SetValue(int.MaxValue); // no tutorial dialog over the test
            knowledge.Field("knowledge").SetValue(BigInteger.Zero);
            knowledge.Field("produced").SetValue(BigInteger.Zero);
            knowledge.Field("metaInsight").SetValue(BigInteger.Zero);
            knowledge.Field("metaInsightEarned").SetValue(BigInteger.Zero);
            knowledge.Field("upgrades").GetValue<HashSet<string>>().Clear();
            knowledge.Field("automationOff").GetValue<HashSet<string>>().Clear();
            var scales = knowledge.Field("scales").GetValue<int[]>();
            var unlocked = knowledge.Field("unlocked").GetValue<bool[]>();
            for (var tier = 1; tier < scales.Length; tier++)
            {
                scales[tier] = 0;
                if (tier > 1) unlocked[tier] = false;
            }

            var reward = Traverse.Create(R);
            reward.Field("attrSteps").GetValue<Dictionary<string, int>>().Clear();
            reward.Field("attrTickets").SetValue(0);
            reward.Field("attrTicketSteps").SetValue(0);
            reward.Field("interestTickets").SetValue(0);
            reward.Field("lifetimeShifts").SetValue(0);
            reward.Field("milestonesClaimed").SetValue(7);
            reward.Field("milestonesShown").SetValue(7); // no milestone notice over the test
            ClearGrants();
        }

        private void ClearGrants()
        {
            var grants = Traverse.Create(R).Field("grants").GetValue<System.Collections.IList>();
            grants.Clear();
        }

        public void SetCc(int value) => Traverse.Create(K).Field("criticalConstant").SetValue(value);
        public void Set(string field, object value) => Traverse.Create(K).Field(field).SetValue(value);
        public void SetReward(string field, object value) => Traverse.Create(R).Field(field).SetValue(value);
        public int[] Scales => Traverse.Create(K).Field("scales").GetValue<int[]>();
        public bool[] Unlocked => Traverse.Create(K).Field("unlocked").GetValue<bool[]>();
        public HashSet<string> Upgrades => Traverse.Create(K).Field("upgrades").GetValue<HashSet<string>>();
        public int Cc => K.CriticalConstant;

        public MinionIdentity SpawnMinion()
        {
            var go = Arrange.SpawnDuplicant(Arrange.HomeCell());
            Spawned.Add(go);
            return go.GetComponent<MinionIdentity>();
        }

        public void Dispose()
        {
            foreach (var go in Spawned) Arrange.Destroy(go);
            knowledgeBackup.Dispose();
            rewardBackup.Dispose();
            RewardEffects.ApplyAll();
        }
    }

    private static float Attribute(MinionIdentity minion, string id) => minion.GetAttributes().Get(id).GetTotalValue();

    // ---- rewards (docs/spec-rewards.md) ----

    [HarnessTest(Name = "incrementalresearch.rewards_cost_curves", Tags = "incrementalresearch")]
    public static void RewardsCostCurves()
    {
        // Attribute bonuses: round(1 + n^2/5) CC when n steps are already held (the 10th purchase costs 17).
        Check.Equal(1, RewardRules.AttrCost(0), "attribute cost, first");
        Check.Equal(17, RewardRules.AttrCost(9), "attribute cost, 10th purchase");
        Check.Equal(73, RewardRules.AttrCost(19), "attribute cost, 20th purchase");
        Check.Equal(169, RewardRules.AttrCost(29), "attribute cost, 30th purchase");
        Check.Equal(481, RewardRules.AttrCost(49), "attribute cost, 50th purchase");
        // Interests: round(10 + 10 k^2) CC with k granted in the colony.
        Check.Equal(10, RewardRules.InterestCost(0), "interest cost, first");
        Check.Equal(20, RewardRules.InterestCost(1), "interest cost, second");
        Check.Equal(50, RewardRules.InterestCost(2), "interest cost, third");
        Check.Equal(170, RewardRules.InterestCost(4), "interest cost, fifth");
        Check.Equal(820, RewardRules.InterestCost(9), "interest cost, tenth");
        Check.Equal(5, RewardRules.AttrResetCost, "attribute reset cost");
        Check.Equal(5, RewardRules.InterestResetCost, "interest reset cost");
    }

    [HarnessTest(Name = "incrementalresearch.rewards_attribute_bonus", Tags = "incrementalresearch", TimeoutSeconds = 240)]
    public static IEnumerator RewardsAttributeBonus()
    {
        using var s = new State();
        var minion = s.SpawnMinion();
        yield return null;
        var ids = RewardEffects.AttributeIds;
        Check.Equal(11, ids.Length, "attributes that can be boosted");
        var before = ids.ToDictionary(id => id, id => Attribute(minion, id));

        // Nothing to spend: no purchase.
        Check.False(s.R.CanBuyAttr, "bought without CC or tickets");
        Check.False(s.R.TryBuyAttr(), "a purchase went through without CC");

        // Ten purchases cost 67 CC in all, each adding +0.1 to one attribute of the existing Duplicant.
        s.SetCc(1000);
        for (var i = 0; i < 10; i++) Check.True(s.R.TryBuyAttr(), "purchase " + (i + 1));
        Check.Equal(933, s.Cc, "CC left after 10 purchases (67 spent)");
        Check.Equal(10, s.R.TotalAttrSteps, "steps after 10 purchases");
        foreach (var id in ids)
            Check.Near(before[id] + s.R.AttrBonus(id), Attribute(minion, id), 0.001, "attribute " + id + " of the Duplicant");
        Check.Near(1.0, ids.Sum(id => Attribute(minion, id) - before[id]), 0.01, "total added to the Duplicant");

        // A Duplicant that arrives later gets the bonuses on its own (MinionIdentity.OnSpawn), with the reward's description.
        var late = s.SpawnMinion();
        yield return null;
        Check.Near(1.0, ids.Sum(id => s.R.AttrBonus(id)), 0.001, "bonuses held");
        foreach (var id in ids)
        {
            var bonus = s.R.AttrBonus(id);
            var carried = 0f;
            var modifiers = late.GetAttributes().Get(id).Modifiers;
            for (var i = 0; i < modifiers.Count; i++)
                if (modifiers[i].GetDescription() == (string)STRINGS.UI.REWARDS.MODIFIER_DESC) carried += modifiers[i].Value;
            Check.Near(bonus, carried, 0.001, "reward modifier on the late Duplicant for " + id);
        }

        // Reset: 5 CC, every bonus gone.
        Check.True(s.R.TryResetAttr(), "reset");
        Check.Equal(928, s.Cc, "CC left after the reset");
        Check.Equal(0, s.R.TotalAttrSteps, "steps after the reset");
        foreach (var id in ids)
            Check.Near(before[id], Attribute(minion, id), 0.001, "attribute " + id + " after the reset");
    }

    [HarnessTest(Name = "incrementalresearch.rewards_attribute_limits", Tags = "incrementalresearch", TimeoutSeconds = 240)]
    public static void RewardsAttributeLimits()
    {
        using var s = new State();
        s.SetCc(1_000_000);
        var spent = 0;
        while (s.R.CanBuyAttr)
        {
            spent += s.R.NextAttrCost;
            Check.True(s.R.TryBuyAttr(), "purchase " + (s.R.TotalAttrSteps + 1));
        }

        Check.Equal(50, s.R.TotalAttrSteps, "purchases are capped at 50");
        Check.True(s.R.AttrAtLimit, "limit not reported");
        Check.Equal(1_000_000 - spent, s.Cc, "CC spent equals the sum of the costs");
        foreach (var id in RewardEffects.AttributeIds)
            Check.True(s.R.AttrSteps(id) <= 20, id + " went beyond +2.0 (" + s.R.AttrSteps(id) + " steps)");
        Check.Near(5.0, RewardEffects.AttributeIds.Sum(id => s.R.AttrBonus(id)), 0.001, "total bonus");
        Check.False(s.R.TryBuyAttr(), "a 51st purchase went through");
        // 50 purchases: sum of round(1 + n^2/5).
        Check.Equal(Enumerable.Range(0, 50).Sum(RewardRules.AttrCost), spent, "total price of the 50 purchases");
    }

    [HarnessTest(Name = "incrementalresearch.rewards_free_tickets", Tags = "incrementalresearch")]
    public static void RewardsFreeTickets()
    {
        using var s = new State();
        s.SetCc(100);
        s.R.DebugAddTickets(2, 0);
        Check.True(s.R.TryBuyAttr(), "first ticket purchase");
        Check.True(s.R.TryBuyAttr(), "second ticket purchase");
        Check.Equal(100, s.Cc, "tickets are used before CC");
        Check.Equal(0, s.R.AttrTickets, "tickets left");
        Check.True(s.R.TryBuyAttr(), "purchase with CC");
        Check.Equal(98, s.Cc, "the third step costs round(1 + 4/5) = 2 CC");

        // Resetting returns the steps bought with tickets as tickets (not the one bought with CC).
        Check.True(s.R.TryResetAttr(), "reset");
        Check.Equal(2, s.R.AttrTickets, "tickets after the reset");
        Check.Equal(93, s.Cc, "CC after the reset");
    }

    [HarnessTest(Name = "incrementalresearch.rewards_interest", Tags = "incrementalresearch", TimeoutSeconds = 240)]
    public static IEnumerator RewardsInterest()
    {
        using var s = new State();
        var minion = s.SpawnMinion();
        var other = s.SpawnMinion();
        yield return null;
        var resume = RewardEffects.Resume(minion);
        var id = RewardEffects.MinionId(minion);
        var otherResume = RewardEffects.Resume(other);
        var before = resume.AptitudeBySkillGroup.ToDictionary(p => p.Key, p => p.Value);
        var otherBefore = otherResume.AptitudeBySkillGroup.Count;

        s.SetCc(100);
        s.R.DebugAddTickets(0, 1);
        Check.True(s.R.TryGrantInterest(minion), "grant with a ticket");
        Check.Equal(0, s.R.InterestTickets, "ticket used");
        Check.Equal(100, s.Cc, "the ticket pays, not CC");
        var first = s.R.GrantedGroups(id).Single();
        Check.False(before.ContainsKey(new HashedString(first)), "an interest the Duplicant already had was chosen");
        Check.Equal(1f, RewardEffects.Aptitude(resume, first), "granted interest level");

        Check.Equal(20, s.R.NextInterestCost, "second grant price (one granted in the colony)");
        Check.True(s.R.TryGrantInterest(minion), "second grant");
        Check.Equal(80, s.Cc, "CC after the second grant");
        Check.Equal(2, s.R.GrantCount(id), "grants on the Duplicant");
        Check.False(s.R.CanGrantInterest(minion), "a third grant is offered for the same Duplicant");
        Check.False(s.R.TryGrantInterest(minion), "a third grant went through");
        Check.Equal(80, s.Cc, "CC spent on a refused grant");
        Check.Equal(otherBefore, otherResume.AptitudeBySkillGroup.Count, "another Duplicant's interests changed");

        // Reset: 5 CC, interests back as before, the ticket comes back.
        Check.True(s.R.TryResetInterest(minion), "reset");
        Check.Equal(75, s.Cc, "CC after the reset");
        Check.Equal(0, s.R.GrantCount(id), "grants after the reset");
        Check.Equal(1, s.R.InterestTickets, "ticket after the reset");
        Check.Equal(before.Count, resume.AptitudeBySkillGroup.Count, "interests after the reset");
        foreach (var pair in before)
            Check.Equal(pair.Value, resume.AptitudeBySkillGroup.TryGetValue(pair.Key, out var now) ? now : 0f, "interest " + pair.Key + " after the reset");
    }

    [HarnessTest(Name = "incrementalresearch.rewards_milestones", Tags = "incrementalresearch", TimeoutSeconds = 300)]
    public static IEnumerator RewardsMilestones()
    {
        using var s = new State();
        s.SetReward("milestonesClaimed", 0);

        // Through a real paradigm shift: the first milestone gives 2 attribute tickets.
        s.Set("produced", BigInteger.Pow(10, 20));
        Check.True(s.K.TryShift(), "paradigm shift");
        Check.Equal(1, s.R.LifetimeShifts, "shifts counted");
        Check.Equal(2, s.R.AttrTickets, "attribute tickets after 1 shift");
        Check.Equal(0, s.R.InterestTickets, "interest tickets after 1 shift");
        s.R.OnShifted();
        Check.Equal(2, s.R.AttrTickets, "a milestone paid twice");

        s.SetReward("lifetimeShifts", 9);
        s.R.OnShifted();
        Check.Equal(5, s.R.AttrTickets, "attribute tickets after 10 shifts (+3)");
        Check.Equal(1, s.R.InterestTickets, "interest tickets after 10 shifts (+1)");
        s.SetReward("lifetimeShifts", 99);
        s.R.OnShifted();
        Check.Equal(10, s.R.AttrTickets, "attribute tickets after 100 shifts (+5, ten in all)");
        Check.Equal(3, s.R.InterestTickets, "interest tickets after 100 shifts (+2, three in all)");
        s.R.OnShifted();
        Check.Equal(10, s.R.AttrTickets, "tickets after the 101st shift");

        // The notice appears once per milestone, one at a time, and not again.
        s.SetReward("milestonesShown", 0);
        var shown = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            yield return Wait.Seconds(1f);
            var dialog = Traverse.Create(s.R).Field("milestoneDialog").GetValue<UnityEngine.Object>();
            Check.NotNull(dialog, "no milestone notice for milestone " + (i + 1));
            shown.Add(Traverse.Create(s.R).Field("milestonesShown").GetValue<int>());
            ((KScreen)dialog).Deactivate();
            yield return null;
        }

        Check.Equal("1,3,7", string.Join(",", shown), "milestones shown, in order");
        yield return Wait.Seconds(1f);
        Check.True(Traverse.Create(s.R).Field("milestoneDialog").GetValue<UnityEngine.Object>() == null, "a notice appeared again");
    }

    // ---- Critical Constant (docs/spec-singularity.md) ----

    [HarnessTest(Name = "incrementalresearch.cc_expansions", Tags = "incrementalresearch")]
    public static void CcExpansions()
    {
        using var s = new State();
        // Cost ceil(2 * 1.6^n).
        Check.Equal("2,4,6,9", string.Join(",", Enumerable.Range(0, 4).Select(KnowledgeRules.CcExpansionCost)), "CC expansion costs");
        s.SetCc(3);
        Check.False(s.K.TryExpandCc(1) && s.K.TryExpandCc(1), "two expansions bought with 3 CC (2 + 4)");
        Check.Equal(1, s.Cc, "CC left after one expansion (2 spent)");
        Check.Equal(1, s.K.ScaleCcMi, "kind 1 raises the Meta-Insight constant");

        // Production: x3.8 with three expansions, never beyond x19.8.
        s.SetCc(0);
        s.Set("scaleCcMi", 0);
        var baseline = s.K.TierMultiplier(1);
        s.Set("scaleCcProduction", 3);
        Check.Near(3.84, s.K.TierMultiplier(1) / baseline, 0.02, "production multiplier of 3 expansions");
        Check.Near(19.8, KnowledgeRules.CcMultiplier(KnowledgeRules.CcProductionFirst, 200), 0.3, "production limit");
        // (docs/spec-singularity.md says about x4.4 for this limit; the formula converges to x4.97.)
        Check.Near(4.97, KnowledgeRules.CcMultiplier(KnowledgeRules.CcMiGainFirst, 200), 0.02, "Meta-Insight limit");
        Check.Near(11.2, KnowledgeRules.CcMultiplier(KnowledgeRules.CcProductionFirst, 8), 0.05, "production multiplier of 8 expansions");

        // The constant of Meta-Insight raises what a shift gives (x1.35 for the first).
        s.Set("produced", BigInteger.Pow(10, 22));
        s.Set("scaleCcMi", 0);
        var gain = (double)s.K.MetaInsightGain;
        Check.True(gain > 1000, "the test Meta-Insight gain is too small to measure (" + gain + ")");
        s.Set("scaleCcMi", 1);
        Check.Near(1.35, (double)s.K.MetaInsightGain / gain, 0.01, "Meta-Insight gain multiplier of one expansion");
    }

    [HarnessTest(Name = "incrementalresearch.singularity_gain_and_requirements", Tags = "incrementalresearch")]
    public static void SingularityGainAndRequirements()
    {
        using var s = new State();
        // Data Banks: 10 + 2n, at most 60.
        foreach (var (done, expected) in new[] { (0, 10), (1, 12), (2, 14), (24, 58), (25, 60), (100, 60) })
        {
            s.Set("singularityCount", done);
            Check.Equal(expected, s.K.DatabanksRequired, "Data Banks needed after " + done + " singularities");
        }

        // CC: five times (floor(log10(Meta-Insight earned)) - 5), at least 1 and at most 10 steps.
        foreach (var (earned, expected) in new[] { (1, 5), (100000, 5), (1000000, 5), (100000000, 15), (12345678901L, 25) })
            Check.Equal(expected, KnowledgeRules.CcGain(earned), "CC for " + earned + " Meta-Insight earned");
        Check.Equal(50, KnowledgeRules.CcGain(BigInteger.Pow(10, 40)), "CC limit");
        s.Set("metaInsightEarned", new BigInteger(100000000));
        Check.Equal(15, s.K.NextCcGain, "NextCcGain follows the Meta-Insight earned");
    }

    // The building that takes the Data Banks, with some of them inside.
    private static GameObject BuildArchive(State s, int databanks)
    {
        var cell = Arrange.FindBuildableCell(RecordArchiveConfig.ID, Arrange.HomeCell(), 150);
        Check.True(cell >= 0, "no free cell for the Record Archive");
        var archive = Arrange.Build(RecordArchiveConfig.ID, cell);
        s.Spawned.Add(archive);
        AddDatabanks(archive, databanks);
        return archive;
    }

    private static void AddDatabanks(GameObject archive, int count)
    {
        var storage = archive.GetComponent<Storage>();
        for (var i = 0; i < count; i++)
            storage.Store(Arrange.Spawn(DatabankHelper.ID, Arrange.CellOf(archive)), true);
    }

    private static void MakeReady(State s)
    {
        s.Unlocked[KnowledgeRules.MaxTier] = true;
        s.Scales[KnowledgeRules.MaxTier] = KnowledgeRules.SingularityMinTopExpansions;
        foreach (var id in new[]
                 {
                     Upgrades.AutoExpand, Upgrades.AutoExpandT2, Upgrades.AutoExpandT3, Upgrades.AutoExpandT4,
                     Upgrades.AutoExpandT5, Upgrades.AutoExpandT6, Upgrades.AutoExpandT7
                 })
            s.Upgrades.Add(id);
    }

    [HarnessTest(Name = "incrementalresearch.singularity_conditions", Tags = "incrementalresearch", TimeoutSeconds = 240)]
    public static IEnumerator SingularityConditions()
    {
        using var s = new State();
        var archive = BuildArchive(s, 9);
        yield return null;
        Check.Equal(9, s.K.DatabanksStored, "Data Banks counted in the archive");
        Check.False(s.K.SingularityConditionsMet, "conditions met from nothing");

        // The four conditions one by one: every one of them alone is not enough.
        s.Unlocked[KnowledgeRules.MaxTier] = true;
        Check.True(s.K.SingularityVisible, "the singularity stays hidden after unlocking the last blueprint");
        Check.False(s.K.SingularityConditionsMet, "met with only the blueprint");
        s.Scales[KnowledgeRules.MaxTier] = 44;
        MakeReady(s);
        s.Scales[KnowledgeRules.MaxTier] = 44;
        Check.False(s.K.SingularityConditionsMet, "met with 44 top tier expansions");
        s.Scales[KnowledgeRules.MaxTier] = 45;
        Check.True(s.K.SingularityConditionsMet, "not met with 45 expansions and every auto-expand");
        s.Upgrades.Remove(Upgrades.AutoExpandT4);
        Check.False(s.K.SingularityConditionsMet, "met with an auto-expand missing");
        s.Upgrades.Add(Upgrades.AutoExpandT4);
        s.Unlocked[KnowledgeRules.MaxTier] = false;
        Check.False(s.K.SingularityConditionsMet, "met without the last blueprint");
        s.Unlocked[KnowledgeRules.MaxTier] = true;
        Check.True(s.K.SingularityConditionsMet, "conditions lost");

        // Data Banks: 9 are not enough for the first one, 10 are; 16 needed after three.
        Check.False(s.K.CanSingularity, "allowed with 9 Data Banks");
        Check.False(s.K.TrySingularity(), "a singularity ran with 9 Data Banks");
        Check.Equal(0, s.K.SingularityCount, "singularities after a refused one");
        Check.Equal(9, s.K.DatabanksStored, "Data Banks consumed by a refused singularity");
        AddDatabanks(archive, 1);
        Check.True(s.K.CanSingularity, "refused with 10 Data Banks");
        s.Set("singularityCount", 3);
        Check.False(s.K.CanSingularity, "allowed with 10 Data Banks after three singularities (16 needed)");
    }

    [HarnessTest(Name = "incrementalresearch.singularity_resets_and_keeps", Tags = "incrementalresearch", TimeoutSeconds = 240)]
    public static IEnumerator SingularityResetsAndKeeps()
    {
        using var s = new State();
        var archive = BuildArchive(s, 12);
        yield return null;
        MakeReady(s);
        for (var tier = 1; tier < s.Scales.Length; tier++) s.Scales[tier] = tier == KnowledgeRules.MaxTier ? 45 : 7;
        s.Upgrades.Add(Upgrades.AutoShift);   // a CC upgrade: stays
        s.Upgrades.Add(Upgrades.AutoUpgrade); // an Insight upgrade: goes
        s.Upgrades.Add(Upgrades.AutoMiUpgrade);
        Traverse.Create(s.K).Field("automationOff").GetValue<HashSet<string>>().Add(Upgrades.AutoShift);
        s.SetCc(7);
        s.Set("scaleCcProduction", 2);
        s.Set("scaleCcMi", 1);
        s.Set("shiftCount", 5);
        s.Set("scaleMi", 3);
        s.Set("schoolId", Schools.All.First().Id);
        s.Set("knowledge", new BigInteger(123456));
        s.Set("produced", new BigInteger(654321));
        s.Set("metaInsight", new BigInteger(50));
        s.Set("metaInsightEarned", new BigInteger(100000000));
        s.Set("tutorialFlags", 0x1FFF);
        s.SetReward("lifetimeShifts", 5);
        s.SetReward("attrTickets", 4);

        Check.True(s.K.CanSingularity, "the colony is ready for a singularity");
        Check.True(s.K.TrySingularity(), "singularity");

        // Reset.
        Check.Equal(2, s.K.DatabanksStored, "Data Banks left after consuming 10 of 12");
        Check.Equal(1, s.K.SingularityCount, "singularity count");
        for (var tier = 1; tier < s.Scales.Length; tier++)
            Check.Equal(0, s.Scales[tier], "expansions of tier " + tier);
        for (var tier = 2; tier < s.Unlocked.Length; tier++)
            Check.False(s.Unlocked[tier], "blueprint of tier " + tier + " still unlocked");
        Check.Equal(BigInteger.Zero, s.K.KnowledgeScaled, "Insight");
        Check.Equal(BigInteger.Zero, s.K.ProducedScaled, "produced this cycle");
        Check.Equal(BigInteger.Zero, s.K.MetaInsight, "Meta-Insight");
        Check.Equal(BigInteger.Zero, s.K.MetaInsightEarned, "Meta-Insight earned");
        Check.Equal(0, s.K.ShiftCount, "paradigm shifts");
        Check.Equal(0, s.K.ScaleMi, "Meta-Insight expansions");
        Check.Equal(null, s.K.CurrentSchool, "school");
        Check.False(s.K.HasUpgrade(Upgrades.AutoUpgrade), "an Insight upgrade survived");
        Check.False(s.K.HasUpgrade(Upgrades.AutoExpandT7), "an auto-expand survived");
        Check.False(s.K.SingularityConditionsMet, "conditions still met right after the singularity");

        // Kept: CC (+15), CC expansions, CC upgrades and their switch, tutorial, rewards, the building.
        Check.Equal(22, s.Cc, "CC: 7 + 15");
        Check.Equal(2, s.K.ScaleCcProduction, "production constants");
        Check.Equal(1, s.K.ScaleCcMi, "Meta-Insight constants");
        Check.True(s.K.HasUpgrade(Upgrades.AutoShift), "a CC upgrade was lost");
        Check.False(s.K.IsAutomationEnabled(Upgrades.AutoShift), "the switch of an automation was reset");
        Check.Equal(0x1FFF, Traverse.Create(s.K).Field("tutorialFlags").GetValue<int>(), "tutorial progress");
        Check.Equal(5, s.R.LifetimeShifts, "lifetime shifts (milestones)");
        Check.Equal(4, s.R.AttrTickets, "tickets");
        Check.True(archive != null, "the archive was removed");

        // The second one needs 12 Data Banks: the 2 left are not enough.
        MakeReady(s);
        Check.Equal(12, s.K.DatabanksRequired, "Data Banks for the second singularity");
        Check.False(s.K.CanSingularity, "second singularity allowed with 2 Data Banks");
    }

    [HarnessTest(Name = "incrementalresearch.singularity_databank_requests", Tags = "incrementalresearch", TimeoutSeconds = 240)]
    public static IEnumerator SingularityDatabankRequests()
    {
        using var s = new State();
        var archive = BuildArchive(s, 0);
        var delivery = archive.GetComponent<ManualDeliveryKG>();
        Check.NotNull(delivery, "the archive has no delivery component");
        yield return Wait.Seconds(1f);
        Check.Equal(0f, delivery.capacity, "Data Banks requested before the last blueprint is unlocked");

        s.Unlocked[KnowledgeRules.MaxTier] = true;
        yield return Wait.Seconds(1f);
        Check.Equal(10f, delivery.capacity, "Data Banks requested once the singularity is in sight");
        Check.Equal(10f, delivery.refillMass, "refill amount");

        s.Set("singularityCount", 5);
        yield return Wait.Seconds(1f);
        Check.Equal(20f, delivery.capacity, "request after five singularities");
        s.Set("singularityCount", 40);
        yield return Wait.Seconds(1f);
        Check.Equal(60f, delivery.capacity, "request is capped at 60");
        Check.Equal(DatabankHelper.TAG, delivery.RequestedItemTag, "the archive asks for Data Banks");
    }

    [HarnessTest(Name = "incrementalresearch.auto_shift_threshold", Tags = "incrementalresearch", TimeoutSeconds = 240)]
    public static IEnumerator AutoShiftThreshold()
    {
        using var s = new State();
        s.Upgrades.Add(Upgrades.AutoShift);
        s.Set("produced", BigInteger.Pow(10, 22));
        var gain = s.K.MetaInsightGain;
        Check.True(gain >= 1, "no Meta-Insight to gain in the test");
        // The shift waits until the gain reaches the set percent of what has been earned so far.
        s.Set("metaInsightEarned", gain * 100);
        s.K.SetAutoShiftPercent(2); // needs 200% of the earned amount: the gain is 1%
        yield return Wait.Seconds(1f);
        Check.Equal(0, s.K.ShiftCount, "shifted below the threshold");

        s.K.SetAutoShiftPercent(1); // the gain is exactly 1%
        yield return Wait.Seconds(1f);
        Check.Equal(1, s.K.ShiftCount, "did not shift at the threshold");
        Check.Equal(gain + gain * 100, s.K.MetaInsightEarned, "Meta-Insight earned after the automatic shift");

        // Switched off: no shift.
        s.Set("produced", BigInteger.Pow(10, 22));
        s.Set("metaInsightEarned", BigInteger.Zero);
        s.K.ToggleAutomation(Upgrades.AutoShift);
        yield return Wait.Seconds(1f);
        Check.Equal(1, s.K.ShiftCount, "shifted while the automation was switched off");
        s.K.ToggleAutomation(Upgrades.AutoShift);
        yield return Wait.Seconds(1f);
        Check.Equal(2, s.K.ShiftCount, "did not shift once switched on again");
    }
}
#endif
