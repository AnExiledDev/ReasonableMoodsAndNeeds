using ReasonableMoodsAndNeeds.Core;
using Xunit;

namespace ReasonableMoodsAndNeeds.Tests
{
    public class ThoughtFactsTests
    {
        private static ThoughtFacts WithStages(params StageValues[] stages) =>
            new ThoughtFacts("Some", "some", "Core", "", false, true, false, false, stages, 1f);

        [Fact]
        public void A_thought_whose_stages_are_all_zero_is_computed_in_code_and_cannot_be_tuned()
        {
            Assert.False(WithStages(new StageValues(0f, 0f), new StageValues(0f, 0f)).IsTunable);
        }

        [Fact]
        public void One_stage_with_a_mood_value_makes_a_thought_tunable()
        {
            Assert.True(WithStages(new StageValues(0f, 0f), new StageValues(-3f, 0f)).IsTunable);
        }

        [Fact]
        public void An_opinion_only_thought_is_tunable()
        {
            Assert.True(WithStages(new StageValues(0f, 10f)).IsTunable);
        }
    }
}
