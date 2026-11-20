# TFTV v1.1.4.16 — Internal Beta 20261120

Changes since the last internal beta, **v1.1.4.15** (Internal Beta 20261119). Most of them answer tester reports and playtests on v1.1.4.15.

This is an internal test build. None of it has been playtested yet. There is no new player-facing text.

## Installing over v1.1.4.15

Replace `TFTV.dll` and `meta.json`. No asset changed. TFTV settings are not reset by this version.

## What to send back

`TFTV.log` (in the TestMod/TFTV folder) and `Player.log` after missions with enemies. Haven defences, nests, missions with an objective to reach (consoles, rescues) and missions with Mindfraggers or worms are the most useful this time. The changes below say which log lines to look for.

---

## Haven defenders

**Haven defenders close in and shoot**
- In the Anu haven against the Purists the defenders "did nothing": the Purists stayed 25–36 m away and the defenders, held to the rescuees' rules, never moved closer. They ended turns doing nothing or set Overwatch, one of them with a shotgun.
- Haven defenders with guns now:
  - when no known enemy is within their guns' effective range, move up, cover to cover, up to 12 m a turn;
  - once in range, hold and fight from cover as before;
  - shoot rather than set Overwatch. A New Jericho sniper on a roof set Overwatch instead of shooting a Triton 14 m away;
  - move to a spot with no line of fire only to get out of the open. Defenders kept moving between blind spots behind high cover and never fired.
- Rescuees keep the stricter rules.
- TFTV.log: `TFTV AI defender: <unit> (haven defender) closes in: nearest known enemy ... beyond its <n>m range`.

## Enemy AI

**Ambushers spread out**
- On Firebird 6 of 9 Pandorans lay in wait around the one console, within a few metres of each other, and spider mines and Scarab rockets caught them together.
- Now at most 2 units wait at any one objective, and they keep at least 5 m apart. The rest fight as usual. TFTV.log: `TFTV AI ambush: <unit> doesn't lie in wait: the place it could cover already has 2 ambushers`.
- An ambusher spotted by a unit its side didn't know about (a spider drone found two on Firebird) used to spend the turn walking to random waypoints. Its side now learns where that unit is, and it goes for it. The `springs its ambush` line adds `its side now knows where <unit> is`.

**Mindfraggers**
- They no longer hang back one leap short. Their advance looked for the nearest spot no soldier could see, however short the step, and ended the turn with most of its AP; two sat 15 m out for two turns. A hidden spot within next turn's leap of a soldier now comes first. With none, a spot in view but within leap range beats hiding out of reach.
- A Mindfragger whose leap is stopped by a newly revealed soldier carries on to the same spot and mind controls as planned, instead of giving up ("Invalid position"). It stops if it was hit on the way, its target is gone or already controlled, or it can't make it with AP left for the mind control. TFTV.log: `[Mindfragger] <unit>'s leap at <soldier> ... carries on` or `... decides again from there`.
- A Mindfragger no longer reserves a soldier it has no walk to. The rest of its side left that soldier alone while the Mindfragger had no mind control to offer.

**Worms no longer explode next to their own mind-controlled units**
- A Poisonworm crawled up to an Arthron under the player's mind control and blew it up. The check meant to prevent this never worked for any worm explosion; it does now. It also stops a worm going off next to its own side. TFTV.log: `[SelfBlast] <unit> holds ...`.

**Pandorans only walk to Mist they can reach (Void Omen 21)**
- A Brute Alpha and a Tyrant walked "toward the Mist" 12 times in a nest and never got into it: the Mist they aimed at had no floor they could reach. They now head only for a spot in the Mist they can stand on and walk to.
- A walk toward Mist that can't be reached this turn no longer beats an attack. A Brute Alpha at 71% HP gave up a shot and a strike to walk 11 m, then walked back. Stepping into Mist it can reach this turn still comes first, as before.
- TFTV.log: `TFTV AI mist: <unit> ... heading for <spot>`, or `has Mist nearby ... but no spot in it to walk to`.

**Faster turns for melee units**
- A Triton that lost its rifle took 37 seconds to decide its move: melee units measure distance by walking routes, and it worked out over 3,000 of them. Each unit now works out at most 300 routes per decision and estimates the rest. TFTV.log: `TFTV AI closest path: <unit> hit the cap of 300 walks in one decision`.

## Fixes

- **Haven leader names in events:** a leader's surname could show as `[HavenLeaderSurname]` when the stored name had a stray space. The name is now read with extra spaces removed.
- **Phoenix base tooltip:** hovering a base with more facilities than the tooltip had icon slots (seen after a base upgrade) threw an error and left the rest of the tooltip blank. TFTV.log: `[BaseTooltip]` the first time it adds slots.

## Known and untested

- Nothing in this build has been playtested.
- Haven defenders closing in may now step into danger more often. Tell us if they feel reckless, or still too passive.
- Mindfraggers will now sometimes end a turn in view of a soldier to be within leap range next turn.
