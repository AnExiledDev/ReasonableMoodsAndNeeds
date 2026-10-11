using System;

namespace ReasonableMoodsAndNeeds.Core
{
    /// <summary>What a thought ends up with after categories and overrides are resolved.</summary>
    public readonly struct ResolvedTuning
    {
        private ResolvedTuning(bool isMuted, Multipliers values)
        {
            IsMuted = isMuted;
            Values = values;
        }

        public bool IsMuted { get; }

        /// <summary>The multipliers in force. Meaningless when <see cref="IsMuted"/> is true.</summary>
        public Multipliers Values { get; }

        public static ResolvedTuning Muted => new ResolvedTuning(true, Multipliers.Identity);

        public static ResolvedTuning Scaled(Multipliers values) => new ResolvedTuning(false, values);
    }

    public static class ThoughtMath
    {
        /// <summary>Smallest opinion a social memory can keep. Vanilla discards social memories whose opinion is exactly 0.</summary>
        public const float SocialOpinionFloor = 0.01f;

        /// <summary>Shortest duration a scaled memory can have, so a memory never expires the tick it is gained.</summary>
        public const float MinDurationDays = 0.01f;

        public static ResolvedTuning Resolve(TuningProfile profile, string categoryId, string defName)
        {
            if (profile.Thoughts.TryGetValue(defName, out var thoughtOverride))
            {
                switch (thoughtOverride)
                {
                    case ThoughtOverride.Muted _:
                        return ResolvedTuning.Muted;
                    case ThoughtOverride.Custom custom:
                        return ResolvedTuning.Scaled(custom.Values);
                    default:
                        throw new InvalidOperationException("Unknown thought override " + thoughtOverride.GetType().Name);
                }
            }

            return ResolvedTuning.Scaled(profile.CategoryOrIdentity(categoryId));
        }

        public static float Scale(float value, float negativeMultiplier, float positiveMultiplier) =>
            value < 0f ? value * negativeMultiplier : value * positiveMultiplier;

        /// <param name="isSocialMemory">True for Thought_MemorySocial and subclasses, whose zero-opinion instances vanilla throws away.</param>
        public static StageValues ApplyStage(StageValues vanilla, ResolvedTuning tuning, bool isSocialMemory)
        {
            if (tuning.IsMuted)
                return new StageValues(0f, 0f);

            var values = tuning.Values;
            var mood = Scale(vanilla.Mood, values.MoodNegative, values.MoodPositive);
            var opinion = Scale(vanilla.Opinion, values.OpinionNegative, values.OpinionPositive);
            var wouldBeDiscarded = isSocialMemory && vanilla.Opinion != 0f && opinion == 0f && mood != 0f;

            if (wouldBeDiscarded)
                opinion = Math.Sign(vanilla.Opinion) * SocialOpinionFloor;

            return new StageValues(mood, opinion);
        }

        /// <summary>Scales a memory's duration. Zero or negative durations (situational thoughts) are left alone.</summary>
        public static float ApplyDuration(float vanillaDays, ResolvedTuning tuning)
        {
            if (vanillaDays <= 0f || tuning.IsMuted)
                return vanillaDays;

            return Math.Max(vanillaDays * tuning.Values.Duration, MinDurationDays);
        }
    }

    public static class BreakMath
    {
        public static float Threshold(float vanilla, BreakTuning tuning) => vanilla * tuning.ThresholdMultiplier;

        public static float Commonality(string breakDefName, float vanilla, BreakTuning tuning) =>
            tuning.Commonality.TryGetValue(breakDefName, out var multiplier) ? vanilla * multiplier : vanilla;

        public static int StateTicks(int vanillaTicks, BreakTuning tuning)
        {
            if (vanillaTicks <= 0)
                return vanillaTicks;

            return Math.Max(1, (int)Math.Round(vanillaTicks * tuning.StateDurationMultiplier));
        }

        public static float RecoveryMtbDays(float vanillaDays, BreakTuning tuning) =>
            vanillaDays <= 0f ? vanillaDays : vanillaDays * tuning.StateDurationMultiplier;
    }

    public static class NeedMath
    {
        /// <summary>The mood need's base level (0..1) after shifting by whole mood points.</summary>
        public static float MoodBaseLevel(float vanillaBaseLevel, float shiftPoints)
        {
            var shifted = vanillaBaseLevel + shiftPoints / 100f;

            return shifted < 0f ? 0f : shifted > 1f ? 1f : shifted;
        }

        public static SeekerRates RatesFor(string needDefName, NeedTuning tuning) =>
            tuning.Seekers.TryGetValue(needDefName, out var rates) ? rates : new SeekerRates();
    }

    public static class BattleFocus
    {
        public const int TicksPerHour = 2500;

        /// <summary>Whether battle focus may ever ignore this thought. Precomputed per def; social memories never qualify.</summary>
        public static bool IsEligible(ThoughtFacts thought, string categoryId, BattleFocusSettings settings) =>
            settings.Enabled && thought.IsMemory && !thought.IsSocial && settings.Categories.Contains(categoryId);

        /// <summary>The per-event decision, once the pawn is known to be in combat.</summary>
        public static bool ShouldIgnore(float moodOffset, BattleFocusSettings settings) =>
            moodOffset < 0f && -moodOffset <= settings.MaxMoodMagnitude;

        public static int WindowTicks(BattleFocusSettings settings) =>
            Math.Max(1, (int)(settings.CombatWindowHours * TicksPerHour));
    }
}
