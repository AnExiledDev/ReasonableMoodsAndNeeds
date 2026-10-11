using ReasonableMoodsAndNeeds.Core;
using ReasonableMoodsAndNeeds.Game;
using UnityEngine;
using Verse;

namespace ReasonableMoodsAndNeeds.UI
{
    /// <summary>One thought on one line: name, vanilla values, tuned values. Shared by the category preview and the thought list.</summary>
    public static class ThoughtRow
    {
        public static void DrawSummary(Rect row, ThoughtEntry entry, TuningProfile profile, int index)
        {
            var tuning = ThoughtMath.Resolve(profile, entry.CategoryId, entry.Def.defName);
            var nameRect = row.LeftPart(0.45f);
            var vanillaRect = new Rect(nameRect.xMax, row.y, row.width * 0.25f, row.height);
            var tunedRect = new Rect(vanillaRect.xMax, row.y, row.width * 0.3f, row.height);

            if (index % 2 == 1)
                Widgets.DrawLightHighlight(row);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(nameRect.ContractedBy(4f, 0f), entry.Facts.Label);
            GUI.color = Ui.Muted;
            Widgets.Label(vanillaRect, Describe(entry, entry.Facts.Stages, null));
            GUI.color = tuning.IsMuted || !entry.Facts.IsTunable ? Ui.Muted : Ui.Changed;
            Widgets.Label(tunedRect, TunedText(entry, tuning));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            TooltipHandler.TipRegion(row, entry.Tooltip);
        }

        private static string TunedText(ThoughtEntry entry, ResolvedTuning tuning)
        {
            if (!entry.Facts.IsTunable)
                return "RMN.CodeComputedShort".Translate();

            return tuning.IsMuted ? "RMN.Muted".Translate().ToString() : Describe(entry, entry.Facts.Stages, tuning);
        }

        /// <summary>The mood range across stages, plus the opinion range for social thoughts. "-10" or "-10 to -2".</summary>
        private static string Describe(ThoughtEntry entry, System.Collections.Generic.IReadOnlyList<StageValues> stages, ResolvedTuning? tuning)
        {
            var moodMin = float.MaxValue;
            var moodMax = float.MinValue;
            var opinionMin = float.MaxValue;
            var opinionMax = float.MinValue;

            foreach (var vanilla in stages)
            {
                var value = tuning.HasValue ? ThoughtMath.ApplyStage(vanilla, tuning.Value, entry.IsSocialMemory) : vanilla;

                moodMin = Mathf.Min(moodMin, value.Mood);
                moodMax = Mathf.Max(moodMax, value.Mood);
                opinionMin = Mathf.Min(opinionMin, value.Opinion);
                opinionMax = Mathf.Max(opinionMax, value.Opinion);
            }

            var text = Span(moodMin, moodMax);

            if (opinionMin != 0f || opinionMax != 0f)
                text += " / " + Span(opinionMin, opinionMax) + " " + "RMN.OpinionShort".Translate();

            return text;
        }

        private static string Span(float min, float max) =>
            Mathf.Approximately(min, max) ? Ui.Signed(min) : Ui.Signed(min) + ".." + Ui.Signed(max);
    }
}
