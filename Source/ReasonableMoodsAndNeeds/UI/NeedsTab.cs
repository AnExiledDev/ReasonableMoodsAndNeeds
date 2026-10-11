using ReasonableMoodsAndNeeds.Core;
using ReasonableMoodsAndNeeds.Game;
using UnityEngine;
using Verse;

namespace ReasonableMoodsAndNeeds.UI
{
    /// <summary>The mood baseline, then rise and fall speed for every need that drifts towards a target.</summary>
    public sealed class NeedsTab : ISettingsTab
    {
        private readonly ReasonableMod mod;
        private readonly Catalogs catalogs;
        private Vector2 scroll;

        public NeedsTab(ReasonableMod mod, Catalogs catalogs)
        {
            this.mod = mod;
            this.catalogs = catalogs;
        }

        public void ProfileReplaced()
        {
        }

        public void Draw(Rect rect)
        {
            var needs = mod.Profile.Needs;
            var y = rect.y;

            y += Ui.Paragraph(new Rect(rect.x, y, rect.width, 0f), "RMN.Needs.Intro".Translate()) + Ui.Gap;
            needs.MoodBaselineShift = Ui.Slider(Ui.NextRow(rect, ref y), "RMN.Needs.Baseline".Translate(), needs.MoodBaselineShift, Limits.MoodBaseline, Ui.Signed, 0f, "RMN.Needs.BaselineTip".Translate(), roundTo: 1f);
            y += DrawBaselineReadout(new Rect(rect.x, y, rect.width, Ui.RowHeight), needs);
            y += Ui.Gap;
            DrawSeekers(new Rect(rect.x, y, rect.width, rect.yMax - y), needs);
        }

        private float DrawBaselineReadout(Rect row, NeedTuning needs)
        {
            var vanilla = catalogs.Needs.VanillaMoodBaseLevel;
            var tuned = NeedMath.MoodBaseLevel(vanilla, needs.MoodBaselineShift);

            Ui.Paragraph(row, "RMN.Needs.BaselineReadout".Translate(tuned.ToStringPercent(), vanilla.ToStringPercent()));

            return Ui.RowHeight;
        }

        private void DrawSeekers(Rect rect, NeedTuning needs)
        {
            var seekers = catalogs.Needs.Seekers;
            var perSeeker = (Ui.RowHeight + 2f) + (Ui.SliderHeight + 2f) * 2f + Ui.Gap;
            var view = new Rect(0f, 0f, rect.width - 8f - 16f, seekers.Count * perSeeker);
            var y = 0f;

            Widgets.DrawMenuSection(rect);
            Widgets.BeginScrollView(rect.ContractedBy(4f), ref scroll, view);

            foreach (var entry in seekers)
            {
                DrawSeeker(view, ref y, entry, needs);
                y += Ui.Gap;
            }

            Widgets.EndScrollView();
        }

        private void DrawSeeker(Rect view, ref float y, SeekerEntry entry, NeedTuning needs)
        {
            var defName = entry.Def.defName;
            var current = NeedMath.RatesFor(defName, needs);

            Widgets.Label(Ui.NextRow(view, ref y, Ui.RowHeight), entry.Def.LabelCap);

            var rise = Ui.Slider(Ui.NextRow(view, ref y), "RMN.Needs.Rise".Translate(), current.Rise, Limits.SeekerRate, Ui.Times, 1f, "RMN.Needs.RiseTip".Translate());
            var fall = Ui.Slider(Ui.NextRow(view, ref y), "RMN.Needs.Fall".Translate(), current.Fall, Limits.SeekerRate, Ui.Times, 1f, "RMN.Needs.FallTip".Translate());

            Store(defName, rise, fall);
        }

        /// <summary>Identity rates are dropped so an untouched need never shows up in the saved profile.</summary>
        private void Store(string defName, float rise, float fall)
        {
            var needs = mod.Profile.Needs;

            if (rise == 1f && fall == 1f)
            {
                needs.Seekers.Remove(defName);
                return;
            }

            var rates = mod.Profile.EditSeeker(defName);

            rates.Rise = rise;
            rates.Fall = fall;
        }
    }
}
