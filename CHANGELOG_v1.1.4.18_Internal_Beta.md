# TFTV v1.1.4.18 — Internal Beta 20261122

Changes since the last internal beta, **v1.1.4.17** (Internal Beta 20261121).

This is an internal test build. None of it has been playtested yet. There is one new line of player-facing text ("RESISTED").

## Installing over v1.1.4.17

Replace `TFTV.dll` and `meta.json`, and copy this changed asset:
- `Assets/Localization/TFTV_AbilitiesEffectsStatusesTactical_Localization.csv`

TFTV settings are not reset by this version.

## What to send back

`TFTV.log` (in the TestMod/TFTV folder) and `Player.log` after missions with enemies. Haven defences are the most useful this time, then any mission with Chirons or Acherons, and with Harsh Attrition on, any mission where you use Mind Control or Induce Panic on Pandorans. The changes below say which log lines to look for.

---

## Moving next to unseen enemies

**Move and jump abilities no longer give away where unseen enemies are**
- Moving, dashing or jet-jumping onto a tile, or through it, used to be refused when an enemy you couldn't see stood there, so you could feel out hidden enemies with the move cursor.
- Now the move looks possible. On the ground, the unit stops on the last free tile before the hidden unit. A jump lands on the nearest free tile within 2.5 m. Either way the unit that blocked it becomes located.
- TFTV.log: `[HiddenBlockers] <unit> <ability> to <spot>: runs into unseen <enemy> ...; stops at <spot>`, `... lands at <spot>`, and `bumped into <enemy>, still unseen; it is now located`.

## Enemy squads

**Chirons cost less; how many Chirons and Acherons a mission gets is limited instead**
- Worm and Goo Chirons now cost 150 (agile and stomping legs) or 160 (heavy legs), down from 300–310. Mortar, acid mortar and crystal mortar Chirons keep their costs (350–550).
- Acherons are back to their base-game costs: Acheron 120, Acheron Prime 150, Asclepius and Achlys 225, their Champions 275.
- New limits per mission, reinforcements included:
  - at most 3 Chirons, and at most 2 of them throwing worms;
  - at most 2 Acherons, and at most 1 of them throwing Mist (Acheron or Acheron Prime).
- When a limit is full the points go to other units, so the enemy force is as strong as before.
- TFTV.log: `<template> reached limit of allowed spawns (<limit>: N of M)`.

## Enemy AI

**Haven attackers go after people before buildings**
- In haven defences the attackers spent most of their attacks on the haven's structures, even with soldiers in reach: buildings are big, close and easy to hit.
- Attackers now hit structures and civilians only when they can't attack any of your soldiers or the haven's defenders.
- TFTV.log: `TFTV AI haven attackers: <unit> leaves the haven's structures and civilians: it can go after <target>`.

**The AI takes a clear best option**
- The AI drew at random among every option scoring at least 75% of the best, so a clearly weaker option still got picked now and then. It now draws only among options within 10% of the best, and otherwise takes the best.
- Overwatch instead of shooting is mostly unaffected: the logs show Overwatch usually scored higher than the shot outright.

## Abilities

**Ability targets no longer flicker**
- Whether a target in line of sight could be picked for Induce Panic, Mind Control, healing and similar abilities could change from one moment to the next with nobody moving, because the check followed the target's idle animation. It now uses the target's standing position.

## Harsh Attrition

**Paralysis no longer gives away hit points**
- On a Pandoran whose real hit points you don't know, the paralysis status now reads like "12/?" instead of "12/180", on the health bar and in the tooltip.

**Mind Control and Induce Panic work on unknown willpower**
- While you don't know a Pandoran's real willpower (vivisection and close study), Mind Control, Induce Panic and similar abilities are no longer greyed out on it.
- If its willpower is too high, the ability is resisted: the caster spends the Action Points but keeps the Will Points, nothing else happens, and "RESISTED" shows over the target.
- TFTV.log: `[HarshAttrition] <caster>'s <ability> on <target> is resisted: target Will Points X against the caster's Y`.

## Known and untested

- Nothing in this build has been playtested.
- A resisted cast skips the ability's animation and returns control straight away. Tell us if the game seems stuck after one.
- Tell us if an Acheron mission ever shows more than 2 Acherons outside story missions.
