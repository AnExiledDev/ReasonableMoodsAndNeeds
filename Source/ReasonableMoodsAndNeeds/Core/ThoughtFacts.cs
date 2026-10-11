using System.Collections.Generic;

namespace ReasonableMoodsAndNeeds.Core
{
    /// <summary>The vanilla values of one thought stage, as loaded from XML.</summary>
    public readonly struct StageValues
    {
        public StageValues(float mood, float opinion)
        {
            Mood = mood;
            Opinion = opinion;
        }

        public float Mood { get; }

        public float Opinion { get; }
    }

    /// <summary>
    /// Everything the classifier and the tuning maths need to know about a ThoughtDef,
    /// captured once from the untouched def so the core never touches game types.
    /// </summary>
    public sealed class ThoughtFacts
    {
        public ThoughtFacts(
            string defName,
            string label,
            string modName,
            string fileName,
            bool isSocial,
            bool isMemory,
            bool isPrecept,
            bool hasRequiredTraits,
            IReadOnlyList<StageValues> stages,
            float durationDays)
        {
            DefName = defName;
            Label = label;
            ModName = modName;
            FileName = fileName;
            IsSocial = isSocial;
            IsMemory = isMemory;
            IsPrecept = isPrecept;
            HasRequiredTraits = hasRequiredTraits;
            Stages = stages;
            DurationDays = durationDays;
        }

        public string DefName { get; }

        public string Label { get; }

        public string ModName { get; }

        /// <summary>The XML file the def came from, or an empty string when the game did not record one.</summary>
        public string FileName { get; }

        public bool IsSocial { get; }

        public bool IsMemory { get; }

        public bool IsPrecept { get; }

        public bool HasRequiredTraits { get; }

        public IReadOnlyList<StageValues> Stages { get; }

        public float DurationDays { get; }

        public bool HasNegativeMood
        {
            get
            {
                for (var i = 0; i < Stages.Count; i++)
                {
                    if (Stages[i].Mood < 0f || Stages[i].Opinion < 0f)
                        return true;
                }

                return false;
            }
        }

        public bool HasPositiveMood
        {
            get
            {
                for (var i = 0; i < Stages.Count; i++)
                {
                    if (Stages[i].Mood > 0f || Stages[i].Opinion > 0f)
                        return true;
                }

                return false;
            }
        }
    }
}
