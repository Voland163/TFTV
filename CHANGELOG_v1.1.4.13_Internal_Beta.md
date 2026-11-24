# TFTV v1.1.4.13 — Internal Beta 20261115

Changes since the last internal beta, **v1.1.4.12** (Internal Beta 20261114). Most of them answer the first tester reports on that build.

This is an internal test build. None of it has been playtested yet. There is no new player-facing text.

## Installing over v1.1.4.12

Replace `TFTV.dll` and `meta.json`. No asset changed. TFTV settings are not reset by this version.

## What to send back

`TFTV.log` (in the TestMod/TFTV folder) and `Player.log` after a mission with enemies. The changes below say which log lines to look for.

---

## Enemy AI

**Melee enemies find their way down**
- Arthrons without a shield, and other melee-only units, now judge their plain advance by walking distance instead of straight-line distance. Before, a unit on an upper floor above the fight already counted as "close" and never looked for the stairs (an Arthron spent four turns on a third floor in a haven defence).
- Units with a gun they can fire still use the straight line, as before.

**Gunners don't run up to things they can't hurt**
- A gunner with no shot only moves to get a line of fire on an enemy its gun does at least 10 damage per shot to, after armour. Before, a Priest with a hand cannon ran up to a Kaos Buggy it could barely scratch, with no AP left to shoot. When nobody near qualifies, TFTV.log says so: `TFTV AI: <unit> has no shot from where it stands, and its <weapon> can't hurt anyone near (...)`.
- A gunner's advance no longer likes ending within 3.5 m of a known enemy.

**Class skills**
- Skills that cost Will Points (Gun Kata and the like) are only used while the unit keeps at least half its WP. WP is also its buffer against panic and pays for Mind Control and Dash; Gun Kata was spending it on every shot.
- Quick Aim waits until the unit has done something this turn. Used first thing, it was often followed by a move or Overwatch, and Overwatch used up the buff. It also no longer misses shots it does make affordable (a 4.75 AP shot with 6.1 AP left).
- When a class skill isn't used, TFTV.log says why, once per unit, skill and reason each turn: `[ClassSkills AI] <unit> won't use <skill>: ...` (WP reserve, target has more WP, target kept for Mind Control...).

**Faster AI turns**
- Dash checks the walk to the nearest enemy from every tile it could reach, which meant one pathfind per enemy per tile. It now only paths to the enemies that could still be the nearest, with the same result. Worms and the new melee advances use the same shortcut.
- At the start of each faction's turn, TFTV.log reports the saving for the previous turn: `TFTV AI closest path: N positions scored with X pathfinds instead of Y`. If that line ever ends with `... walks came out shorter than their straight-line bound`, please send the log: the shortcut has gone wrong.

## Fixes

- Bottle test: when a broken faction had no exit zone of its own, making one threw an error ("Value cannot be null") and the units could not leave. Seen on StoryAN0 and a Phoenix base infestation.

## Known and untested

- Nothing in this build has been playtested.
- Walking distance makes melee units' advance planning more expensive. Watch the `TFTV AI closest path` lines and how long Pandoran turns take.
- The 10-damage threshold and the 3.5 m distance are first guesses.
