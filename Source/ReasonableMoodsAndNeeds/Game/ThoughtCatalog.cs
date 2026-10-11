using System;
using System.Collections.Generic;
using RimWorld;
using ReasonableMoodsAndNeeds.Core;
using Verse;

namespace ReasonableMoodsAndNeeds.Game
{
    /// <summary>A ThoughtDef plus everything needed to put it back exactly as vanilla loaded it.</summary>
    public sealed class ThoughtEntry
    {
        public ThoughtEntry(ThoughtDef def, ThoughtFacts facts, string categoryId, bool isSocialMemory)
        {
            Def = def;
            Facts = facts;
            CategoryId = categoryId;
            IsSocialMemory = isSocialMemory;
            DefDescription = def.description;

            var stages = def.stages ?? new List<ThoughtStage>();

            StageDescriptions = new string?[stages.Count];
            StageVisible = new bool[stages.Count];

            for (var i = 0; i < stages.Count; i++)
            {
                StageDescriptions[i] = stages[i]?.description;
                StageVisible[i] = stages[i]?.visible ?? false;
            }

            var description = DefDescription ?? (StageDescriptions.Length > 0 ? StageDescriptions[0] : null) ?? "";

            Tooltip = facts.Label + "\n" + def.defName + " (" + facts.ModName + ")\n\n" + description;
        }

        /// <summary>Built once here because the thought list draws it for every visible row on every frame.</summary>
        public string Tooltip { get; }

        public ThoughtDef Def { get; }

        public ThoughtFacts Facts { get; }

        public string CategoryId { get; }

        public bool IsSocialMemory { get; }

        public string? DefDescription { get; }

        public string?[] StageDescriptions { get; }

        public bool[] StageVisible { get; }
    }

    /// <summary>
    /// Snapshot of every ThoughtDef taken once after all mods have loaded. Applying a profile rewrites the defs
    /// from this snapshot, so it is idempotent and never compounds. Cost is O(thoughts) per apply and zero per tick.
    /// </summary>
    public sealed class ThoughtCatalog
    {
        private readonly List<ThoughtEntry> entries = new List<ThoughtEntry>();
        private readonly List<ThoughtCategoryDef> categories;
        private readonly Dictionary<string, int> countByCategory = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> defNames = new HashSet<string>(StringComparer.Ordinal);

        public ThoughtCatalog()
        {
            categories = new List<ThoughtCategoryDef>(DefDatabase<ThoughtCategoryDef>.AllDefsListForReading);
            categories.Sort((a, b) => a.order.CompareTo(b.order));

            var rules = new List<CategoryRule>();

            foreach (var category in categories)
                rules.Add(category.ToRule());

            var classifier = new ThoughtClassifier(rules);
            var precepts = PreceptThoughts();

            foreach (var def in DefDatabase<ThoughtDef>.AllDefsListForReading)
            {
                if (def.stages == null || def.stages.Count == 0)
                    continue;

                var facts = FactsOf(def, precepts);
                var categoryId = classifier.Classify(facts);

                entries.Add(new ThoughtEntry(def, facts, categoryId, IsSocialMemory(def)));
                defNames.Add(def.defName);
                countByCategory[categoryId] = CountOf(categoryId) + 1;
            }

            entries.Sort((a, b) => string.Compare(a.Facts.Label, b.Facts.Label, StringComparison.OrdinalIgnoreCase));
        }

        public IReadOnlyList<ThoughtEntry> Entries => entries;

        public IReadOnlyList<ThoughtCategoryDef> Categories => categories;

        /// <summary>False for overrides saved while a since-removed mod was loaded.</summary>
        public bool Contains(string defName) => defNames.Contains(defName);

        public int CountOf(string categoryId) => countByCategory.TryGetValue(categoryId, out var n) ? n : 0;

        public string LabelOf(string categoryId)
        {
            foreach (var category in categories)
            {
                if (category.defName == categoryId)
                    return category.LabelCap;
            }

            return categoryId;
        }

        public void Apply(TuningProfile profile)
        {
            foreach (var entry in entries)
            {
                var tuning = ThoughtMath.Resolve(profile, entry.CategoryId, entry.Def.defName);

                ApplyStages(entry, tuning, profile.ShowVanillaInTooltips);
                entry.Def.durationDays = ThoughtMath.ApplyDuration(entry.Facts.DurationDays, tuning);
            }
        }

