> **AI-written from operator direction.** The intent is the operator's; the wording and scope are an agent's.
> `session: 663e4019-4580-4e94-9215-893a9bca0315 | 2026-10-11 | source: op:2026-10-11-0736-7e53`

# Reasonable Moods and Needs

A RimWorld 1.6 mod that makes colonists less fragile about small things, and lets you tune every part of it.

Back in 2017 I put out [Reasonable Moods and Needs for B18](https://steamcommunity.com/sharedfiles/filedetails/?id=935084395). It was a pile of XML patches that cut down the mood penalties that kept setting off breaks over nothing, and you got whatever numbers I picked. This is a rebuild for 1.6, written from scratch in C#, published as a new Workshop item, and every number in it is yours to change.

## What it does

Every thought in the game gets sorted into one of 19 categories, food, sleep, corpses, death, social, romance, ideology and so on. There are close to 900 thoughts with every DLC loaded, so tuning them one at a time isn't realistic. Drag one category slider and a few hundred thoughts move with it. Each category has its own multipliers for bad mood, good mood, bad opinion, good opinion and memory duration, and any single thought can be overridden or muted on top of that.

The rest of it:

- **Mental breaks.** Move the break threshold, change how long breaks last, change how often each break gets picked or turn it off, and change which trait a break needs. The Reasonable preset makes berserk need Psychopath.
- **Needs.** Raise base mood, and change how fast mood, beauty, comfort and the other drifting needs rise and fall.
- **Battle focus.** Optional. Drafted colonists, and anyone who was in a fight in the last few hours, ignore small bad memories from the categories you pick. Nobody should care about eating without a table while raiders are at the door.
- **Presets.** Vanilla, Light, Reasonable and Relaxed. Reasonable is the default for new installs and it's close to what the B18 mod did.
- **Sharing.** Settings copy to the clipboard as plain text, so you can move them between installs or paste them to someone.

Thoughts added by other mods get sorted by the same rules. Anything the mod can't place goes into "everything else", where you can still tune it, and the Copy thought list button gives you a table to paste into a bug report if something landed in the wrong spot.

## Performance

This was the part I cared about most. A lot of mods check things far more often than they need to, every tick or every frame, for numbers that change maybe once a session.

This one rewrites the thought, break and need definitions once at startup and again whenever you close the settings window. After that the game reads its own fields like it always does, so during play the mod costs nothing per tick. Dragging sliders doesn't touch the game either, the work happens once when the window closes.

The only Harmony patch is battle focus. It only runs when a pawn gains a memory, and it starts with a single array lookup that most memories never get past. It's on in the Reasonable preset, which is the default, so most people will have the patch. Turn battle focus off and the patch is removed entirely. [docs/performance.md](docs/performance.md) has the details.

## Status

It compiles clean against the 1.6 reference assemblies and the core logic has unit tests, but it hasn't been run inside the game yet. I'm testing it on my own PC before it goes on the Workshop, using [docs/testing-checklist.md](docs/testing-checklist.md). Odyssey's thoughts weren't part of the data the categories were tuned against, so expect a few of those to land in "everything else" for now.

You can add it to an existing colony. Removing it puts the thought, break and need numbers back to vanilla on the next launch. One thing does stick: a social memory (an insult, a nice chat) stores its opinion value in your save when it's created. So changing the opinion sliders only affects new social memories, and the ones made while the mod was on keep their tuned value until they fade.

## Install

It needs [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077). Until the Workshop page is up, grab the `ReasonableMoodsAndNeeds` artifact from the latest [CI run](https://github.com/AnExiledDev/ReasonableMoodsAndNeeds/actions), unzip it into `RimWorld/Mods/`, and load it after Harmony.

## Building

You need the .NET SDK, 10 or newer. The game assemblies come from the `Krafs.Rimworld.Ref` package, so you don't need RimWorld installed to build.

```sh
dotnet test Tests/ReasonableMoodsAndNeeds.Tests
dotnet build Source/ReasonableMoodsAndNeeds -c Release
```

The build drops `ReasonableMoodsAndNeeds.dll` into `1.6/Assemblies/`. If you want to see how it's put together, [docs/design.md](docs/design.md) covers the layout and what each setting writes, and the categories live in `tools/categories.py`, which generates `Common/Defs/ThoughtCategories.xml`.

## License

MIT. See [LICENSE](LICENSE).
