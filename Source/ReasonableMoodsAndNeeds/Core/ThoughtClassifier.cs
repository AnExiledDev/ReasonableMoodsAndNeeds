using System;
using System.Collections.Generic;

namespace ReasonableMoodsAndNeeds.Core
{
    /// <summary>One category's matching rules, read from a ThoughtCategoryDef.</summary>
    public sealed class CategoryRule
    {
        public CategoryRule(
            string id,
            int priority,
            IEnumerable<string>? thoughts = null,
            IEnumerable<string>? defNameContains = null,
            IEnumerable<string>? fileNameContains = null,
            bool matchPrecepts = false,
            bool matchTraitGated = false,
            bool matchSocial = false,
            bool isFallback = false)
        {
            Id = id;
            Priority = priority;
            Thoughts = new HashSet<string>(thoughts ?? Array.Empty<string>(), StringComparer.Ordinal);
            DefNameContains = ToArray(defNameContains);
            FileNameContains = ToArray(fileNameContains);
            MatchPrecepts = matchPrecepts;
            MatchTraitGated = matchTraitGated;
            MatchSocial = matchSocial;
            IsFallback = isFallback;
        }

        public string Id { get; }

        /// <summary>Higher priority patterns are tried first.</summary>
        public int Priority { get; }

        public HashSet<string> Thoughts { get; }

        public string[] DefNameContains { get; }

        public string[] FileNameContains { get; }

        public bool MatchPrecepts { get; }

        public bool MatchTraitGated { get; }

        public bool MatchSocial { get; }

        public bool IsFallback { get; }

        private static string[] ToArray(IEnumerable<string>? values) =>
            values == null ? Array.Empty<string>() : new List<string>(values).ToArray();
    }

    /// <summary>
    /// Sorts every thought into exactly one category. Runs once per startup, never at runtime.
    /// Order: explicit name, precept structure, defName pattern, file name pattern,
    /// trait-gated structure, social structure, fallback.
    /// </summary>
    public sealed class ThoughtClassifier
    {
        private readonly List<CategoryRule> byPriority;
        private readonly string fallbackId;

        public ThoughtClassifier(IEnumerable<CategoryRule> rules)
        {
            byPriority = new List<CategoryRule>(rules);
            byPriority.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            fallbackId = FindFallback(byPriority);
        }

        public string Classify(ThoughtFacts thought)
        {
            return ByExplicitName(thought.DefName)
                ?? (thought.IsPrecept ? FirstWhere(r => r.MatchPrecepts) : null)
                ?? ByPattern(thought.DefName, r => r.DefNameContains)
                ?? ByPattern(thought.FileName, r => r.FileNameContains)
                ?? (thought.HasRequiredTraits ? FirstWhere(r => r.MatchTraitGated) : null)
                ?? (thought.IsSocial ? FirstWhere(r => r.MatchSocial) : null)
                ?? fallbackId;
        }

        private string? ByExplicitName(string defName)
        {
            foreach (var rule in byPriority)
            {
                if (rule.Thoughts.Contains(defName))
                    return rule.Id;
            }

            return null;
        }

        private string? FirstWhere(Func<CategoryRule, bool> predicate)
        {
            foreach (var rule in byPriority)
            {
                if (predicate(rule))
                    return rule.Id;
            }

            return null;
        }

        private string? ByPattern(string text, Func<CategoryRule, string[]> patternsOf)
        {
            if (text.Length == 0)
                return null;

            foreach (var rule in byPriority)
            {
                foreach (var pattern in patternsOf(rule))
                {
                    if (text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
                        return rule.Id;
                }
            }

            return null;
        }

        private static string FindFallback(List<CategoryRule> rules)
        {
            foreach (var rule in rules)
            {
                if (rule.IsFallback)
                    return rule.Id;
            }

            return CategoryIds.Other;
        }
    }

    /// <summary>Category ids the presets and the battle focus defaults refer to by name.</summary>
    public static class CategoryIds
    {
        public const string Death = "death";
        public const string Justice = "justice";
        public const string Corpses = "corpses";
        public const string Food = "food";
        public const string Rest = "rest";
        public const string Needs = "needs";
        public const string Drugs = "drugs";
        public const string Environment = "environment";
        public const string Social = "social";
        public const string Romance = "romance";
        public const string Health = "health";
        public const string Traits = "traits";
        public const string Psychic = "psychic";
        public const string Gatherings = "gatherings";
        public const string Colony = "colony";
        public const string Children = "children";
        public const string Titles = "titles";
        public const string Ideology = "ideology";
        public const string Other = "other";
    }
}
