using System.Collections.Generic;
using RimWorld;
using ReasonableMoodsAndNeeds.Core;
using UnityEngine;
using Verse;

namespace ReasonableMoodsAndNeeds.UI
{
    public sealed class PresetsTab : ISettingsTab
    {
        private readonly ReasonableMod mod;
        private readonly Catalogs catalogs;
        private readonly SettingsWindow window;

        public PresetsTab(ReasonableMod mod, Catalogs catalogs, SettingsWindow window)
        {
            this.mod = mod;
            this.catalogs = catalogs;
            this.window = window;
        }

        public void ProfileReplaced()
        {
        }

        public void Draw(Rect rect)
        {
            var list = new Listing_Standard();

            list.Begin(rect);
            list.Label("RMN.Presets.Intro".Translate());
            list.Gap();

            foreach (var id in Presets.All)
                DrawPreset(list, id);

            list.GapLine();
            list.Label("RMN.Share.Header".Translate());

            if (list.ButtonText("RMN.Share.Export".Translate()))
                Export();

            if (list.ButtonText("RMN.Share.Import".Translate()))
                Import();

            if (list.ButtonText("RMN.Share.ThoughtList".Translate()))
                ExportThoughtList();

            list.GapLine();

            var tips = mod.Profile.ShowVanillaInTooltips;

            list.CheckboxLabeled("RMN.VanillaTips".Translate(), ref tips, "RMN.VanillaTipsTip".Translate());
            mod.Profile.ShowVanillaInTooltips = tips;
            list.Gap();
            list.Label(Summary());
            list.Label("RMN.AppliesOnClose".Translate());
            list.End();
        }

        private void DrawPreset(Listing_Standard list, PresetId id)
        {
            var row = list.GetRect(Ui.RowHeight);
            var button = row.LeftPartPixels(160f);
            var description = new Rect(button.xMax + Ui.Gap, row.y, row.width - button.width - Ui.Gap, row.height);
            var name = ("RMN.Preset." + id).Translate();

            if (Widgets.ButtonText(button, name))
                Ui.Confirm("RMN.Preset.Confirm".Translate(name), () => window.ReplaceProfile(Presets.Build(id)));

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(description, ("RMN.Preset." + id + ".Desc").Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            list.Gap(4f);
        }

        private void Export()
        {
            GUIUtility.systemCopyBuffer = ProfileCodec.Encode(mod.Profile);
            Messages.Message("RMN.Share.Copied".Translate(), MessageTypeDefOf.TaskCompletion, false);
        }

        private void Import()
        {
            var text = GUIUtility.systemCopyBuffer ?? "";

            if (text.Trim().Length == 0)
            {
                Messages.Message("RMN.Share.Empty".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            var result = ProfileCodec.Decode(text);

            Ui.Confirm("RMN.Share.ImportConfirm".Translate(), () =>
            {
                window.ReplaceProfile(result.Profile);

                var message = result.Problems == 0
                    ? "RMN.Share.Imported".Translate()
                    : "RMN.Share.ImportedWithProblems".Translate(result.Problems);

                Messages.Message(message, result.Problems == 0 ? MessageTypeDefOf.TaskCompletion : MessageTypeDefOf.CautionInput, false);
            });
        }

        private void ExportThoughtList()
        {
            var rows = new List<KeyValuePair<ThoughtFacts, string>>();

            foreach (var entry in catalogs.Thoughts.Entries)
                rows.Add(new KeyValuePair<ThoughtFacts, string>(entry.Facts, entry.CategoryId));

            GUIUtility.systemCopyBuffer = ProfileCodec.ThoughtListTsv(rows);
            Messages.Message("RMN.Share.ThoughtListCopied".Translate(rows.Count), MessageTypeDefOf.TaskCompletion, false);
        }

        private string Summary()
        {
            var profile = mod.Profile;
            var changedCategories = 0;

            foreach (var pair in profile.Categories)
            {
                if (!pair.Value.IsIdentity)
                    changedCategories++;
            }

            return "RMN.Summary".Translate(catalogs.Thoughts.Entries.Count, changedCategories, profile.Thoughts.Count);
        }
    }
}
