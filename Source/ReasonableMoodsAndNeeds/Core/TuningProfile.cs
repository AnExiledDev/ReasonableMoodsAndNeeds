using System;
using System.Collections.Generic;

namespace ReasonableMoodsAndNeeds.Core
{
    /// <summary>Scales applied to a thought. 1 everywhere means vanilla.</summary>
    public sealed class Multipliers
    {
        public float MoodNegative = 1f;
        public float MoodPositive = 1f;
        public float OpinionNegative = 1f;
        public float OpinionPositive = 1f;
        public float Duration = 1f;

        public static Multipliers Identity => new Multipliers();

        public bool IsIdentity =>
            MoodNegative == 1f && MoodPositive == 1f && OpinionNegative == 1f && OpinionPositive == 1f && Duration == 1f;

        public Multipliers Clone() => new Multipliers
        {
            MoodNegative = MoodNegative,
            MoodPositive = MoodPositive,
            OpinionNegative = OpinionNegative,
            OpinionPositive = OpinionPositive,
            Duration = Duration,
        };

        public bool SameAs(Multipliers other) =>
            MoodNegative == other.MoodNegative
            && MoodPositive == other.MoodPositive
            && OpinionNegative == other.OpinionNegative
            && OpinionPositive == other.OpinionPositive
            && Duration == other.Duration;
    }

    /// <summary>A per-thought override. No entry at all means the thought follows its category.</summary>
    public abstract class ThoughtOverride
    {
        private ThoughtOverride()
        {
        }

        public abstract ThoughtOverride Clone();

        /// <summary>The thought uses its own multipliers instead of its category's.</summary>
        public sealed class Custom : ThoughtOverride
        {
            public Custom(Multipliers values)
            {
                Values = values;
            }

            public Multipliers Values { get; }

            public override ThoughtOverride Clone() => new Custom(Values.Clone());
        }

        /// <summary>The thought gives no mood and no opinion, and is hidden from the needs tab.</summary>
        public sealed class Muted : ThoughtOverride
        {
            public static readonly Muted Instance = new Muted();

            private Muted()
            {
            }

            public override ThoughtOverride Clone() => Instance;
        }
    }

    /// <summary>A mental break's required trait. No entry means vanilla's own requirement.</summary>
    public abstract class TraitGate
    {
        private TraitGate()
        {
        }

        /// <summary>Anyone can have the break, even if vanilla gates it behind a trait.</summary>
        public sealed class Anyone : TraitGate
        {
            public static readonly Anyone Instance = new Anyone();

            private Anyone()
            {
            }
        }

        /// <summary>Only pawns with this trait can have the break.</summary>
        public sealed class Requires : TraitGate
        {
            public Requires(string traitDefName)
            {
                TraitDefName = traitDefName;
            }

            public string TraitDefName { get; }
        }
    }

    public sealed class SeekerRates
    {
        public float Rise = 1f;
        public float Fall = 1f;

        public bool IsIdentity => Rise == 1f && Fall == 1f;

        public SeekerRates Clone() => new SeekerRates { Rise = Rise, Fall = Fall };
    }

    public sealed class BreakTuning
    {
        /// <summary>Scales the mental break threshold stat. Lower means pawns break at a lower mood.</summary>
        public float ThresholdMultiplier = 1f;

        /// <summary>Scales how long mental states caused by breaks last.</summary>
        public float StateDurationMultiplier = 1f;

        /// <summary>Per MentalBreakDef commonality multiplier. 0 disables the break.</summary>
        public Dictionary<string, float> Commonality = new Dictionary<string, float>(StringComparer.Ordinal);

        public Dictionary<string, TraitGate> TraitGates = new Dictionary<string, TraitGate>(StringComparer.Ordinal);

        public BreakTuning Clone() => new BreakTuning
        {
            ThresholdMultiplier = ThresholdMultiplier,
            StateDurationMultiplier = StateDurationMultiplier,
            Commonality = new Dictionary<string, float>(Commonality, StringComparer.Ordinal),
            TraitGates = new Dictionary<string, TraitGate>(TraitGates, StringComparer.Ordinal),
        };
    }

    public sealed class NeedTuning
    {
        /// <summary>Mood points added to every colonist's base mood (vanilla base is 50).</summary>
        public float MoodBaselineShift;

        /// <summary>Per NeedDef rate multipliers for needs that drift towards a target (mood, beauty, comfort).</summary>
        public Dictionary<string, SeekerRates> Seekers = new Dictionary<string, SeekerRates>(StringComparer.Ordinal);

        public NeedTuning Clone()
        {
            var copy = new NeedTuning { MoodBaselineShift = MoodBaselineShift };

            foreach (var pair in Seekers)
                copy.Seekers[pair.Key] = pair.Value.Clone();

            return copy;
        }
    }

    public sealed class BattleFocusSettings
    {
        public bool Enabled;

        /// <summary>Only negative memories this size or smaller are ignored.</summary>
        public float MaxMoodMagnitude = 5f;

        /// <summary>How long after fighting a pawn still counts as in combat.</summary>
        public float CombatWindowHours = 2f;

        public HashSet<string> Categories = new HashSet<string>(StringComparer.Ordinal);

        public BattleFocusSettings Clone() => new BattleFocusSettings
        {
            Enabled = Enabled,
            MaxMoodMagnitude = MaxMoodMagnitude,
            CombatWindowHours = CombatWindowHours,
            Categories = new HashSet<string>(Categories, StringComparer.Ordinal),
        };
    }

    /// <summary>The whole user configuration. Absent entries always mean vanilla.</summary>
    public sealed class TuningProfile
    {
        public Dictionary<string, Multipliers> Categories = new Dictionary<string, Multipliers>(StringComparer.Ordinal);
        public Dictionary<string, ThoughtOverride> Thoughts = new Dictionary<string, ThoughtOverride>(StringComparer.Ordinal);
        public BreakTuning Breaks = new BreakTuning();
        public NeedTuning Needs = new NeedTuning();
        public BattleFocusSettings BattleFocus = new BattleFocusSettings();
        public bool ShowVanillaInTooltips = true;

        public Multipliers CategoryOrIdentity(string categoryId) =>
            Categories.TryGetValue(categoryId, out var found) ? found : Multipliers.Identity;

        /// <summary>The category's multipliers, created on first write so the UI can edit them in place.</summary>
        public Multipliers EditCategory(string categoryId)
        {
            if (!Categories.TryGetValue(categoryId, out var found))
            {
                found = new Multipliers();
                Categories[categoryId] = found;
            }

            return found;
        }

        public SeekerRates EditSeeker(string needDefName)
        {
            if (!Needs.Seekers.TryGetValue(needDefName, out var found))
            {
                found = new SeekerRates();
                Needs.Seekers[needDefName] = found;
            }

            return found;
        }

        public TuningProfile Clone()
        {
            var copy = new TuningProfile
            {
                Breaks = Breaks.Clone(),
                Needs = Needs.Clone(),
                BattleFocus = BattleFocus.Clone(),
                ShowVanillaInTooltips = ShowVanillaInTooltips,
            };

            foreach (var pair in Categories)
                copy.Categories[pair.Key] = pair.Value.Clone();

            foreach (var pair in Thoughts)
                copy.Thoughts[pair.Key] = pair.Value.Clone();

            return copy;
        }
    }
}
