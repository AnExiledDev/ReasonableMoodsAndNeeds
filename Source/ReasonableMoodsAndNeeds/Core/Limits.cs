namespace ReasonableMoodsAndNeeds.Core
{
    /// <summary>A closed slider range, shared by the UI and the profile import so both agree on what is legal.</summary>
    public readonly struct Range
    {
        public Range(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Min { get; }

        public float Max { get; }

        public bool Contains(float value) => value >= Min && value <= Max;

        public float Clamp(float value) => value < Min ? Min : value > Max ? Max : value;
    }

    public static class Limits
    {
        public static readonly Range Multiplier = new Range(0f, 3f);
        public static readonly Range Duration = new Range(0.1f, 3f);
        public static readonly Range BreakThreshold = new Range(0.25f, 1.5f);
        public static readonly Range StateDuration = new Range(0.1f, 2f);
        public static readonly Range Commonality = new Range(0f, 3f);
        public static readonly Range MoodBaseline = new Range(-30f, 30f);
        public static readonly Range SeekerRate = new Range(0.1f, 3f);
        public static readonly Range BattleMaxMood = new Range(1f, 30f);
        public static readonly Range BattleWindowHours = new Range(0.5f, 12f);
    }
}
