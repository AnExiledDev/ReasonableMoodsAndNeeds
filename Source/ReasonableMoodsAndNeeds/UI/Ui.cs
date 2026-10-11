using System;
using System.Collections.Generic;
using ReasonableMoodsAndNeeds.Core;
using UnityEngine;
using Verse;
using Range = ReasonableMoodsAndNeeds.Core.Range;

namespace ReasonableMoodsAndNeeds.UI
{
    /// <summary>Small drawing helpers shared by every tab.</summary>
    public static class Ui
    {
        public const float RowHeight = 28f;
        public const float SliderHeight = 32f;
        public const float Gap = 8f;

        public static readonly Color Muted = new Color(0.6f, 0.6f, 0.6f);
        public static readonly Color Changed = new Color(0.55f, 0.85f, 1f);
        public static readonly Color Bad = new Color(1f, 0.55f, 0.5f);
        public static readonly Color Good = new Color(0.6f, 1f, 0.6f);

        /// <summary>A labelled slider row: label on the left, slider with its value in the middle, an optional reset on the right.</summary>
        public static float Slider(Rect row, string label, float value, Range range, Func<float, string> format, float vanilla, string? tooltip = null, float roundTo = 0.05f)
        {
            var labelRect = row.LeftPart(0.38f);
            var resetRect = row.RightPartPixels(24f);
            var sliderRect = new Rect(labelRect.xMax + Gap, row.y, row.width - labelRect.width - resetRect.width - Gap * 2f, row.height);
            var isChanged = value != vanilla;

            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = isChanged ? Changed : Color.white;
            Widgets.Label(labelRect, label);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            if (tooltip != null)
                TooltipHandler.TipRegion(labelRect, tooltip);

            var result = Widgets.HorizontalSlider(sliderRect, value, range.Min, range.Max, middleAlignment: true, label: format(value), roundTo: roundTo);

            if (isChanged && Widgets.ButtonText(resetRect, "x"))
                result = vanilla;

            if (isChanged)
                TooltipHandler.TipRegion(resetRect, "RMN.ResetTo".Translate(format(vanilla)));

            return result;
        }

        public static string Times(float multiplier) => "x" + multiplier.ToString("0.##");

        public static string Percent(float multiplier) => multiplier.ToStringPercent();

        public static string Signed(float value) => value.ToStringWithSign("0.#");

        public static void Header(Rect rect, string text)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(rect, text);
            Text.Font = GameFont.Small;
        }

        public static float Paragraph(Rect rect, string text)
        {
            GUI.color = Muted;
            var height = Text.CalcHeight(text, rect.width);
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, height), text);
            GUI.color = Color.white;

            return height;
        }

        public static void Menu<T>(IEnumerable<T> items, Func<T, string> label, Action<T> choose)
        {
            var options = new List<FloatMenuOption>();

            foreach (var item in items)
            {
                var captured = item;

                options.Add(new FloatMenuOption(label(captured), () => choose(captured)));
            }

            if (options.Count > 0)
                Find.WindowStack.Add(new FloatMenu(options));
        }

        public static void Confirm(string text, Action confirmed)
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(text, confirmed, destructive: true));
        }

        /// <summary>The five multiplier sliders shared by the category editor and a thought's custom override.</summary>
        public static float MultiplierSliders(Rect rect, Multipliers m)
        {
            var y = rect.y;
            var identity = Multipliers.Identity;

            m.MoodNegative = Slider(NextRow(rect, ref y), "RMN.MoodNegative".Translate(), m.MoodNegative, Limits.Multiplier, Times, identity.MoodNegative, "RMN.MoodNegativeTip".Translate());
            m.MoodPositive = Slider(NextRow(rect, ref y), "RMN.MoodPositive".Translate(), m.MoodPositive, Limits.Multiplier, Times, identity.MoodPositive, "RMN.MoodPositiveTip".Translate());
            m.OpinionNegative = Slider(NextRow(rect, ref y), "RMN.OpinionNegative".Translate(), m.OpinionNegative, Limits.Multiplier, Times, identity.OpinionNegative, "RMN.OpinionNegativeTip".Translate());
            m.OpinionPositive = Slider(NextRow(rect, ref y), "RMN.OpinionPositive".Translate(), m.OpinionPositive, Limits.Multiplier, Times, identity.OpinionPositive, "RMN.OpinionPositiveTip".Translate());
            m.Duration = Slider(NextRow(rect, ref y), "RMN.Duration".Translate(), m.Duration, Limits.Duration, Times, identity.Duration, "RMN.DurationTip".Translate());

            return y - rect.y;
        }

        /// <summary>One of three equal columns of a rect, with a small gap between them.</summary>
        public static Rect Third(Rect rect, int column)
        {
            var width = rect.width / 3f;

            return new Rect(rect.x + width * column, rect.y, width, rect.height).ContractedBy(2f, 0f);
        }

        public static Rect NextRow(Rect area, ref float y, float height = SliderHeight)
        {
            var row = new Rect(area.x, y, area.width, height);

            y += height + 2f;

            return row;
        }
    }
}
