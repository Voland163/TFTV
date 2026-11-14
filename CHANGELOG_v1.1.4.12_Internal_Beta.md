# TFTV v1.1.4.12 — Internal Beta 20261114

Changes since the last internal beta, **v1.1.4.8** (Internal Beta 20261108). Testers who had a build in between (dated 20261111) already have part of the enemy AI section.

This is an internal test build. Almost all of it is unplaytested. There is no new player-facing text.

## Installing over v1.1.4.8

Replace `TFTV.dll` and `meta.json`. No asset changed. TFTV settings are not reset by this version.

## What to send back

`TFTV.log` (in the TestMod/TFTV folder) and `Player.log` after a mission with human enemies or a Scylla. TFTV.log now has one `[AI decide]` line for every enemy decision, with the scores of its options, so "why did it do that" can be answered from the log. The new features below say which lines they write.

---

## Enemy AI

**Enemy soldiers use the class skills players have**
- Enemies now get their class skills at the same level a player soldier does (main row at levels 2, 3, 5, 6 and 7; the two class perks at 3 and 6). Logged as `[ClassSkills] <unit> (<class> level N): ...`.
- New for enemies: Quick Aim, Kill'n'Run, Quarterback, Ready for Action, Aimed Burst (Assault); Hunker Down, Jetpack Control, Boom Blast, Rage Burst (Heavy); Armor Break, Gunslinger, Marked for Death (Sniper); Gun Kata, Killer Instinct, Adrenaline Rush (Berserker); Induce Panic, Biochemist (Priest); Remote Deployment, Remote Control (Technician); Vanish (Infiltrator). Kill Zone and Lay Waste are not given to enemies.
- Each active skill has an AI action that only fires when it pays off this turn:
  - Quick Aim only when it buys an extra shot; Armor Break only against a target with 15+ armour; Boom Blast when the target is only in reach with the +50% range, or a launcher gets an extra shot; Adrenaline Rush at the start of the turn near enemies with a one-handed weapon.
  - Hunker Down instead of ending a turn when the unit has acted, is seen, and has no shot left.
  - Aimed Burst (assault rifles), Gunslinger and Gun Kata (handguns, Gun Kata without moving), Rage Burst (two or more enemies in the cone, or a vehicle).
  - Marked for Death on the enemy the squad is focusing fire on, keeping the AP for the sniper's own shot. Induce Panic on someone the priest out-wills, never on the soldier being saved for Mind Control.
  - Remote Control on a turret with something to shoot; the turret fires at once.
  - Kill'n'Run's dash and Vanish move the unit into cover or out of sight.
  - Heavies can jet jump (or use Jetpack Control) to a spot and shoot.
  - Special shots, heals and Remote Control log `[ClassSkills AI] ...`.

**Healing**
- The AI heal now uses whichever heal suits the patient (medkit, stimpack, Field Medic, the technician's arms), and counts disabled limbs, so technicians repair limbs and stimpack-only units heal.
- Heals pick a spot in cover toward the enemies who can see it, with AP left for the heal, instead of the tile next to the patient.

**Skills the AI overused**
- Electric Reinforcement: only when two or more allies in reach lack it and are seen by an enemy (it fired every turn before).
- Neural Disruption (Exo helmet): once per unit per turn, never on a soldier already disrupted, and only on one with WP abilities they can still pay for (before, it counted plain shooting and kept casting until WP fell below half).
- Demolition Man (Juggernaut torso): only when the unit has no shot and a known enemy within 20 m is hidden from it (it fired every turn once alerted).

**Bionics**
- The Exo leap-and-shoot no longer outweighs walking and shooting, and units holding a position don't leap into the open.
- Shadow Step units no longer route around Overwatch cones they don't set off.

**Scylla**
- She doesn't ready her guns when a friendly bigger than a worm (Arthrons, Tritons, Sirens, Sentinels...) stands in their cone.
- While her guns are ready, friendlies don't end a move in the cone. Logged as `[ScyllaGuns] ...`.

**Deployment and morale**
- Enemy spawn spots: units start out of the squad's sight when the zone allows it (whole body checked, stealth included), and spread at least 2 m apart. Logged as `[SpawnSpots] ...`.
- Bottle test: a human or Pandoran faction at war with you breaks when it is below 15% of its starting strength and below half of yours. Its units then run for the exits (exit zones are added if the mission has none). Kill-objective targets never run; story bosses may. Not on missions with unlimited reinforcements. Logged as `[Bottle] ...`.

**Behaviour**
- Sniper nests: long-range gunners with nothing to shoot climb to covered high ground facing the enemy and watch from there.
- Ambushes: unseen units with nothing to shoot wait near your objectives (exits, consoles, unopened crates).
- Protect: the squad shoots first at soldiers threatening its leader or sniper; burst guns suppress soldiers who aren't on overwatch.
- Melee units that can't reach anyone this turn approach through spots you can't see, instead of ending their move in the open.
- Gunners with no shot advance to a spot with a line of fire, instead of shuffling behind a wall.
- The suppress move no longer walks through overwatch on its way.
- Mind control hold: the soldier a priest is going to take over is re-checked before each enemy acts, so others no longer shoot him first.
- The AI no longer aims at the Armadillo's front lights (0 HP, 0 armour, and the player can't pick them), which soaked most of its shots.

## Fixes

- Panic: a panicked unit no longer shows action points it can't use (the vanilla panic run halved AP and left the rest).
- Mission summary: units you mind-controlled (civilians, enemies) no longer appear with XP.
- Scavenging: empty guns now trigger picking up weapons and ammo, and empty dropped weapons are no longer taken.
- Void Omen 33: the permanent third-augment perk survives Delirium perk recovery.
- Harsh Attrition: an egg's or sentinel's surveillance range stays hidden until researched; a crash when a hit landed on a body part of a creature destroyed that frame.

## Known and untested

- None of the enemy class skills, heal changes, Scylla guard, bottle test, nests, ambushes or spawn changes has been playtested.
- Demolition Man: unverified that the AI actually walks through walls while it is active.
- Remote Deployment relies on the game's existing turret-throw AI.
