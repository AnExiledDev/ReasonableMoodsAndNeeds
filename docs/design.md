> **AI-written from operator direction.** The intent is the operator's; the wording and scope are an agent's.
> `session: 663e4019-4580-4e94-9215-893a9bca0315 | 2026-10-11 | source: op:2026-10-11-0736-7e53`

# Design

## Provenance

Operator, 2026-10-11 (op:2026-10-11-0736-7e53):

> I want categories and such as well, so you can modify whole sets easily, as there are a LOT to do one at a time.

## Layout

The mod is ports-and-adapters with a pure core.

| Folder | Role | Touches the game? |
|---|---|---|
| `Source/ReasonableMoodsAndNeeds/Core` | Profile model, presets, the codec, the classifier, all tuning math | No. Compiled into the net10 test project directly, so a stray `Verse` import breaks the test build. |
| `Source/ReasonableMoodsAndNeeds/Game` | Catalogs that snapshot defs and rewrite them, the category def type, the battle focus patch | Yes |
| `Source/ReasonableMoodsAndNeeds/UI` | One `ISettingsTab` per tab, plus shared widgets in `Ui.cs` | Yes |
| `Source/ReasonableMoodsAndNeeds/ReasonableMod.cs` | Composition root: loads settings, builds catalogs once, applies on window close | Yes |
| `Common/Defs/ThoughtCategories.xml` | The 19 categories and their matching rules | Data |
| `Common/Languages/English/Keyed/RMN.xml` | Every UI string | Data |

## Lifecycle

1. `ReasonableMod` constructor reads the saved profile. First install gets the Reasonable preset.
2. `LongEventHandler.ExecuteWhenFinished` runs `Initialize` once every mod has loaded, so defs other mods patched are part of the snapshot. The catalogs record the vanilla value of every field the mod will ever write.
3. `Apply` rewrites defs from the snapshot plus the profile. It always starts from the snapshot, so applying twice gives the same result as applying once.
4. Closing the settings window calls `WriteSettings`, which saves the profile and runs `Apply` again.

Nothing is written into save files. Removing the mod restores vanilla on the next launch.

## Profile

`TuningProfile` holds per-category multipliers, per-thought overrides (a `Custom` set of multipliers or `Muted`), break tuning, need tuning and battle focus settings. A missing entry means identity, so the profile only stores what changed.

`ProfileCodec` serializes it as `key=value` lines with a `v=1` header. The same text is the clipboard share format. Decoding is total: an unknown or out-of-range line is skipped or clamped and counted, never thrown.

## Classifier

`ThoughtClassifier` assigns each thought exactly one category, first match wins:

1. The category's explicit `thoughts` list.
2. Precept thoughts go to the category with `matchPrecepts`.
3. `defNameContains`, case-insensitive, higher `priority` first.
4. `fileNameContains` against the def's source file, same ordering.
5. Trait or gene gated thoughts go to `matchTraitGated`.
6. Social thoughts go to `matchSocial`.
7. Everything else goes to the `isFallback` category.

Other mods can add a `ThoughtCategoryDef` or patch a defName into an existing category's `thoughts` list. The in-game "Copy thought list" button exports every thought with its category for bug reports.

`tools/categories.py` is the single source for that XML. Edit the category table in it, then run `write` to regenerate the file and `simulate` to see where every thought lands. `tools/thoughts.py` builds the `simulate` input from the game's def files, which aren't in this repo. The current table was tuned against 877 vanilla and DLC thoughts. Odyssey thoughts were not in that data.

## What each setting writes

| Setting | Def field |
|---|---|
| Mood and opinion multipliers | `ThoughtStage.baseMoodEffect`, `baseOpinionOffset` |
| Duration | `ThoughtDef.durationDays` (memories only) |
| Mute | stage effects to 0 and `visible` to false |
| Break threshold | `StatDef MentalBreakThreshold.defaultBaseValue` |
| Break length | `MentalStateDef` min and max ticks |
| Break commonality and trait gate | `MentalBreakDef.baseCommonality`, `requiredTrait` |
| Base mood | mood `NeedDef.baseLevel` |
| Rise and fall rates | seeker `NeedDef.seekerRisePerHour`, `seekerFallPerHour` |
| Battle focus | the one Harmony prefix, see [performance.md](performance.md) |
