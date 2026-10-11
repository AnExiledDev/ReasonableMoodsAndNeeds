using System;

namespace ReasonableMoodsAndNeeds.Core
{
    public enum PresetId
    {
        Vanilla,
        Light,
        Reasonable,
        Relaxed,
    }

    /// <summary>
    /// The four starting points. Reasonable follows the 2017 mod: grief, guilt and gore still hurt,
    /// they just stop wrecking the colony. Ideology, titles, drugs and anything unrecognised stay vanilla
    /// in every preset, because those are deliberate gameplay systems rather than noise.
    /// </summary>
    public static class Presets
    {
        public const string BerserkBreak = "Berserk";
        public const string PsychopathTrait = "Psychopath";
        public const string MoodNeed = "Mood";
        public const string BeautyNeed = "Beauty";
        public const string ComfortNeed = "Comfort";

        public static readonly PresetId[] All = { PresetId.Vanilla, PresetId.Light, PresetId.Reasonable, PresetId.Relaxed };

        public static TuningProfile Build(PresetId id)
        {
            switch (id)
            {
                case PresetId.Vanilla:
                    return new TuningProfile();
                case PresetId.Light:
                    return Light();
                case PresetId.Reasonable:
                    return Reasonable();
                case PresetId.Relaxed:
                    return Relaxed();
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown preset");
            }
        }

        private static TuningProfile Light()
        {
            var p = new TuningProfile();

            Negative(p, CategoryIds.Death, 0.75f, duration: 0.75f);
            Negative(p, CategoryIds.Justice, 0.8f, duration: 0.75f);
            Negative(p, CategoryIds.Corpses, 0.85f, duration: 0.75f);
            Negative(p, CategoryIds.Food, 0.75f);
            Negative(p, CategoryIds.Rest, 0.85f);
            Negative(p, CategoryIds.Needs, 0.75f);
            Negative(p, CategoryIds.Environment, 0.85f);
            Social(p, CategoryIds.Social, 0.75f, duration: 0.75f);
            Social(p, CategoryIds.Romance, 0.75f, duration: 0.75f);
            Negative(p, CategoryIds.Health, 0.8f);
            Negative(p, CategoryIds.Traits, 0.75f);
            Negative(p, CategoryIds.Children, 0.85f);
            p.Breaks.StateDurationMultiplier = 0.75f;
            p.Needs.MoodBaselineShift = 5f;

            return p;
        }

        private static TuningProfile Reasonable()
        {
            var p = new TuningProfile();

            Negative(p, CategoryIds.Death, 0.5f, duration: 0.4f);
            Negative(p, CategoryIds.Justice, 0.6f, duration: 0.5f);
            Negative(p, CategoryIds.Corpses, 0.75f, duration: 0.5f);
            Negative(p, CategoryIds.Food, 0.5f);
            Negative(p, CategoryIds.Rest, 0.75f);
            Negative(p, CategoryIds.Needs, 0.5f);
            Negative(p, CategoryIds.Environment, 0.75f);
            Social(p, CategoryIds.Social, 0.5f, duration: 0.5f);
            Social(p, CategoryIds.Romance, 0.5f, duration: 0.35f);
            Negative(p, CategoryIds.Health, 0.6f);
            Negative(p, CategoryIds.Traits, 0.5f);
            Both(p, CategoryIds.Psychic, 0.5f);
            p.EditCategory(CategoryIds.Gatherings).MoodPositive = 1.5f;
            Negative(p, CategoryIds.Children, 0.75f);

            p.Breaks.TraitGates[BerserkBreak] = new TraitGate.Requires(PsychopathTrait);
            p.Breaks.StateDurationMultiplier = 0.5f;
            p.Needs.MoodBaselineShift = 10f;
            Seeker(p, MoodNeed, rise: 1.25f, fall: 0.5f);
            Seeker(p, BeautyNeed, rise: 1f, fall: 0.25f);
            Seeker(p, ComfortNeed, rise: 1f, fall: 0.5f);
            BattleFocusOn(p, 5f);

            return p;
        }

        private static TuningProfile Relaxed()
        {
            var p = new TuningProfile();

            Negative(p, CategoryIds.Death, 0.35f, duration: 0.3f);
            Negative(p, CategoryIds.Justice, 0.35f, duration: 0.3f);
            Negative(p, CategoryIds.Corpses, 0.4f, duration: 0.3f);
            Negative(p, CategoryIds.Food, 0.35f);
            Negative(p, CategoryIds.Rest, 0.5f);
            Negative(p, CategoryIds.Needs, 0.35f);
            Negative(p, CategoryIds.Environment, 0.5f);
            Social(p, CategoryIds.Social, 0.35f, duration: 0.35f);
            Social(p, CategoryIds.Romance, 0.35f, duration: 0.25f);
            Negative(p, CategoryIds.Health, 0.4f);
            Negative(p, CategoryIds.Traits, 0.35f);
            Both(p, CategoryIds.Psychic, 0.35f);
            p.EditCategory(CategoryIds.Gatherings).MoodPositive = 1.75f;
            Negative(p, CategoryIds.Children, 0.5f);

            p.Breaks.TraitGates[BerserkBreak] = new TraitGate.Requires(PsychopathTrait);
            p.Breaks.ThresholdMultiplier = 0.8f;
            p.Breaks.StateDurationMultiplier = 0.35f;
            p.Needs.MoodBaselineShift = 15f;
            Seeker(p, MoodNeed, rise: 1.5f, fall: 0.35f);
            Seeker(p, BeautyNeed, rise: 1f, fall: 0.2f);
            Seeker(p, ComfortNeed, rise: 1f, fall: 0.35f);
            BattleFocusOn(p, 8f);

            return p;
        }

        private static void Negative(TuningProfile p, string category, float mood, float duration = 1f)
        {
            var m = p.EditCategory(category);

            m.MoodNegative = mood;
            m.Duration = duration;
        }

        private static void Social(TuningProfile p, string category, float scale, float duration)
        {
            var m = p.EditCategory(category);

            m.MoodNegative = scale;
            m.OpinionNegative = scale;
            m.Duration = duration;
        }

        private static void Both(TuningProfile p, string category, float scale)
        {
            var m = p.EditCategory(category);

            m.MoodNegative = scale;
            m.MoodPositive = scale;
        }

        private static void Seeker(TuningProfile p, string need, float rise, float fall)
        {
            var rates = p.EditSeeker(need);

            rates.Rise = rise;
            rates.Fall = fall;
        }

        private static void BattleFocusOn(TuningProfile p, float maxMood)
        {
            p.BattleFocus.Enabled = true;
            p.BattleFocus.MaxMoodMagnitude = maxMood;
            p.BattleFocus.Categories.Add(CategoryIds.Food);
            p.BattleFocus.Categories.Add(CategoryIds.Rest);
            p.BattleFocus.Categories.Add(CategoryIds.Environment);
            p.BattleFocus.Categories.Add(CategoryIds.Corpses);
            p.BattleFocus.Categories.Add(CategoryIds.Social);
        }
    }
}
