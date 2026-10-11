using System.Collections.Generic;
using ReasonableMoodsAndNeeds.Core;
using ReasonableMoodsAndNeeds.Game;
using UnityEngine;
using Verse;

namespace ReasonableMoodsAndNeeds.UI
{
    /// <summary>Category list on the left, the selected category's sliders and a live preview on the right.</summary>
    public sealed class CategoriesTab : ISettingsTab
    {
        private const float ListWidth = 230f;

        private readonly ReasonableMod mod;
        private readonly Catalogs catalogs;
        private readonly List<ThoughtEntry> preview = new List<ThoughtEntry>();
        private string selectedId = "";
        private Vector2 listScroll;
        private Vector2 previewScroll;

        public CategoriesTab(ReasonableMod mod, Catalogs catalogs)
        {
            this.mod = mod;
            this.catalogs = catalogs;

            if (catalogs.Thoughts.Categories.Count > 0)
                Select(catalogs.Thoughts.Categories[0].defName);
        }

        public void ProfileReplaced()
        {
        }

        public void Draw(Rect rect)
        {
            DrawList(rect.LeftPartPixels(ListWidth));
            DrawEditor(new Rect(rect.x + ListWidth + 12f, rect.y, rect.width - ListWidth - 12f, rect.height));
        }

        private void DrawList(Rect rect)
        {
            var categories = catalogs.Thoughts.Categories;
            var view = new Rect(0f, 0f, rect.width - 16f, categories.Count * Ui.RowHeight);

            Widgets.BeginScrollView(rect, ref listScroll, view);

            for (var i = 0; i < categories.Count; i++)
            {
                var category = categories[i];
                var row = new Rect(0f, i * Ui.RowHeight, view.width, Ui.RowHeight);
                var isChanged = !mod.Profile.CategoryOrIdentity(category.defName).IsIdentity;

                if (category.defName == selectedId)
                    Widgets.DrawHighlight(row);

                Widgets.DrawHighlightIfMouseover(row);
                GUI.color = isChanged ? Ui.Changed : Color.white;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(row.ContractedBy(4f, 0f), category.LabelCap + " (" + catalogs.Thoughts.CountOf(category.defName) + ")");
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;

                if (Widgets.ButtonInvisible(row))
                    Select(category.defName);
            }

            Widgets.EndScrollView();
        }

        private void DrawEditor(Rect rect)
        {
            var category = SelectedDef();

            if (category == null)
                return;

            var y = rect.y;

            Ui.Header(new Rect(rect.x, y, rect.width, 32f), category.LabelCap);
            y += 34f;
            y += Ui.Paragraph(new Rect(rect.x, y, rect.width, 0f), category.description ?? "") + Ui.Gap;

            var values = mod.Profile.EditCategory(category.defName);

            y += Ui.MultiplierSliders(new Rect(rect.x, y, rect.width, 0f), values) + Ui.Gap;

            var buttons = new Rect(rect.x, y, rect.width, Ui.RowHeight);

            if (Widgets.ButtonText(buttons.LeftHalf().ContractedBy(2f, 0f), "RMN.Category.Reset".Translate()))
                mod.Profile.Categories.Remove(category.defName);

            if (Widgets.ButtonText(buttons.RightHalf().ContractedBy(2f, 0f), "RMN.Category.ApplyToAll".Translate()))
                Ui.Confirm("RMN.Category.ApplyToAllConfirm".Translate(category.LabelCap), () => CopyToAll(values));

            y += Ui.RowHeight + Ui.Gap;
            DrawPreview(new Rect(rect.x, y, rect.width, rect.yMax - y));
        }

        private void CopyToAll(Multipliers values)
        {
            foreach (var category in catalogs.Thoughts.Categories)
                mod.Profile.Categories[category.defName] = values.Clone();
        }

        /// <summary>Every thought in the category, vanilla on the left and tuned on the right. Only visible rows are drawn.</summary>
        private void DrawPreview(Rect rect)
        {
            if (rect.height < Ui.RowHeight * 2f)
                return;

            var view = new Rect(0f, 0f, rect.width - 16f, preview.Count * Ui.RowHeight);
            var first = Mathf.Max(0, Mathf.FloorToInt(previewScroll.y / Ui.RowHeight));
            var last = Mathf.Min(preview.Count, first + Mathf.CeilToInt(rect.height / Ui.RowHeight) + 1);

            Widgets.DrawMenuSection(rect);
            Widgets.BeginScrollView(rect, ref previewScroll, view);

            for (var i = first; i < last; i++)
                ThoughtRow.DrawSummary(new Rect(0f, i * Ui.RowHeight, view.width, Ui.RowHeight), preview[i], mod.Profile, i);

            Widgets.EndScrollView();
        }

        private ThoughtCategoryDef? SelectedDef()
        {
            foreach (var category in catalogs.Thoughts.Categories)
            {
                if (category.defName == selectedId)
                    return category;
            }

            return null;
        }

        private void Select(string categoryId)
        {
            selectedId = categoryId;
            previewScroll = Vector2.zero;
            preview.Clear();

            foreach (var entry in catalogs.Thoughts.Entries)
            {
                if (entry.CategoryId == categoryId)
                    preview.Add(entry);
            }
        }
    }
}
