using System.Collections.Generic;
using ReasonableMoodsAndNeeds.Core;
using Xunit;

namespace ReasonableMoodsAndNeeds.Tests
{
    public class ProfileCodecTests
    {
        private static TuningProfile RoundTrip(TuningProfile profile)
        {
            var result = ProfileCodec.Decode(ProfileCodec.Encode(profile));

            Assert.Equal(0, result.Problems);

            return result.Profile;
        }

        [Theory]
        [InlineData(PresetId.Vanilla)]
        [InlineData(PresetId.Light)]
        [InlineData(PresetId.Reasonable)]
        [InlineData(PresetId.Relaxed)]
        public void Every_preset_survives_save_and_load_unchanged(PresetId id)
        {
            var original = Presets.Build(id);

            Assert.Equal(ProfileCodec.Encode(original), ProfileCodec.Encode(RoundTrip(original)));
        }

        [Fact]
        public void Overrides_trait_gates_and_seekers_survive_save_and_load()
        {
            var p = new TuningProfile { ShowVanillaInTooltips = false };

            p.Thoughts["Insulted"] = ThoughtOverride.Muted.Instance;
            p.Thoughts["AteRawFood"] = new ThoughtOverride.Custom(new Multipliers { MoodNegative = 0.2f, Duration = 0.5f });
            p.Breaks.TraitGates["Berserk"] = new TraitGate.Requires("Psychopath");
            p.Breaks.TraitGates["Jailbreaker"] = TraitGate.Anyone.Instance;
            p.Breaks.Commonality["Binging_Food"] = 0f;
            p.EditSeeker("Beauty").Fall = 0.25f;

            var back = RoundTrip(p);

            Assert.IsType<ThoughtOverride.Muted>(back.Thoughts["Insulted"]);
            Assert.Equal(0.2f, Assert.IsType<ThoughtOverride.Custom>(back.Thoughts["AteRawFood"]).Values.MoodNegative);
            Assert.Equal("Psychopath", Assert.IsType<TraitGate.Requires>(back.Breaks.TraitGates["Berserk"]).TraitDefName);
            Assert.IsType<TraitGate.Anyone>(back.Breaks.TraitGates["Jailbreaker"]);
            Assert.Equal(0f, back.Breaks.Commonality["Binging_Food"]);
            Assert.Equal(0.25f, back.Needs.Seekers["Beauty"].Fall);
            Assert.False(back.ShowVanillaInTooltips);
        }

        [Fact]
        public void A_custom_override_left_at_vanilla_values_stays_an_override()
        {
            var p = new TuningProfile();

            p.Thoughts["Pain"] = new ThoughtOverride.Custom(new Multipliers());

            Assert.IsType<ThoughtOverride.Custom>(RoundTrip(p).Thoughts["Pain"]);
        }

        [Fact]
        public void Vanilla_saves_as_just_the_version_line()
        {
            Assert.Equal("v=1", ProfileCodec.Encode(new TuningProfile()));
        }

        [Fact]
        public void Numbers_are_written_the_same_in_every_locale()
        {
            var previous = System.Globalization.CultureInfo.CurrentCulture;

            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
                var p = new TuningProfile();

                p.EditCategory(CategoryIds.Death).MoodNegative = 0.5f;

                Assert.Contains("cat.death.mn=0.5", ProfileCodec.Encode(p));
                Assert.Equal(0.5f, ProfileCodec.Decode("cat.death.mn=0.5").Profile.Categories[CategoryIds.Death].MoodNegative);
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previous;
            }
        }

        [Fact]
        public void Garbage_lines_are_counted_and_skipped_without_losing_the_good_ones()
        {
            var result = ProfileCodec.Decode("v=1\nnonsense\ncat.death.mn=abc\ncat.death.mp=0.5\nwhat.is.this=1\n\n# a comment");

            Assert.Equal(3, result.Problems);
            Assert.Equal(0.5f, result.Profile.Categories[CategoryIds.Death].MoodPositive);
            Assert.Equal(1f, result.Profile.Categories[CategoryIds.Death].MoodNegative);
        }

