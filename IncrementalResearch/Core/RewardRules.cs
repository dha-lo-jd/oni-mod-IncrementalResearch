using System;

namespace IncrementalResearch.Core;

// Rewards for the main game (docs/spec-rewards.md): attribute bonuses for the whole colony and interests for
// single Duplicants, bought with Critical Constant, plus one-time milestone rewards for paradigm shifts.
internal static class RewardRules
{
    // One attribute purchase adds this to one randomly chosen attribute.
    public const float AttrStep = 0.1f;
    public const int AttrMaxStepsPerAttribute = 20;   // +2.0 per attribute
    public const int AttrMaxStepsTotal = 50;          // +5.0 in all
    // Costs grow quadratically with the number already held: attributes 1 + n^2/5, interests 10 + 10 k^2.
    public const double AttrCostQuadratic = 1d / 5d;
    public const int AttrResetCost = 5;

    public const int InterestCostBase = 10;
    public const double InterestCostQuadratic = 10d;
    public const int InterestResetCost = 5;
    public const int InterestMaxPerMinion = 2;
    public const float InterestAmount = 1f;

    // Milestones are counted in paradigm shifts over the whole game (a singularity does not reset them).
    public static readonly int[] MilestoneShifts = { 1, 10, 100 };
    public static readonly int[] MilestoneAttrTickets = { 2, 3, 5 };
    public static readonly int[] MilestoneInterestTickets = { 0, 1, 2 };

    public static int AttrCost(int purchased)
    {
        return (int)Math.Round(1d + AttrCostQuadratic * purchased * purchased, MidpointRounding.AwayFromZero);
    }

    // held = interests currently granted in the whole colony.
    public static int InterestCost(int held)
    {
        return (int)Math.Round(InterestCostBase + InterestCostQuadratic * held * held, MidpointRounding.AwayFromZero);
    }
}
