using HarmonyLib;
using KMod;

namespace IncrementalResearch;

public sealed class IncrementalResearchMod : UserMod2
{
    public override void OnLoad(Harmony harmony)
    {
        base.OnLoad(harmony);
        harmony.PatchAll(typeof(IncrementalResearchMod).Assembly);
    }
}
