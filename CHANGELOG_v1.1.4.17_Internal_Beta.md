# TFTV v1.1.4.17 — Internal Beta 20261121

Changes since the last internal beta, **v1.1.4.16** (Internal Beta 20261120). Both answer tester reports on v1.1.4.16.

This is an internal test build. None of it has been playtested yet. There is no new player-facing text.

## Installing over v1.1.4.16

Replace `TFTV.dll` and `meta.json`. No asset changed. TFTV settings are not reset by this version.

## What to send back

`TFTV.log` (in the TestMod/TFTV folder) and `Player.log` after missions with enemies. Haven defences are the most useful this time, and any mission where a soldier stands on something that gets destroyed (crates, walkways, roofs). The changes below say which log lines to look for.

---

## Fixes

**Units no longer die from short falls**
- In Favour for a Friend a soldier standing on a crate, 0.6 m up, died at full health when the Scarab drove through the crate. When a fall finds no landing spot the base game deals damage equal to the unit's current health.
- The fix for this shipped in v1.1.4.15 but never ran: it skipped every real fall. It now works. A unit whose floor gives way lands on the nearest safe spot and takes normal fall damage (10 HP per metre, none for a level spot).
- TFTV.log: `[FallRescue] <unit> falls from ...` for every fall, saying whether the game's own landing was used or a fallback spot.

## Haven defenders

**Haven defenders move around the haven, but not into the open**
- A Priest stepped out to shoot every turn and then stepped back to the same open spot, with no cover at all. A Berserker closing in on the enemy moved into the open.
- Haven defenders with guns now:
  - after a shot, step back only to a spot with cover, or stay put if there's none in reach;
  - move up to 12 m a turn, toward the enemy as well, but only to a spot with at least low cover;
  - when moving up to get within range, never cross open ground a known enemy can see.
- Soldiers waiting for rescue keep the stricter rules.
- TFTV.log: `TFTV AI defender: <unit> (haven defender) defends: no advance, moves within 12m of ..., nearer the enemy only behind cover`, and `goes back after its shot ... (better cover)`.

## Known and untested

- Nothing in this build has been playtested.
- Tell us if haven defenders now get stuck because no covered spot is in reach.