        private static void ApplyStages(ThoughtEntry entry, ResolvedTuning tuning, bool showVanilla)
        {
            var stages = entry.Def.stages;
            var annotateDef = showVanilla && stages.Count == 1 && string.IsNullOrEmpty(entry.StageDescriptions[0]);

            entry.Def.description = entry.DefDescription;

            for (var i = 0; i < stages.Count; i++)
            {
                var stage = stages[i];

                if (stage == null)
                    continue;

                var vanilla = entry.Facts.Stages[i];
                var tuned = ThoughtMath.ApplyStage(vanilla, tuning, entry.IsSocialMemory);
                var note = showVanilla ? VanillaNote(vanilla, tuned) : null;

                stage.baseMoodEffect = tuned.Mood;
                stage.baseOpinionOffset = tuned.Opinion;
                // A code-computed thought has no stage values to zero, so muting it would hide a thought that still applies.
                stage.visible = entry.StageVisible[i] && !(tuning.IsMuted && entry.Facts.IsTunable);
                stage.description = Annotate(entry.StageDescriptions[i], note);

                if (annotateDef)
                    entry.Def.description = Annotate(entry.DefDescription, note);
            }
        }

        private static string? Annotate(string? description, string? note)
        {
            if (note == null || string.IsNullOrEmpty(description))
                return description;

            return description + "\n\n" + note;
        }

        private static string? VanillaNote(StageValues vanilla, StageValues tuned)
        {
            var moodChanged = vanilla.Mood != tuned.Mood;
            var opinionChanged = vanilla.Opinion != tuned.Opinion;

            if (!moodChanged && !opinionChanged)
                return null;

            if (moodChanged && opinionChanged)
                return "RMN.VanillaMoodAndOpinion".Translate(vanilla.Mood.ToStringWithSign(), vanilla.Opinion.ToStringWithSign());

            return moodChanged
                ? "RMN.VanillaMood".Translate(vanilla.Mood.ToStringWithSign())
                : "RMN.VanillaOpinion".Translate(vanilla.Opinion.ToStringWithSign());
        }

        private static ThoughtFacts FactsOf(ThoughtDef def, HashSet<ThoughtDef> precepts)
        {
            var stages = new StageValues[def.stages.Count];

            for (var i = 0; i < stages.Length; i++)
            {
                var stage = def.stages[i];

                stages[i] = stage == null ? new StageValues(0f, 0f) : new StageValues(stage.baseMoodEffect, stage.baseOpinionOffset);
            }

            return new ThoughtFacts(
                def.defName,
                LabelOf(def),
                def.modContentPack?.Name ?? "?",
                def.fileName ?? "",
                def.IsSocial,
                def.IsMemory,
                precepts.Contains(def) || IsPreceptWorker(def),
                def.requiredTraits != null && def.requiredTraits.Count > 0,
                stages,
                def.durationDays);
        }

        private static string LabelOf(ThoughtDef def)
        {
            if (!string.IsNullOrEmpty(def.label))
                return def.label.CapitalizeFirst();

            foreach (var stage in def.stages)
            {
                if (stage != null && !string.IsNullOrEmpty(stage.label))
                    return stage.label.CapitalizeFirst();
            }

            return def.defName;
        }

        private static bool IsSocialMemory(ThoughtDef def) =>
            def.ThoughtClass != null && typeof(Thought_MemorySocial).IsAssignableFrom(def.ThoughtClass);

        private static bool IsPreceptWorker(ThoughtDef def) =>
            def.workerClass != null && typeof(ThoughtWorker_Precept).IsAssignableFrom(def.workerClass);

        /// <summary>Every thought a precept hands out. Read once; precepts are how Ideology expresses beliefs.</summary>
        private static HashSet<ThoughtDef> PreceptThoughts()
        {
            var found = new HashSet<ThoughtDef>();

            foreach (var precept in DefDatabase<PreceptDef>.AllDefsListForReading)
            {
                if (precept.comps == null)
                    continue;

                foreach (var comp in precept.comps)
                {
                    if (comp is PreceptComp_Thought withThought && withThought.thought != null)
                        found.Add(withThought.thought);
                }
            }

            return found;
        }
    }
}
