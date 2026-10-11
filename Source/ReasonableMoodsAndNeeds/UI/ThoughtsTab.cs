using System;
using System.Collections.Generic;
using ReasonableMoodsAndNeeds.Core;
using ReasonableMoodsAndNeeds.Game;
using UnityEngine;
using Verse;

namespace ReasonableMoodsAndNeeds.UI
{
    public enum SignFilter
    {
        Any,
        Negative,
        Positive,
    }

    /// <summary>
    /// Every thought, searchable and filterable, with per-thought overrides. The filtered list is rebuilt
    /// only when a filter changes, and only the rows on screen are drawn, so a thousand thoughts scroll smoothly.
    /// </summary>
    public sealed class ThoughtsTab : ISettingsTab
    {
        private const float FilterHeight = 30f;
        private const float EditorHeight = 230f;

        private readonly ReasonableMod mod;
        private readonly Catalogs catalogs;
        private readonly List<ThoughtEntry> shown = new List<ThoughtEntry>();
        private readonly List<string> modNames = new List<string>();
        private string search = "";
        private string? categoryFilter;
        private string? modFilter;
        private SignFilter sign;
        private bool overriddenOnly;
        private bool isDirty = true;
        private ThoughtEntry? selected;
        private Vector2 scroll;

        public ThoughtsTab(ReasonableMod mod, Catalogs catalogs)
        {
            this.mod = mod;
            this.catalogs = catalogs;

            var seen = new HashSet<string>();

            foreach (var entry in catalogs.Thoughts.Entries)
            {
                if (seen.Add(entry.Facts.ModName))
                    modNames.Add(entry.Facts.ModName);
            }

            modNames.Sort(StringComparer.OrdinalIgnoreCase);
        }

        public void ProfileReplaced()
        {
            isDirty = true;
        }

        public void Draw(Rect rect)
        {
            DrawFilters(new Rect(rect.x, rect.y, rect.width, FilterHeight));

            if (isDirty)
                Refilter();

            var listTop = rect.y + FilterHeight * 2f + Ui.Gap;
            var listRect = new Rect(rect.x, listTop, rect.width, rect.height - (listTop - rect.y) - EditorHeight - Ui.Gap);

            DrawList(listRect);
            DrawEditor(new Rect(rect.x, listRect.yMax + Ui.Gap, rect.width, EditorHeight));
        }

        private void DrawFilters(Rect row)
        {
            var x = row.x;
            var searchRect = new Rect(x, row.y, 220f, Ui.RowHeight);
            var newSearch = Widgets.TextField(searchRect, search);

            if (newSearch != search)
            {
                search = newSearch;
                isDirty = true;
            }

            if (search.Length == 0)
            {
                GUI.color = Ui.Muted;
                Widgets.Label(searchRect.ContractedBy(6f, 4f), "RMN.Search".Translate());
                GUI.color = Color.white;
            }

            x += 228f;

            if (Widgets.ButtonText(new Rect(x, row.y, 170f, Ui.RowHeight), categoryFilter == null ? "RMN.Filter.AllCategories".Translate().ToString() : catalogs.Thoughts.LabelOf(categoryFilter)))
                ChooseCategory();

            x += 178f;

            if (Widgets.ButtonText(new Rect(x, row.y, 170f, Ui.RowHeight), modFilter ?? "RMN.Filter.AllMods".Translate().ToString()))
                ChooseMod();

            x += 178f;

            if (Widgets.ButtonText(new Rect(x, row.y, 120f, Ui.RowHeight), ("RMN.Filter.Sign." + sign).Translate()))
                Ui.Menu((SignFilter[])Enum.GetValues(typeof(SignFilter)), s => ("RMN.Filter.Sign." + s).Translate(), s => { sign = s; isDirty = true; });

            var second = new Rect(row.x, row.y + FilterHeight, row.width, Ui.RowHeight);
            var only = overriddenOnly;

            Widgets.CheckboxLabeled(second.LeftPartPixels(220f), "RMN.Filter.OverriddenOnly".Translate(), ref only);

            if (only != overriddenOnly)
            {
                overriddenOnly = only;
                isDirty = true;
            }

            var countRect = new Rect(second.x + 228f, second.y, 200f, second.height);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(countRect, "RMN.ShownCount".Translate(shown.Count, catalogs.Thoughts.Entries.Count));
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonText(second.RightPartPixels(150f), "RMN.Bulk.Reset".Translate()))
                Ui.Confirm("RMN.Bulk.ResetConfirm".Translate(shown.Count), () => Bulk(null));

            if (Widgets.ButtonText(new Rect(second.xMax - 308f, second.y, 150f, second.height), "RMN.Bulk.Mute".Translate()))
                Ui.Confirm("RMN.Bulk.MuteConfirm".Translate(shown.Count), () => Bulk(ThoughtOverride.Muted.Instance));
        }

