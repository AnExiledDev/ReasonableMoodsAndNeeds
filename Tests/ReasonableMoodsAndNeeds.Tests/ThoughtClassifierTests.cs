using ReasonableMoodsAndNeeds.Core;
using Xunit;

namespace ReasonableMoodsAndNeeds.Tests
{
    public class ThoughtClassifierTests
    {
        private static ThoughtFacts Thought(
            string defName,
            string file = "",
            bool social = false,
            bool precept = false,
            bool traitGated = false) =>
            new ThoughtFacts(defName, defName, "Core", file, social, true, precept, traitGated, new[] { new StageValues(-5f, 0f) }, 1f);

        private static ThoughtClassifier Classifier() => new ThoughtClassifier(new[]
        {
            new CategoryRule(CategoryIds.Justice, 90, thoughts: new[] { "KnowGuestOrganHarvested" }, defNameContains: new[] { "Execut", "Prisoner" }),
            new CategoryRule(CategoryIds.Death, 80, defNameContains: new[] { "Died", "Death" }, fileNameContains: new[] { "Thoughts_Memory_Death" }),
            new CategoryRule(CategoryIds.Food, 50, defNameContains: new[] { "Ate" }),
            new CategoryRule(CategoryIds.Social, 20, defNameContains: new[] { "Insult" }, matchSocial: true),
            new CategoryRule(CategoryIds.Traits, 15, matchTraitGated: true),
            new CategoryRule(CategoryIds.Ideology, 10, fileNameContains: new[] { "Precepts_" }, matchPrecepts: true),
            new CategoryRule(CategoryIds.Other, 0, isFallback: true),
        });

        [Fact]
        public void An_explicit_name_beats_every_pattern()
        {
            Assert.Equal(CategoryIds.Justice, Classifier().Classify(Thought("KnowGuestOrganHarvested", file: "Thoughts_Memory_Death.xml")));
        }

        [Fact]
        public void A_higher_priority_pattern_wins_when_two_match()
        {
            Assert.Equal(CategoryIds.Justice, Classifier().Classify(Thought("ExecutedPrisonerDied")));
        }

        [Fact]
        public void Patterns_ignore_case()
        {
            Assert.Equal(CategoryIds.Food, Classifier().Classify(Thought("modded_ate_bugs")));
        }

        [Fact]
        public void Precept_thoughts_go_to_ideology_even_when_the_name_looks_like_another_category()
        {
            Assert.Equal(CategoryIds.Ideology, Classifier().Classify(Thought("AteMeat_Abhorrent", precept: true)));
        }

        [Fact]
        public void The_source_file_sorts_a_thought_whose_name_says_nothing()
        {
            Assert.Equal(CategoryIds.Death, Classifier().Classify(Thought("SomeoneWeLost", file: "Thoughts_Memory_Death.xml")));
        }

        [Fact]
        public void A_name_pattern_beats_the_source_file()
        {
            Assert.Equal(CategoryIds.Food, Classifier().Classify(Thought("AteCorpse", file: "Thoughts_Memory_Misc.xml")));
        }

        [Fact]
        public void Unnamed_trait_thoughts_fall_back_to_traits()
        {
            Assert.Equal(CategoryIds.Traits, Classifier().Classify(Thought("Unknown", traitGated: true)));
        }

        [Fact]
        public void Unnamed_social_thoughts_fall_back_to_social()
        {
            Assert.Equal(CategoryIds.Social, Classifier().Classify(Thought("Unknown", social: true)));
        }

        [Fact]
        public void Anything_unrecognised_lands_in_other()
        {
            Assert.Equal(CategoryIds.Other, Classifier().Classify(Thought("Zzz")));
        }

        [Fact]
        public void Without_a_fallback_rule_other_is_still_used()
        {
            var classifier = new ThoughtClassifier(new[] { new CategoryRule(CategoryIds.Food, 1, defNameContains: new[] { "Ate" }) });

            Assert.Equal(CategoryIds.Other, classifier.Classify(Thought("Zzz")));
        }
    }
}
