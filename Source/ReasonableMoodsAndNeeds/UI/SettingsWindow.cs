using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ReasonableMoodsAndNeeds.UI
{
    public interface ISettingsTab
    {
        void Draw(Rect rect);

        /// <summary>Drop any cached view state; called when the whole profile is replaced.</summary>
        void ProfileReplaced();
    }

    /// <summary>
    /// The mod settings page. Edits go straight into the in-memory profile; the game only sees them
    /// when the window closes and RimWorld calls WriteSettings, so dragging a slider costs nothing.
    /// </summary>
    public sealed class SettingsWindow
    {
        private const float TabHeight = 32f;

        private readonly ReasonableMod mod;
        private readonly List<ISettingsTab> tabs = new List<ISettingsTab>();
        private readonly List<TabRecord> records = new List<TabRecord>();
        private int selected;

        public SettingsWindow(ReasonableMod mod)
        {
            this.mod = mod;
        }

        public void Draw(Rect inRect)
        {
            var catalogs = mod.Catalogs;

            if (catalogs == null)
            {
                Widgets.Label(inRect, "RMN.NotReady".Translate());
                return;
            }

            if (tabs.Count == 0)
                Build(catalogs);

            var body = new Rect(inRect.x, inRect.y + TabHeight, inRect.width, inRect.height - TabHeight);

            Widgets.DrawMenuSection(body);
            TabDrawer.DrawTabs(body, records);
            tabs[selected].Draw(body.ContractedBy(10f));
        }

        /// <summary>Replaces the whole profile (preset or import) and tells every tab to forget cached views.</summary>
        public void ReplaceProfile(Core.TuningProfile profile)
        {
            mod.Profile = profile;

            foreach (var tab in tabs)
                tab.ProfileReplaced();
        }

        private void Build(Catalogs catalogs)
        {
            Add("RMN.Tab.Presets", new PresetsTab(mod, catalogs, this));
            Add("RMN.Tab.Categories", new CategoriesTab(mod, catalogs));
            Add("RMN.Tab.Thoughts", new ThoughtsTab(mod, catalogs));
            Add("RMN.Tab.Breaks", new BreaksTab(mod, catalogs));
            Add("RMN.Tab.Needs", new NeedsTab(mod, catalogs));
            Add("RMN.Tab.BattleFocus", new BattleFocusTab(mod, catalogs));
        }

        private void Add(string labelKey, ISettingsTab tab)
        {
            var index = tabs.Count;

            tabs.Add(tab);
            records.Add(new TabRecord(labelKey.Translate(), () => selected = index, () => selected == index));
        }
    }
}