        [Fact]
        public void Out_of_range_values_are_clamped_and_reported()
        {
            var result = ProfileCodec.Decode("cat.death.mn=99\nneed.baseline=-500");

            Assert.Equal(2, result.Problems);
            Assert.Equal(Limits.Multiplier.Max, result.Profile.Categories[CategoryIds.Death].MoodNegative);
            Assert.Equal(Limits.MoodBaseline.Min, result.Profile.Needs.MoodBaselineShift);
        }

        [Fact]
        public void Windows_line_endings_from_the_clipboard_still_load()
        {
            var result = ProfileCodec.Decode("v=1\r\nbattle.on=1\r\nbattle.cats=food, rest\r\n");

            Assert.Equal(0, result.Problems);
            Assert.True(result.Profile.BattleFocus.Enabled);
            Assert.Equal(new HashSet<string> { "food", "rest" }, result.Profile.BattleFocus.Categories);
        }

        [Fact]
        public void The_thought_list_export_has_one_row_per_thought_plus_a_header()
        {
            var facts = new ThoughtFacts("AteRawFood", "ate raw food", "Core", "Thoughts_Memory_Eating.xml", false, true, false, false, new[] { new StageValues(-7f, 0f) }, 1f);
            var tsv = ProfileCodec.ThoughtListTsv(new[] { new KeyValuePair<ThoughtFacts, string>(facts, CategoryIds.Food) });
            var lines = tsv.TrimEnd('\n').Split('\n');

            Assert.Equal(2, lines.Length);
            Assert.Equal("AteRawFood\tfood\tCore\tThoughts_Memory_Eating.xml\t0\t1\t0\t0\t1\t-7/0", lines[1]);
        }
    }

    public class PresetTests
    {
        [Fact]
        public void Vanilla_changes_nothing()
        {
            var p = Presets.Build(PresetId.Vanilla);

            Assert.Empty(p.Categories);
            Assert.Empty(p.Thoughts);
            Assert.False(p.BattleFocus.Enabled);
            Assert.Equal(0f, p.Needs.MoodBaselineShift);
        }

        [Fact]
        public void Reasonable_softens_grief_and_limits_berserk_to_psychopaths()
        {
            var p = Presets.Build(PresetId.Reasonable);

            Assert.Equal(0.5f, p.Categories[CategoryIds.Death].MoodNegative);
            Assert.Equal("Psychopath", Assert.IsType<TraitGate.Requires>(p.Breaks.TraitGates[Presets.BerserkBreak]).TraitDefName);
            Assert.True(p.BattleFocus.Enabled);
        }

        [Theory]
        [InlineData(PresetId.Light)]
        [InlineData(PresetId.Reasonable)]
        [InlineData(PresetId.Relaxed)]
        public void No_preset_touches_ideology_titles_or_drugs(PresetId id)
        {
            var p = Presets.Build(id);

            Assert.False(p.Categories.ContainsKey(CategoryIds.Ideology));
            Assert.False(p.Categories.ContainsKey(CategoryIds.Titles));
            Assert.False(p.Categories.ContainsKey(CategoryIds.Drugs));
        }

        [Theory]
        [InlineData(PresetId.Light)]
        [InlineData(PresetId.Reasonable)]
        [InlineData(PresetId.Relaxed)]
        public void Every_preset_value_is_inside_the_slider_ranges(PresetId id)
        {
            Assert.Equal(0, ProfileCodec.Decode(ProfileCodec.Encode(Presets.Build(id))).Problems);
        }

        [Fact]
        public void Building_a_preset_twice_gives_independent_copies()
        {
            var a = Presets.Build(PresetId.Reasonable);
            var b = Presets.Build(PresetId.Reasonable);

            a.Categories[CategoryIds.Death].MoodNegative = 0.1f;

            Assert.Equal(0.5f, b.Categories[CategoryIds.Death].MoodNegative);
        }

        [Fact]
        public void A_cloned_profile_does_not_share_state_with_the_original()
        {
            var a = Presets.Build(PresetId.Reasonable);
            var b = a.Clone();

            b.Categories[CategoryIds.Death].MoodNegative = 0.1f;
            b.BattleFocus.Categories.Clear();
            b.Needs.Seekers[Presets.MoodNeed].Rise = 3f;

            Assert.Equal(0.5f, a.Categories[CategoryIds.Death].MoodNegative);
            Assert.NotEmpty(a.BattleFocus.Categories);
            Assert.Equal(1.25f, a.Needs.Seekers[Presets.MoodNeed].Rise);
        }
    }
}
