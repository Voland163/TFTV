# TFTV v1.1.4.15 — Internal Beta 20261119

Changes since the last internal beta, **v1.1.4.14** (Internal Beta 20261116). Most of them answer tester reports on v1.1.4.14 and the builds before it.

This is an internal test build. None of it has been playtested yet. There is no new player-facing text.

## Installing over v1.1.4.14

Replace `TFTV.dll` and `meta.json`. No asset changed. TFTV settings are not reset by this version.

## What to send back

`TFTV.log` (in the TestMod/TFTV folder) and `Player.log` after a mission with enemies. Haven defences and Rescue Soldiers missions are the most useful this time. The changes below say which log lines to look for.

---

## Defenders and rescuees

**Haven defenders with guns fight from cover**
- On a New Jericho haven defence the defenders stood in the open where they spawned on turn 1, and when they fell back after shooting they often ended up behind no cover at all.
- Haven defenders carrying a gun now:
  - take cover on turn 1, facing where the attackers deploy, before any Pandoran is seen;
  - never advance or dash, stay within 8 m of where their turn began, and never move closer to the enemy;
  - move to a covered spot they can shoot from when they have no shot, or watch from cover;
  - go back to their starting spot (or better cover) after stepping out to shoot.
- Defenders without a gun keep charging in: the Anu berserkers did the haven's best work with their hammers.
- They can't move forward to help civilians attacked outside their range.
- TFTV.log: `TFTV AI defender: <unit> (haven defender) defends`, `takes position ... cover <n>`, `goes back after its shot`.

**Rescuees defend instead of charging, and start alerted**
- On Rescue Soldiers missions the soldiers waiting for rescue ran 10–15 m into the open toward the raiders and were cut down. Later they stood idle with bandits in plain sight.
- They now follow the same rules as haven defenders above.
- They start the mission alerted. Before, they ignored enemies until one came within 16 m, shot at them or hurt them.
- Their shots from the open count for less, so moving to a covered firing spot to shoot comes first.
- TFTV.log: `TFTV AI defender: <unit> (rescuee) ...`.

**Defenders fall back only behind cover**
- Haven defenders and rescuees now fall back only to a spot with at least low cover toward the enemy. Being out of sight is no longer enough. With no such spot in reach, they return to where they started or stay put.

**Haven defenders and rescuees start every mission alerted**
- Vanilla already did this for haven defenders. TFTV now makes sure of both at mission start. TFTV.log: `TFTV AI alert: <faction> (...) starts alerted`.

## Enemy AI

**Units that aren't alerted no longer act on enemies they can't attack**
- An AI unit that isn't alerted yet ignores soldiers entirely; it only goes for civilians and crates. Some TFTV AI still acted on soldiers its side could see. Now:
  - it no longer casts Armor Break or Quick Aim with no shot to follow, or holds back AP or skips the Mist for such a shot;
  - the squad planner gives it no suppress order or focus-fire target, so it no longer calls out orders it can't carry out;
  - an ambusher is no longer sprung by a soldier it couldn't attack. An Arthron sprung that way just wandered off.
- Ambushers now hide from every soldier their side can see, not only from the ones they personally know about.
- The AI only avoids Overwatch cones of soldiers its side has spotted, not of soldiers it has never seen.

**Quick Aim is followed by a shot**
- In the logs every Quick Aim after acting was followed by Overwatch, never a shot. The AI counted the next shot at the weapon's full cost and found it couldn't afford it. It now counts the discounted cost.

**Pandorans in the Mist**
- A Chiron with its mortar trained on a soldier walked to the Mist at 67% health and never fired. The "keep AP for a shot" check now looks at every ranged weapon, not just the one selected.
- A unit that is barely scratched (a Brute Alpha 7% down) no longer leaves an adjacent soldier to go heal.
- A Pandoran next to an enemy attacks rather than walking to the Mist.
- Once in the Mist and still hurt, a Pandoran leaves it only as a last resort.
- Mindfraggers and worms (no weapons) no longer count as disarmed and walk to the Mist at full health.

**Mindfraggers**
- A Mindfragger's death acid no longer hits other Mindfraggers. They ride soldiers standing side by side, so one dying used to kill the next and free its soldier, in a chain. TFTV.log: `[Mindfragger] <name>'s death acid spares <name>`.
- Willpower no longer breaks a Mindfragger's control or costs it anything.

**Arthron shields** face the nearest enemy in 3D. A soldier one floor up counted as close as a berserker next to the Arthron, and the shield turned to face upstairs.

## Fixes

- **Mission summary:** an enemy kept for good by Void Omen 32 showed on the summary, with Officer Class XP. Units with no Phoenix character behind them are now left off.
- **Void Omen 21:** empty body-part slots (a Brute with no carapace) were "mended" every turn. Only slots with a part in them are restored now.
- **Void Omen 27** ("raiding penalties doubled, better trade with friendly leaders") now only rolls after Phoenix has raided havens 3 times. TFTV.log: `[VO27] Phoenix raid #<n>`.
- **Units killed by a fall:** when the floor under a unit gave way and the game found no landing spot, the unit died however short the drop. It now lands on the nearest safe spot. TFTV.log: `[FallRescue] ...`.
- **Geoscape events that tire or hurt the soldiers on your aircraft:**
  - A stamina loss now applies in full to every soldier aboard. Before, it was a pool handed out a point at a time.
  - The soldier damage pool is raised from 30 to 120, the aircraft damage from 40 to 100. No soldier is taken below 1 HP.
  - TFTV.log: `[EventStamina]`, `[EventDamage]`.
- **Occult affinity base-attack warning:** the early warning now gives the expected arrival (days until the attack is launched plus the attackers' travel time). It is only given when nothing would stop the attack. A second message gives the real arrival time once the attack is launched. TFTV.log: `[OccultAffinity]`.
- **Marketplace:** with more than 7 offers, the list threw an error and could stop refreshing. Fixed.
- **Health bars stuck open:** an enemy's expanded health bar (body-part picture and status row) could stay up after a grenade. It now closes after 4 seconds. TFTV.log: `[Healthbar]` says what it was stuck on.
- **Harsh Attrition:** human guns carried by Tritons (Gauss rifles, shotguns, pistols) are no longer rolled; their tooltip shows the normal values.
- Fewer warnings in the log about aircraft modules in the faction storage (Aircraft Rework).

## Known and untested

- Nothing in this build has been playtested.
- The turn-1 cover for defenders points at the attackers' deploy zones. If defenders take cover facing the wrong way, tell us the map.
- Haven defenders with guns stay near their posts; tell us if they now feel too passive while the haven's civilians are attacked.
