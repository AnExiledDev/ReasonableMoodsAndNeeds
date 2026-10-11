using ReasonableMoodsAndNeeds.Core;
using Xunit;

namespace ReasonableMoodsAndNeeds.Tests
{
    public class ThoughtMathTests
    {
        private static TuningProfile ProfileWithCategory(string id, Multipliers m)
        {
            var p = new TuningProfile();

            p.Categories[id] = m;

            return p;
        }

        [Fact]
        public void Negative_moods_use_the_negative_multiplier_and_positive_moods_the_positive_one()
        {
            var tuning = ResolvedTuning.Scaled(new Multipliers { MoodNegative = 0.5f, MoodPositive = 2f });

            Assert.Equal(-5f, ThoughtMath.ApplyStage(new StageValues(-10f, 0f), tuning, false).Mood);
            Assert.Equal(20f, ThoughtMath.ApplyStage(new StageValues(10f, 0f), tuning, false).Mood);
        }

        [Fact]
        public void Opinion_is_scaled_independently_of_mood()
        {
            var tuning = ResolvedTuning.Scaled(new Multipliers { OpinionNegative = 0.25f });
            var result = ThoughtMath.ApplyStage(new StageValues(-8f, -20f), tuning, false);

            Assert.Equal(-8f, result.Mood);
            Assert.Equal(-5f, result.Opinion);
        }

        [Fact]
        public void A_thought_without_an_override_follows_its_category()
        {
            var p = ProfileWithCategory(CategoryIds.Death, new Multipliers { MoodNegative = 0.5f });

            var resolved = ThoughtMath.Resolve(p, CategoryIds.Death, "KnowColonistDied");

            Assert.False(resolved.IsMuted);
            Assert.Equal(0.5f, resolved.Values.MoodNegative);
        }

        [Fact]
        public void A_custom_override_wins_over_its_category()
        {
            var p = ProfileWithCategory(CategoryIds.Death, new Multipliers { MoodNegative = 0.5f });

            p.Thoughts["KnowColonistDied"] = new ThoughtOverride.Custom(new Multipliers { MoodNegative = 0.1f });

            Assert.Equal(0.1f, ThoughtMath.Resolve(p, CategoryIds.Death, "KnowColonistDied").Values.MoodNegative);
            Assert.Equal(0.5f, ThoughtMath.Resolve(p, CategoryIds.Death, "WitnessedDeathAlly").Values.MoodNegative);
        }

        [Fact]
        public void A_muted_thought_gives_no_mood_and_no_opinion()
        {
            var p = new TuningProfile();

            p.Thoughts["Insulted"] = ThoughtOverride.Muted.Instance;

            var result = ThoughtMath.ApplyStage(new StageValues(-5f, -15f), ThoughtMath.Resolve(p, CategoryIds.Social, "Insulted"), true);

            Assert.Equal(0f, result.Mood);
            Assert.Equal(0f, result.Opinion);
        }

        [Fact]
        public void An_unknown_category_is_vanilla()
        {
            var resolved = ThoughtMath.Resolve(new TuningProfile(), "nonexistent", "Anything");

            Assert.True(resolved.Values.IsIdentity);
        }

        [Fact]
        public void A_social_memory_keeps_a_token_opinion_so_its_mood_is_not_thrown_away()
        {
            var tuning = ResolvedTuning.Scaled(new Multipliers { OpinionNegative = 0f, OpinionPositive = 0f });

            var negative = ThoughtMath.ApplyStage(new StageValues(-5f, -15f), tuning, true);
            var positive = ThoughtMath.ApplyStage(new StageValues(5f, 15f), tuning, true);

            Assert.Equal(-ThoughtMath.SocialOpinionFloor, negative.Opinion);
            Assert.Equal(ThoughtMath.SocialOpinionFloor, positive.Opinion);
            Assert.Equal(-5f, negative.Mood);
        }

        [Fact]
        public void A_social_memory_with_no_mood_left_is_allowed_to_disappear()
        {
            var tuning = ResolvedTuning.Scaled(new Multipliers { OpinionNegative = 0f, MoodNegative = 0f });

            Assert.Equal(0f, ThoughtMath.ApplyStage(new StageValues(-5f, -15f), tuning, true).Opinion);
        }

        [Fact]
        public void The_token_opinion_only_applies_to_social_memories()
        {
            var tuning = ResolvedTuning.Scaled(new Multipliers { OpinionNegative = 0f });

            Assert.Equal(0f, ThoughtMath.ApplyStage(new StageValues(-5f, -15f), tuning, false).Opinion);
        }

