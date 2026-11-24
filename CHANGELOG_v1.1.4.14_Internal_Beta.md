# TFTV v1.1.4.14 — Internal Beta 20261116

Changes since the last internal beta, **v1.1.4.13** (Internal Beta 20261115). Most of them answer tester reports on that build and the one before.

This is an internal test build. None of it has been playtested yet. There is no new player-facing text.

## Installing over v1.1.4.13

Replace `TFTV.dll` and `meta.json`. No asset changed. TFTV settings are not reset by this version.

## What to send back

`TFTV.log` (in the TestMod/TFTV folder) and `Player.log` after a mission with enemies. The changes below say which log lines to look for.

---

## Enemy AI

**Enemies spread their fire away from the player's vehicle**
- In the vehicle logs about three in four aimed enemy attacks went at the player's vehicle, mostly its tyres. The AI counted damage in plain points, so a rifle burst that took 60 of a buggy's 750 health was worth as much as the same burst into a soldier, for whom it is half their health.
- A shot at a vehicle now scores √(typical soldier health ÷ the vehicle's current health) of what it did before, never more than before. "Typical" is the median maximum health of the other units on the vehicle's side.
  - A full Kaos Buggy against 150-health soldiers is worth about 0.45 of before, an Armadillo about 0.35. Rifles mostly turn back to the soldiers; heavy weapons still take the vehicle on.
  - Once a vehicle is down to soldier health it is scored as before, so finishing one off is unchanged.
- TFTV.log, once per vehicle per turn: `TFTV AI vehicle targets: shots at <vehicle> (<health> health; its side's soldiers ~<n>) score x<factor> this turn`, and a per-turn count of scaled shot scores. Tell us if vehicles now feel ignored, or still soak most of the fire.

**Stomp is back, without friendly fire**
- Enemies use Stomp again. It was taken away because it hurt their own side; now the AI never stomps where the blast would hit a friendly unit (or one of its own that the player has mind-controlled).
- The same check now covers the other attacks centred on the user: the Chiron's area stun, the spider drone's blast, and the Fire, Acid and Poison worms' explosions. Before, the Chiron's stun only scored a little lower per friendly in range.

**Umbras never flee**
- When the Pandorans fail the bottle test, every other unit still runs for an exit; Umbras stay and fight. They still count toward the Pandorans' strength.

## Fixes

- **Void Omen 32 ("mind-controlled units switch sides after 2 turns")**
  - It also switched haven defenders and civilians that had joined you: the game puts them under your control with a kind of mind control. It now only switches units taken from an enemy.
  - A unit it switched had no AP for that turn: ending mind control at the start of your turn set its AP to 0. It now keeps its AP. TFTV.log: `VO32: <unit> keeps its <n> AP after switching sides`.
- **Harsh Attrition: health bars of Pandorans you haven't studied.** The hidden bar was switched off, and the rest of the health bar collapsed into its place: a status icon (Marked Watch on a Siren) drawn huge and floating, and the body-part doll floating next to an empty spot. The bar is now invisible instead, so everything else stays where it normally sits. Please send a screenshot if a health bar still looks wrong.

## Known and untested

- Nothing in this build has been playtested.
- The vehicle scoring strength (square root) is a first guess.
