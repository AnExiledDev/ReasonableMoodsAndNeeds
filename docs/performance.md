> **AI-written from operator direction.** The intent is the operator's; the wording and scope are an agent's.
> `session: 663e4019-4580-4e94-9215-893a9bca0315 | 2026-10-11 | source: op:2026-10-11-0736-7e53`

# Performance

## Provenance

Operator, 2026-10-11 (op:2026-10-11-0736-7e53):

> I also want to make sure it is ULTRA performant, SUPER friendly on performance, this is very important to me, so many mods do stuff that is not performant like updating things at a much higher frequency than is actually needed.

## The rule

The mod does its work when settings change and nowhere else. During play it adds zero per-tick, per-frame and per-pawn work, with one opt-in exception below. Any change that adds a tick hook, a `GameComponent`, a `MapComponent` or a postfix on something that runs every tick breaks this rule and needs a very good reason.

## Where the cost goes

| When | What runs | How often |
|---|---|---|
| Game start | Snapshot every ThoughtDef, MentalBreakDef, MentalStateDef and seeker NeedDef, classify each thought, apply the profile | Once. The time is logged as `Tuned N thoughts in X ms.` |
| Settings window closes | Rewrite the same defs from the snapshot | Once per close |
| Pawn gains a memory | Battle focus prefix | Only while battle focus is enabled |
| Every tick | Nothing | Never |

Rewriting defs means the game's own code reads the tuned numbers through the fields it already reads. There's no lookup, no dictionary and no extra branch in the hot path.

## The one patch

`BattleFocusPatch` prefixes `MemoryThoughtHandler.TryGainMemory(Thought_Memory, Pawn)`. That method runs when a memory is gained, which is an event, not a tick. The patch is installed with `harmony.Patch` only when the feature is enabled and removed with `harmony.Unpatch` when it is turned off, so a player who never uses battle focus runs the game unpatched.

Inside the prefix the order is cheapest first:

1. A `bool[]` lookup by `ThoughtDef.index`, precomputed on apply. Most memories exit here.
2. `pawn.IsColonist`.
3. `pawn.Drafted`, then `mindState.WasRecentlyCombatantTicks`, both field reads.
4. One float compare against the stage's mood effect.

No allocation, no string work and no LINQ.

## Settings UI

The UI only costs anything while the window is open.

- Sliders write into the in-memory profile. Defs are not touched until the window closes, so dragging a slider never re-tunes 900 thoughts.
- The Thoughts tab rebuilds its filtered list only when a filter changes, and draws only the rows on screen.
- Category counts are computed once when the catalog is built.
