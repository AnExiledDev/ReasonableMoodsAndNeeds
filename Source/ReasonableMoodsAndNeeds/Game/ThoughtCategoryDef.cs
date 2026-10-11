using System.Collections.Generic;
using ReasonableMoodsAndNeeds.Core;
using Verse;

namespace ReasonableMoodsAndNeeds.Game
{
    /// <summary>
    /// A thought category, declared in XML so other mods can add their own categories
    /// or patch thoughts into ours without touching C#.
    /// </summary>
    public class ThoughtCategoryDef : Def
    {
        /// <summary>Position in the settings list.</summary>
        public int order;

        /// <summary>When two categories' patterns both match a thought, the higher priority wins.</summary>
        public int priority;

        public List<string> thoughts = new List<string>();
        public List<string> defNameContains = new List<string>();
        public List<string> fileNameContains = new List<string>();
        public bool matchPrecepts;
        public bool matchTraitGated;
        public bool matchSocial;
        public bool isFallback;

        public CategoryRule ToRule() => new CategoryRule(
            defName,
            priority,
            thoughts,
            defNameContains,
            fileNameContains,
            matchPrecepts,
            matchTraitGated,
            matchSocial,
            isFallback);
    }
}