        private void DrawList(Rect rect)
        {
            var view = new Rect(0f, 0f, rect.width - 16f, shown.Count * Ui.RowHeight);
            var first = Mathf.Max(0, Mathf.FloorToInt(scroll.y / Ui.RowHeight));
            var last = Mathf.Min(shown.Count, first + Mathf.CeilToInt(rect.height / Ui.RowHeight) + 1);

            Widgets.DrawMenuSection(rect);
            Widgets.BeginScrollView(rect, ref scroll, view);

            for (var i = first; i < last; i++)
            {
                var entry = shown[i];
                var row = new Rect(0f, i * Ui.RowHeight, view.width, Ui.RowHeight);

                if (entry == selected)
                    Widgets.DrawHighlightSelected(row);

                ThoughtRow.DrawSummary(row, entry, mod.Profile, i);
                Widgets.DrawHighlightIfMouseover(row);

                if (Widgets.ButtonInvisible(row))
                    selected = entry;
            }

            Widgets.EndScrollView();
        }

        private void DrawEditor(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            if (selected == null)
            {
                Widgets.Label(rect.ContractedBy(8f), "RMN.Thought.SelectHint".Translate());
                return;
            }

            var inner = rect.ContractedBy(8f);
            var defName = selected.Def.defName;
            var header = new Rect(inner.x, inner.y, inner.width, Ui.RowHeight);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(header.LeftPart(0.5f), selected.Facts.Label + "  (" + catalogs.Thoughts.LabelOf(selected.CategoryId) + ")");
            Text.Anchor = TextAnchor.UpperLeft;

            var modeRect = header.RightPartPixels(360f);

            if (Widgets.ButtonText(Ui.Third(modeRect, 0), "RMN.Thought.Inherit".Translate()))
                SetOverride(defName, null);

            if (Widgets.ButtonText(Ui.Third(modeRect, 1), "RMN.Thought.Custom".Translate()))
                SetOverride(defName, new ThoughtOverride.Custom(mod.Profile.CategoryOrIdentity(selected.CategoryId).Clone()));

            if (Widgets.ButtonText(Ui.Third(modeRect, 2), "RMN.Thought.Mute".Translate()))
                SetOverride(defName, ThoughtOverride.Muted.Instance);

            var body = new Rect(inner.x, header.yMax + 4f, inner.width, inner.height - Ui.RowHeight - 4f);

            mod.Profile.Thoughts.TryGetValue(defName, out var current);

            switch (current)
            {
                case null:
                    Widgets.Label(body, "RMN.Thought.Inheriting".Translate(catalogs.Thoughts.LabelOf(selected.CategoryId)));
                    break;
                case ThoughtOverride.Muted _:
                    Widgets.Label(body, "RMN.Thought.IsMuted".Translate());
                    break;
                case ThoughtOverride.Custom custom:
                    Ui.MultiplierSliders(body, custom.Values);
                    break;
            }
        }

        private void SetOverride(string defName, ThoughtOverride? value)
        {
            if (value == null)
                mod.Profile.Thoughts.Remove(defName);
            else
                mod.Profile.Thoughts[defName] = value;

            isDirty = true;
        }

        private void Bulk(ThoughtOverride? value)
        {
            foreach (var entry in shown)
            {
                if (value == null)
                    mod.Profile.Thoughts.Remove(entry.Def.defName);
                else
                    mod.Profile.Thoughts[entry.Def.defName] = value;
            }

            isDirty = true;
        }

        private void ChooseCategory()
        {
            var options = new List<string?> { null };

            foreach (var category in catalogs.Thoughts.Categories)
                options.Add(category.defName);

            Ui.Menu(options, id => id == null ? "RMN.Filter.AllCategories".Translate().ToString() : catalogs.Thoughts.LabelOf(id), id => { categoryFilter = id; isDirty = true; });
        }

        private void ChooseMod()
        {
            var options = new List<string?> { null };

            options.AddRange(modNames);
            Ui.Menu(options, name => name ?? "RMN.Filter.AllMods".Translate().ToString(), name => { modFilter = name; isDirty = true; });
        }

        private void Refilter()
        {
            shown.Clear();

            foreach (var entry in catalogs.Thoughts.Entries)
            {
                if (Matches(entry))
                    shown.Add(entry);
            }

            isDirty = false;
        }

        private bool Matches(ThoughtEntry entry)
        {
            var facts = entry.Facts;

            if (categoryFilter != null && entry.CategoryId != categoryFilter)
                return false;

            if (modFilter != null && facts.ModName != modFilter)
                return false;

            if (sign == SignFilter.Negative && !facts.HasNegativeMood)
                return false;

            if (sign == SignFilter.Positive && !facts.HasPositiveMood)
                return false;

            if (overriddenOnly && !mod.Profile.Thoughts.ContainsKey(entry.Def.defName))
                return false;

            return search.Length == 0
                || facts.Label.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                || facts.DefName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
