using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using ReasonableMoodsAndNeeds.Core;
using Verse;

namespace ReasonableMoodsAndNeeds.Game
{
    /// <summary>
    /// The mod's only Harmony patch. It runs when a pawn gains a memory (an event, not a tick), and it is
    /// only installed while battle focus is switched on. With the feature off, the game runs unpatched.
    /// Harmony prefixes must be static, so the precomputed lookup lives in static fields that
    /// <see cref="Configure"/> replaces wholesale on every settings apply.
    /// </summary>
    public static class BattleFocusPatch
    {
        private static readonly MethodInfo? Target = AccessTools.Method(
            typeof(MemoryThoughtHandler),
            nameof(MemoryThoughtHandler.TryGainMemory),
            new[] { typeof(Thought_Memory), typeof(Pawn) });

        private static bool[] eligibleByIndex = Array.Empty<bool>();
        private static BattleFocusSettings settings = new BattleFocusSettings();
        private static int windowTicks = 1;
        private static bool isPatched;
        private static bool hasReportedMissingTarget;

        public static void Configure(Harmony harmony, ThoughtCatalog catalog, BattleFocusSettings current)
        {
            settings = current.Clone();
            windowTicks = BattleFocus.WindowTicks(settings);
            eligibleByIndex = EligibleMask(catalog, settings);

            if (settings.Enabled == isPatched)
                return;

            if (Target == null)
            {
                if (hasReportedMissingTarget)
                    return;

                hasReportedMissingTarget = true;
                Log.Error("[Reasonable Moods and Needs] MemoryThoughtHandler.TryGainMemory(Thought_Memory, Pawn) was not found; battle focus is disabled.");
                return;
            }

            if (settings.Enabled)
                harmony.Patch(Target, prefix: new HarmonyMethod(typeof(BattleFocusPatch), nameof(Prefix)));
            else
                harmony.Unpatch(Target, HarmonyPatchType.Prefix, harmony.Id);

            isPatched = settings.Enabled;
        }

        /// <returns>False skips the original, which means the memory is never gained.</returns>
        public static bool Prefix(MemoryThoughtHandler __instance, Thought_Memory newThought)
        {
            var index = newThought.def.index;

            if (index >= eligibleByIndex.Length || !eligibleByIndex[index])
                return true;

            var pawn = __instance.pawn;

            if (pawn == null || !pawn.IsColonist || !IsInCombat(pawn))
                return true;

            // CurStage can depend on the owning pawn, and vanilla only assigns it after this prefix runs.
            // It assigns the same pawn a line later, so setting it early changes nothing else.
            newThought.pawn = pawn;

            var stage = newThought.CurStage;

            return stage == null || !BattleFocus.ShouldIgnore(stage.baseMoodEffect, settings);
        }

        private static bool IsInCombat(Pawn pawn) =>
            pawn.Drafted || (pawn.mindState != null && pawn.mindState.WasRecentlyCombatantTicks(windowTicks));

        private static bool[] EligibleMask(ThoughtCatalog catalog, BattleFocusSettings current)
        {
            var mask = new bool[DefDatabase<ThoughtDef>.DefCount];

            if (!current.Enabled)
                return mask;

            foreach (var entry in catalog.Entries)
            {
                var index = entry.Def.index;

                if (index < mask.Length)
                    mask[index] = BattleFocus.IsEligible(entry.Facts, entry.CategoryId, current);
            }

            return mask;
        }
    }
}
