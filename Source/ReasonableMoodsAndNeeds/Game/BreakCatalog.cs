using System.Collections.Generic;
using RimWorld;
using ReasonableMoodsAndNeeds.Core;
using Verse;

namespace ReasonableMoodsAndNeeds.Game
{
    public sealed class BreakEntry
    {
        public BreakEntry(MentalBreakDef def)
        {
            Def = def;
            Commonality = def.baseCommonality;
            RequiredTrait = def.requiredTrait;
        }

        public MentalBreakDef Def { get; }

        public float Commonality { get; }

        public TraitDef? RequiredTrait { get; }
    }

    /// <summary>Mental breaks, the states they cause, and the break threshold stat, snapshotted once and rewritten on apply.</summary>
    public sealed class BreakCatalog
    {
        public const string ThresholdStatName = "MentalBreakThreshold";

        private readonly List<BreakEntry> breaks = new List<BreakEntry>();
        private readonly List<StateSnapshot> states = new List<StateSnapshot>();
        private readonly List<KeyValuePair<StatModifier, float>> raceThresholds = new List<KeyValuePair<StatModifier, float>>();
        private readonly StatDef? thresholdStat;
        private readonly float thresholdDefault;
        private readonly HashSet<string> warnedTraits = new HashSet<string>();

        public BreakCatalog()
        {
            var seenStates = new HashSet<MentalStateDef>();

            foreach (var def in DefDatabase<MentalBreakDef>.AllDefsListForReading)
            {
                breaks.Add(new BreakEntry(def));

                if (def.mentalState != null && seenStates.Add(def.mentalState))
                    states.Add(new StateSnapshot(def.mentalState));
            }

            breaks.Sort((a, b) => a.Def.intensity != b.Def.intensity
                ? b.Def.intensity.CompareTo(a.Def.intensity)
                : string.CompareOrdinal(a.Def.label ?? a.Def.defName, b.Def.label ?? b.Def.defName));

            thresholdStat = DefDatabase<StatDef>.GetNamedSilentFail(ThresholdStatName);

            if (thresholdStat == null)
                return;

            thresholdDefault = thresholdStat.defaultBaseValue;

            foreach (var thing in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (thing.statBases == null)
                    continue;

                foreach (var modifier in thing.statBases)
                {
                    if (modifier?.stat == thresholdStat)
                        raceThresholds.Add(new KeyValuePair<StatModifier, float>(modifier, modifier.value));
                }
            }
        }

        public IReadOnlyList<BreakEntry> Breaks => breaks;

        /// <summary>The vanilla minor break threshold for a pawn with no traits, or null if another mod removed the stat.</summary>
        public float? VanillaThreshold => thresholdStat == null ? (float?)null : thresholdDefault;

        public void Apply(BreakTuning tuning)
        {
            foreach (var entry in breaks)
            {
                entry.Def.baseCommonality = BreakMath.Commonality(entry.Def.defName, entry.Commonality, tuning);
                entry.Def.requiredTrait = RequiredTrait(entry, tuning);
            }

            foreach (var state in states)
                state.Apply(tuning);

            if (thresholdStat != null)
                thresholdStat.defaultBaseValue = BreakMath.Threshold(thresholdDefault, tuning);

            foreach (var pair in raceThresholds)
                pair.Key.value = BreakMath.Threshold(pair.Value, tuning);
        }

        private TraitDef? RequiredTrait(BreakEntry entry, BreakTuning tuning)
        {
            if (!tuning.TraitGates.TryGetValue(entry.Def.defName, out var gate))
                return entry.RequiredTrait;

            switch (gate)
            {
                case TraitGate.Anyone _:
                    return null;
                case TraitGate.Requires requires:
                    var trait = DefDatabase<TraitDef>.GetNamedSilentFail(requires.TraitDefName);

                    if (trait == null && warnedTraits.Add(requires.TraitDefName))
                        Log.Warning("[Reasonable Moods and Needs] Trait " + requires.TraitDefName + " does not exist, " + entry.Def.defName + " keeps its vanilla requirement.");

                    return trait ?? entry.RequiredTrait;
                default:
                    return entry.RequiredTrait;
            }
        }

        private sealed class StateSnapshot
        {
            private readonly MentalStateDef def;
            private readonly int minTicks;
            private readonly int maxTicks;
            private readonly float recoveryMtbDays;

            public StateSnapshot(MentalStateDef def)
            {
                this.def = def;
                minTicks = def.minTicksBeforeRecovery;
                maxTicks = def.maxTicksBeforeRecovery;
                recoveryMtbDays = def.recoveryMtbDays;
            }

            public void Apply(BreakTuning tuning)
            {
                def.minTicksBeforeRecovery = BreakMath.StateTicks(minTicks, tuning);
                def.maxTicksBeforeRecovery = BreakMath.StateTicks(maxTicks, tuning);
                def.recoveryMtbDays = BreakMath.RecoveryMtbDays(recoveryMtbDays, tuning);
            }
        }
    }
}
