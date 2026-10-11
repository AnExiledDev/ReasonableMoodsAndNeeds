using ReasonableMoodsAndNeeds.Core;
using ReasonableMoodsAndNeeds.Game;
using UnityEngine;
using Verse;

namespace ReasonableMoodsAndNeeds.UI
{
    /// <summary>Battle focus: colonists in a fight shrug off small negative memories from the chosen categories.</summary>
    public sealed class BattleFocusTab : ISettingsTab
    {
        private readonly ReasonableMod mod;
        private readonly Catalogs catalogs;
        private Vector2 scroll;

        public BattleFocusTab(ReasonableMod mod, Catalogs catalogs)
        {
            this.mod = mod;
            this.catalogs = catalogs;
        }

        public void ProfileReplaced()
        {
        }

        public void Draw(Rect rect)
        {
            var battle = mod.Profile.BattleFocus;
            var defaults = new BattleFocusSettings();
            var y = rect.y;

            y += Ui.Paragraph(new Rect(rect.x, y, rect.width, 0f), "RMN.Battle.Intro".Translate()) + Ui.Gap;
            Widgets.CheckboxLabeled(Ui.NextRow(rect, ref y, Ui.RowHeight), "RMN.Battle.Enabled".Translate(), ref battle.Enabled);

            if (!battle.Enabled)
                return;

            battle.MaxMoodMagnitude = Ui.Slider(Ui.NextRow(rect, ref y), "RMN.Battle.MaxMood".Translate(), battle.MaxMoodMagnitude, Limits.BattleMaxMood, MoodLimit, defaults.MaxMoodMagnitude, "RMN.Battle.MaxMoodTip".Translate(), roundTo: 1f);
            battle.CombatWindowHours = Ui.Slider(Ui.NextRow(rect, ref y), "RMN.Battle.Window".Translate(), battle.CombatWindowHours, Limits.BattleWindowHours, Hours, defaults.CombatWindowHours, "RMN.Battle.WindowTip".Translate(), roundTo: 0.5f);
            y += Ui.Gap;
            Widgets.Label(Ui.NextRow(rect, ref y, Ui.RowHeight), "RMN.Battle.Categories".Translate());
            DrawCategories(new Rect(rect.x, y, rect.width, rect.yMax - y), battle);
        }

        private static string MoodLimit(float magnitude) => "-" + magnitude.ToString("0");

        private static string Hours(float hours) => "RMN.Battle.Hours".Translate(hours.ToString("0.#"));

        private void DrawCategories(Rect rect, BattleFocusSettings battle)
        {
            var categories = catalogs.Thoughts.Categories;
            var view = new Rect(0f, 0f, rect.width - 16f, categories.Count * Ui.RowHeight);
            var y = 0f;

            Widgets.DrawMenuSection(rect);
            Widgets.BeginScrollView(rect.ContractedBy(4f), ref scroll, view);

            foreach (var category in categories)
            {
                var isOn = battle.Categories.Contains(category.defName);
                var wasOn = isOn;

                Widgets.CheckboxLabeled(Ui.NextRow(view, ref y, Ui.RowHeight - 2f), category.LabelCap, ref isOn);

                if (isOn == wasOn)
                    continue;

                if (isOn)
                    battle.Categories.Add(category.defName);
                else
                    battle.Categories.Remove(category.defName);
            }

            Widgets.EndScrollView();
        }
    }
}
