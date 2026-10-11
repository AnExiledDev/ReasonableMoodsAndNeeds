using System.Collections.Generic;
using RimWorld;
using ReasonableMoodsAndNeeds.Core;
using Verse;

namespace ReasonableMoodsAndNeeds.Game
{
    public sealed class SeekerEntry
    {
        public SeekerEntry(NeedDef def)
        {
            Def = def;
            Rise = def.seekerRisePerHour;
            Fall = def.seekerFallPerHour;
        }

        public NeedDef Def { get; }

        public float Rise { get; }

        public float Fall { get; }
    }

    /// <summary>The mood need's base level and every "seeker" need's drift rates, snapshotted once.</summary>
    public sealed class NeedCatalog
    {
        private readonly List<SeekerEntry> seekers = new List<SeekerEntry>();
        private readonly NeedDef? mood;
        private readonly float moodBaseLevel;

        public NeedCatalog()
        {
            foreach (var def in DefDatabase<NeedDef>.AllDefsListForReading)
            {
                if (def.needClass != null && typeof(Need_Seeker).IsAssignableFrom(def.needClass))
                    seekers.Add(new SeekerEntry(def));
            }

            seekers.Sort((a, b) => string.CompareOrdinal(a.Def.label ?? a.Def.defName, b.Def.label ?? b.Def.defName));
            mood = DefDatabase<NeedDef>.GetNamedSilentFail(Presets.MoodNeed);
            moodBaseLevel = mood?.baseLevel ?? 0.5f;
        }

        public IReadOnlyList<SeekerEntry> Seekers => seekers;

        public float VanillaMoodBaseLevel => moodBaseLevel;

        public void Apply(NeedTuning tuning)
        {
            if (mood != null)
                mood.baseLevel = NeedMath.MoodBaseLevel(moodBaseLevel, tuning.MoodBaselineShift);

            foreach (var entry in seekers)
            {
                var rates = NeedMath.RatesFor(entry.Def.defName, tuning);

                entry.Def.seekerRisePerHour = entry.Rise * rates.Rise;
                entry.Def.seekerFallPerHour = entry.Fall * rates.Fall;
            }
        }
    }
}