        [Fact]
        public void Memory_duration_scales_but_never_reaches_zero()
        {
            var tuning = ResolvedTuning.Scaled(new Multipliers { Duration = 0.5f });
            var tiny = ResolvedTuning.Scaled(new Multipliers { Duration = 0.0001f });

            Assert.Equal(3f, ThoughtMath.ApplyDuration(6f, tuning));
            Assert.Equal(ThoughtMath.MinDurationDays, ThoughtMath.ApplyDuration(1f, tiny));
        }

        [Fact]
        public void Situational_thoughts_with_no_duration_keep_it()
        {
            var tuning = ResolvedTuning.Scaled(new Multipliers { Duration = 0.5f });

            Assert.Equal(0f, ThoughtMath.ApplyDuration(0f, tuning));
            Assert.Equal(-1f, ThoughtMath.ApplyDuration(-1f, tuning));
        }
    }

    public class BreakAndNeedMathTests
    {
        [Fact]
        public void Mood_baseline_shifts_by_whole_mood_points_and_stays_within_the_bar()
        {
            Assert.Equal(0.6f, NeedMath.MoodBaseLevel(0.5f, 10f), 4);
            Assert.Equal(1f, NeedMath.MoodBaseLevel(0.95f, 20f));
            Assert.Equal(0f, NeedMath.MoodBaseLevel(0.1f, -30f));
        }

        [Fact]
        public void A_break_with_zero_commonality_can_never_be_picked()
        {
            var breaks = new BreakTuning();

            breaks.Commonality["Binging_Food"] = 0f;

            Assert.Equal(0f, BreakMath.Commonality("Binging_Food", 2f, breaks));
            Assert.Equal(2f, BreakMath.Commonality("Wander_Sad", 2f, breaks));
        }

        [Fact]
        public void Mental_states_get_shorter_but_never_instant()
        {
            var breaks = new BreakTuning { StateDurationMultiplier = 0.5f };
            var extreme = new BreakTuning { StateDurationMultiplier = 0.000001f };

            Assert.Equal(5000, BreakMath.StateTicks(10000, breaks));
            Assert.Equal(1, BreakMath.StateTicks(10000, extreme));
            Assert.Equal(0, BreakMath.StateTicks(0, breaks));
            Assert.Equal(0.5f, BreakMath.RecoveryMtbDays(1f, breaks));
        }

        [Fact]
        public void The_break_threshold_scales_with_its_multiplier()
        {
            Assert.Equal(0.28f, BreakMath.Threshold(0.35f, new BreakTuning { ThresholdMultiplier = 0.8f }), 4);
        }
    }

    public class BattleFocusTests
    {
        private static ThoughtFacts Memory(bool social) =>
            new ThoughtFacts("AteRawFood", "ate raw food", "Core", "Thoughts_Memory_Eating.xml", social, true, false, false, new[] { new StageValues(-7f, 0f) }, 1f);

        private static BattleFocusSettings Enabled()
        {
            var s = new BattleFocusSettings { Enabled = true, MaxMoodMagnitude = 5f };

            s.Categories.Add(CategoryIds.Food);

            return s;
        }

        [Fact]
        public void Only_memories_in_chosen_categories_are_eligible()
        {
            var s = Enabled();

            Assert.True(BattleFocus.IsEligible(Memory(false), CategoryIds.Food, s));
            Assert.False(BattleFocus.IsEligible(Memory(false), CategoryIds.Death, s));
        }

        [Fact]
        public void Social_memories_are_never_ignored_because_they_change_relationships()
        {
            Assert.False(BattleFocus.IsEligible(Memory(true), CategoryIds.Food, Enabled()));
        }

        [Fact]
        public void Nothing_is_eligible_when_battle_focus_is_off()
        {
            var s = Enabled();

            s.Enabled = false;

            Assert.False(BattleFocus.IsEligible(Memory(false), CategoryIds.Food, s));
        }

        [Fact]
        public void Only_small_negative_memories_are_ignored()
        {
            var s = Enabled();

            Assert.True(BattleFocus.ShouldIgnore(-5f, s));
            Assert.False(BattleFocus.ShouldIgnore(-5.5f, s));
            Assert.False(BattleFocus.ShouldIgnore(3f, s));
            Assert.False(BattleFocus.ShouldIgnore(0f, s));
        }

        [Fact]
        public void The_combat_window_is_measured_in_game_hours()
        {
            Assert.Equal(5000, BattleFocus.WindowTicks(new BattleFocusSettings { CombatWindowHours = 2f }));
        }
    }
}
