using System.Collections.Generic;
using ReasonableMoodsAndNeeds.Core;
using ReasonableMoodsAndNeeds.Game;
using RimWorld;
using UnityEngine;
using Verse;

namespace ReasonableMoodsAndNeeds.UI
{
    /// <summary>Global break threshold and recovery speed, then every mental break with its commonality and trait requirement.</summary>
    public sealed class BreaksTab : ISettingsTab
    {
        private const float TraitButtonWidth = 190f;

        private readonly ReasonableMod mod;
        private readonly Catalogs catalogs;
        private Vector2 scroll;

        public BreaksTab(ReasonableMod mod, Catalogs catalogs)
        {
            this.mod = mod;
            this.catalogs = catalogs;
        }

        public void ProfileReplaced()
        {
        }

        public void Draw(Rect rect)
        {
            var tuning = mod.Profile.Breaks;
            var y = rect.y;

            y += Ui.Paragraph(new Rect(rect.x, y, rect.width, 0f), "RMN.Breaks.Intro".Translate()) + Ui.Gap;
            tuning.ThresholdMultiplier = Ui.Slider(Ui.NextRow(rect, ref y), "RMN.Breaks.Threshold".Translate(), tuning.ThresholdMultiplier, Limits.BreakThreshold, Ui.Times, 1f, "RMN.Breaks.ThresholdTip".Translate());
            y += DrawThresholdReadout(new Rect(rect.x, y, rect.width, Ui.RowHeight), tuning);
            tuning.StateDurationMultiplier = Ui.Slider(Ui.NextRow(rect, ref y), "RMN.Breaks.StateDuration".Translate(), tuning.StateDurationMultiplier, Limits.StateDuration, Ui.Times, 1f, "RMN.Breaks.StateDurationTip".Translate());
            y += Ui.Gap;
            DrawBreakList(new Rect(rect.x, y, rect.width, rect.yMax - y), tuning);
        }

        private float DrawThresholdReadout(Rect row, BreakTuning tuning)
        {
            var vanilla = catalogs.Breaks.VanillaThreshold;

            if (!vanilla.HasValue)
                return 0f;

            var tuned = BreakMath.Threshold(vanilla.Value, tuning);

            Ui.Paragraph(row, "RMN.Breaks.ThresholdReadout".Translate(tuned.ToStringPercent(), vanilla.Value.ToStringPercent()));

            return Ui.RowHeight;
        }

        private void DrawBreakList(Rect rect, BreakTuning tuning)
        {
            var breaks = catalogs.Breaks.Breaks;
            var view = new Rect(0f, 0f, rect.width - 16f, breaks.Count * (Ui.SliderHeight + 2f));
            var y = 0f;

            Widgets.DrawMenuSection(rect);
            Widgets.BeginScrollView(rect.ContractedBy(4f), ref scroll, view);

            foreach (var entry in breaks)
                DrawBreak(Ui.NextRow(view, ref y), entry, tuning);

            Widgets.EndScrollView();
        }

        private void DrawBreak(Rect row, BreakEntry entry, BreakTuning tuning)
        {
            var defName = entry.Def.defName;
            var sliderRect = new Rect(row.x, row.y, row.width - TraitButtonWidth - Ui.Gap, row.height);
            var traitRect = row.RightPartPixels(TraitButtonWidth).ContractedBy(0f, 2f);
            var current = tuning.Commonality.TryGetValue(defName, out var found) ? found : 1f;
            var label = (entry.Def.label ?? defName).CapitalizeFirst() + " (" + IntensityLabel(entry.Def.intensity) + ")";
            var next = Ui.Slider(sliderRect, label, current, Limits.Commonality, Ui.Times, 1f, "RMN.Breaks.CommonalityTip".Translate());

            if (next == 1f)
                tuning.Commonality.Remove(defName);
            else
                tuning.Commonality[defName] = next;

            if (Widgets.ButtonText(traitRect, GateLabel(entry, tuning)))
                ChooseGate(entry, tuning);
        }

        private static string IntensityLabel(MentalBreakIntensity intensity) => ("RMN.Breaks.Intensity." + intensity).Translate();

        private static string GateLabel(BreakEntry entry, BreakTuning tuning)
        {
            if (!tuning.TraitGates.TryGetValue(entry.Def.defName, out var gate))
                return entry.RequiredTrait == null ? "RMN.Breaks.Anyone".Translate().ToString() : TraitLabel(entry.RequiredTrait);

            switch (gate)
            {
                case TraitGate.Requires requires:
                    var trait = DefDatabase<TraitDef>.GetNamedSilentFail(requires.TraitDefName);

                    return trait == null ? requires.TraitDefName : TraitLabel(trait);
                default:
                    return "RMN.Breaks.Anyone".Translate();
            }
        }

        private static string TraitLabel(TraitDef trait) => "RMN.Breaks.Requires".Translate(trait.LabelCap);

        private static void ChooseGate(BreakEntry entry, BreakTuning tuning)
        {
            var defName = entry.Def.defName;
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption("RMN.Breaks.VanillaGate".Translate(), () => tuning.TraitGates.Remove(defName)),
                new FloatMenuOption("RMN.Breaks.Anyone".Translate(), () => tuning.TraitGates[defName] = TraitGate.Anyone.Instance),
            };
            var traits = new List<TraitDef>(DefDatabase<TraitDef>.AllDefsListForReading);

            traits.Sort((a, b) => string.Compare(a.LabelCap.ToString(), b.LabelCap.ToString(), System.StringComparison.OrdinalIgnoreCase));

            foreach (var trait in traits)
            {
                var traitName = trait.defName;

                options.Add(new FloatMenuOption(TraitLabel(trait), () => tuning.TraitGates[defName] = new TraitGate.Requires(traitName)));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}
