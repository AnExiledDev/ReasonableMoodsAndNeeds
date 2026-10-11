> **AI-written from operator direction.** The intent is the operator's; the wording and scope are an agent's.
> `session: 663e4019-4580-4e94-9215-893a9bca0315 | 2026-10-11 | source: op:2026-10-11-0736-7e53`

# In-game testing checklist

## Provenance

Operator, 2026-10-11 (op:2026-10-11-0736-7e53):

> I will bring it over to my PC when it's ready for testing, to conduct the testing.

## What is already verified

- The pure core (math, presets, codec, classifier) has unit tests, run in CI.
- The mod assembly compiles against the RimWorld 1.6 reference assemblies with warnings as errors.
- Category coverage was simulated against 877 vanilla and DLC thoughts.

Nothing has been run inside RimWorld. RimWorld isn't installed on the build machine.

## Install

Download the `ReasonableMoodsAndNeeds` artifact from the latest CI run, unzip it into `RimWorld/Mods/`, and enable it after Harmony. Turn on dev mode so the log is visible.

## Checks

Each item is an assumption the code makes that only the game can confirm.

1. **Startup.** The log shows `[Reasonable Moods and Needs] Tuned N thoughts in X ms.` and no errors. N should be around 900 with every DLC. Note X.
2. **Category defs load.** The Categories tab lists 19 categories. If it is empty, the namespaced def node name `ReasonableMoodsAndNeeds.Game.ThoughtCategoryDef` is not resolving.
3. **File-name matching.** Filter the Thoughts tab to "everything else". It should hold about 5 vanilla thoughts. Hundreds means `Def.fileName` is not populated at runtime and file patterns are not matching.
4. **Mood multipliers.** Reasonable preset, then give a colonist a raw meal. "Ate raw food" should show about half the vanilla value, and its tooltip should show the vanilla line.
5. **Mute hides.** Mute a thought in the Thoughts tab, trigger it, and confirm it gives no mood and does not appear in the needs tab.
6. **Base mood.** Set base mood to +10 and check that a colonist with almost no thoughts sits about 10 points higher than before. This depends on `Need_Mood` reading `NeedDef.baseLevel`.
7. **Break threshold.** Change the threshold, close the window, and hover the mood bar. The break markers should move. Note how long they take, since the stat may be cached. Check the major and extreme markers move in proportion to the minor one.
8. **Break settings.** Set berserk to need Psychopath and confirm a non-psychopath never picks it in dev mode (force a break via the debug menu). Set a break's commonality to 0 and confirm it stops appearing.
9. **Battle focus.** Enable it, draft a colonist, have them eat without a table. No thought. Undraft and wait past the window, repeat. The thought should land. Then confirm a thought worse than the limit still lands while drafted.
10. **Battle focus off.** Disable it and confirm the Harmony patch is gone (dev mode Harmony patch listing, or check that a drafted colonist gets the table thought).
11. **Clicks.** In the Thoughts tab, click a row and confirm the editor opens for it. Rows use `Widgets.ButtonInvisible`.
12. **Clipboard.** Copy settings, change something, paste them back, and confirm the change is undone.
13. **Removal.** Save, quit, disable the mod, load the save. No errors, vanilla values back.
14. **Odyssey.** With Odyssey active, use "Copy thought list" and send the table back so the categories can be tuned for it.
