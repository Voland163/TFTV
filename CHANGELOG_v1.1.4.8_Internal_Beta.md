# TFTV v1.1.4.8 — Internal Beta 20261108

Changes since the last public release, **v1.1.4.7** (Hotfix 2 for Patch 26, 2026-10-09).

This is an internal test build. Much of it has had little or no playtesting, and new text is in English only.

## Installing over v1.1.4.7

Replace `TFTV.dll` and `meta.json` with the new ones, and copy these files from `Assets`:

**New**
- `Assets/Textures/LimbMalus.png`

**Replace** (all in `Assets/Localization/`)
- `PR_BC_Localization.csv`
- `TFTV_AbilitiesEffectsStatusesTactical_Localization.csv`
- `TFTV_AircraftRework_Localization.csv`
- `TFTV_BaseReworkPersonnel_Localization.csv`
- `TFTV_CharactersItemsFacilities_Localization.csv`
- `TFTV_Events_Localization.csv`
- `TFTV_GeoUIElements_Localization.csv`
- `TFTV_HintsGeo_Localization.csv`
- `TFTV_HintsTactical_Localization.csv`
- `TFTV_Incidents_ANU_Localization.csv`
- `TFTV_Incidents_ANY_Localization.csv`
- `TFTV_Incidents_NJ_Localization.csv`
- `TFTV_Incidents_SY_Localization.csv`
- `TFTV_LoreAndTips_Localization.csv`
- `TFTV_MissionObjectives_Localization.csv`
- `TFTV_Options_Localization.csv`
- `TFTV_Phoenixpedia.csv`
- `TFTV_Research_Localization.csv`
- `TFTV_VoidOmensAndODI_Localization.csv`
- `Vehicles.csv`

No other asset changed. TFTV settings are not reset by this version.

---

## New: Harsh Attrition (experimental new-game option)

Off by default; chosen when starting a campaign and saved with it.

**Wounds**
- A disabled arm keeps its hand but widens weapon spread: x2 per disabled arm for two-handed weapons, x2 for one-handed weapons once every arm is out. Melee damage drops by the same factor.
- A disabled head keeps half its Willpower, but costs 10 Perception and widens spread by 4/3.
- Hits on working parts add a temporary malus. Wound spread is capped at x4. Ignore Pain halves all of it; One-Handed Grip halves the disabled-arm spread instead.

**Pandoran variance**
- Every Pandoran's Hit Points, body-part HP and armour roll 60–90% or 110–140% of its template, never close to it. Willpower rolls −5 to +5; Perception and Speed roll too.
- Each Pandoran weapon gets its own damage roll, x0.85 to x1.15. It shows in the weapon's tooltip once you have seen the creature attack with it.
- Intelligence reports no longer introduce new mutations.

**Knowledge comes from research**
- An unknown species shows no health bar, stats, body-part names, damage prediction, innate statuses or detection circle, only the damage it takes.
- An **autopsy** reveals Hit Points, and body-part HP and armour, for creatures killed or captured in earlier missions. Until then values read as the species' base with a "?" (for example "130?").
- A **vivisection** reveals Will Points, movement, Perception, Stealth, Accuracy, abilities, innate statuses, the threat marker and the perception circle. Species that can't be vivisected get all of this from their autopsy.
- A body part your fire has got through to shows its real armour; its HP stays hidden until researched. A revealed part shows current/max on every screen, damage preview included.
- The per-species damage bonuses from vivisections and sentinel/spawnery autopsies are replaced by this, and those research texts say what they reveal.

**Umbras**
- HP rolls x0.4–1.6, Speed ±20%, weapon damage x0.5–1.5. Stats, HP and abilities stay hidden; only the name and the damage taken show.

**Other**
- Acheron healing regrows a disabled Arthron right arm as a claw.
- Disarmed humans and Tritons who flee come back the next turn with a fresh copy of their weapon if above 50% HP.

## New: Void Omens 20–37

Eighteen new omens, plus a change to Void Omen 19. Each new omen only enters the pool when the campaign has what it needs.

| # | Omen |
|---|---|
| 19 | **Changed:** the Pandorans' adaptive resistance now only builds up in missions where a Revenant could appear, and shares the Revenant cooldown |
| 20 | Pandorans see further at night and less far by day |
| 21 | Pandorans and characters with mutations heal and regrow limbs in Mist: 25% of max HP per turn and disabled parts restored; an Arthron that lost its right claw grows a new one |
| 22 | Flying through Mist raises maintenance, but no longer changes aircraft speed (Aircraft Rework only) |
| 23 | Base facilities build twice as fast, but Phoenix bases are attacked more often |
| 24 | Captive Pandorans yield 50% more Mutagens, but escape more easily in base attacks |
| 25 | Fallen havens can be scavenged, at a diplomatic cost with their former owners |
| 26 | Factions at war with us can appear as enemies at scavenging sites and in ambushes |
| 27 | Raiding penalties doubled, but better trade at havens with a friendly leader |
| 28 | Worms explode on death |
| 29 | Aligned factions explore nearby points of interest and share part of what they find |
| 30 | Several Revenants can appear in one mission |
| 31 | Points of interest in Mist take 50% longer to explore, but give higher rewards |
| 32 | Mind-controlled units switch sides for the rest of the mission after 2 turns (both ways) |
| 33 | Operatives can have 3 augments, but a third gives a permanent Delirium perk (kept after the omen ends) |
| 34 | Incidents occur more often, but all their rewards are halved (Aircraft Rework only) |
| 35 | Haven defense rewards fall with every civilian lost and structure destroyed |
| 36 | Badly injured operatives with high Delirium gain a Delirium perk |
| 37 | Pandoran stealth rises with Willpower, but they lose Willpower while undetected |

Console commands for testing (geoscape): `void_omen_force <id>` puts an omen in play; `void_omen_remove <id> [ban] [replace]` takes one out, optionally banning it for the campaign and rolling a replacement.

## Fire rework (always on)

Vanilla's tile-based fire is replaced.
- **Ground fire** hurts only while standing in it, and only Hit Points. Starting a turn on a burning tile ignores armour; walking into one is reduced by average body-part armour. It never sets anyone burning.
- **Incendiary attacks** set individual body parts burning. Each part keeps its own fire value (the higher one wins, no stacking), deals damage once when applied and then at the start of each turn, dropping by 10 each turn (20 with fire resistance). Healing items put out every part.
- Getting into a vehicle puts out burning, and passengers are safe from incendiaries, ground fire, Mist and goo.
- The AI treats burning tiles as dangerous places to end a turn, and its healers value burning allies.

## Enemy AI

**Human factions fight in stances** (missions where you have to wipe them out; haven and base attackers stay on the offensive)
- They start cautious: one or two scouts explore while the rest hold positions in cover. After first contact they turn Defensive or Offensive as the fight goes: losing the exchange of fire, a hurt or dead leader or heavy losses push them back; winning, numbers or several clear shots push them on. Each faction has its own temperament (Bandits reckless, New Jericho disciplined, Synedrion cautious, and so on).
- Holders pick covered firing posts, watch approaches from cover with overwatch, and keep their leader near the squad.
- A Defensive faction that keeps losing the exchange escalates: stealthy soldiers infiltrate to a flank, then a counterattack, then an all-in counterattack.
- **Peek and fall back:** long-range gunners step out, shoot, and step back into cover. Fall back only moves to a spot at least as well covered as the one it leaves.
- AI overwatch cones now follow the same rules as yours (cone width by weapon class, length to perception and range).
- Tritons and Sirens use the same holding, peeking and watching.

**Smarter squads**
- Before each turn, enemies near a leader look at what their team mates are about to do. Leaders are human gang leaders (influence 15 tiles) and lieutenants (10), Sirens (15) and Scyllas (25); Tritons always count. It is skipped above 16 AI units.
- **Focus fire:** these attackers share out their damage between your soldiers, concentrating on a kill when together they can make it and splitting up when they can't. The plan is redone each time one of them acts. A leader calls the target out when the first attack lands on it.
- **Suppression orders** (with the suppression option on): if your overwatch covers the path of an enemy that has to close in (a melee fighter, or a Siren going for a mind control), one or two gunners are told to pin that soldier down first, and act before the attacker.
- **Suppress action:** enemy gunners can fire to pin a soldier down rather than to hurt it, mostly against soldiers on overwatch.
- Orders are shown as floating text over the units involved ("Pin down that overwatch!" / "Covering!"); Pandorans shriek and chitter instead. Only units you can see show anything.
- Nobody attacks the soldier a Siren is about to mind control (unchanged, now part of the same system).

**Pandorans**
- Pandorans prefer to move into Mist, and prefer tiles deep inside it. Under Void Omen 21, hurt, disarmed or crippled Pandorans go looking for Mist to heal.
- Tritons spawn defensive Mist only when they can, and keep the AP for a shot before walking off to Mist.
- Wounded Tritons with a working blood-sucker bite to heal rather than keep shooting.
- Arthron shields face the nearest enemies instead of turning their back on an adjacent soldier because more distant ones share another direction.

**Other AI**
- Tritons and human enemies that lose their weapon or run out of ammo pick up a weapon or magazine from the ground if one is near. Tritons can now reload.
- Enemy snipers and other classes now actually get their class passives (Extreme Focus and the like); they were added too late to take effect.

## Suppression (option)

- Every bullet now counts along its whole flight: any enemy of the shooter it passes within 1.5 m of gets a suppression point, not just those next to where it hit. Hits still count once. Shots past your own team mates don't suppress them.
- **Moderate suppression pins a unit down:** its overwatch is cancelled and it can't return fire until its next turn. This works both ways: you can pin an enemy from range so it won't return fire or overwatch. The status description says so.

## Tactical

- Aimed Burst and other multi-burst abilities: the aiming prediction now covers every burst, not just the first.
- **Deployment phase:** soldiers placed above the floor are set down on it (no more fall damage); it also runs on base defense with the option off, and whenever part of the squad doesn't fit.
- With the deployment phase off (except base defense), enemies no longer spawn in sight of your squad already Alerted, and a Nest's eggs no longer all start hatching before turn 1.
- **Base defense deployment:** start spots no longer run through walls into sealed rooms on some base layouts; phase I spots stay close to the gantry; vehicles stay on the ground floor. Outside base defense, vehicles are no longer placed on rooftops.
- Human enemy squad hints appear when you deploy, for enemies already in sight.
- With unlimited deployment (Aircraft Rework or UnLimitedDeployment), base defense starts with everyone at the base selected.
- Secondary objectives no longer carry over between missions of the same type; hidden objectives that pay XP or SP now show in the battle summary and its total.
- Threat markers skip enemies whose movement is unknown, and now cover exploding worms and the Mindfragger leap.
- Gamepad R3 no longer opens the Anvil cheats panel.
- The limb hit malus option is switched off and removed until it is reworked.

## Geoscape

- Affinity choices are read correctly when loading a save (a Compute leader got aircraft speed instead of the haven attack warning).
- Mutoids no longer lead or speed up incidents.
- Bases stuck mid-activation now complete. After loading a save during an outpost activation, the outpost's visuals update correctly when it finishes (no more black circle).
- Queued geoscape pop-ups no longer get stuck and then all appear at once.
- Fixed a grey, unusable equip screen (and lost items) after redeploying a dismissed operative to another base. Saves already affected repair themselves.
- The aircraft action bar shows only fitted module squares.
- Vehicle/Mutog size-1 cheat works in both modes and with modules.
- The "crew ready" log message is no longer triggered or held back by hidden base personnel.
- The haven recruits button flashes when new recruits become available, until you open the list.
- Loading a save made without TFTV is refused with a message.

## Text

- Proofread English text throughout: typos, spelling, terminology and descriptions that contradicted the game mechanics (Frenzy, Mentor Protocol, Story Mode statuses, Co-Delirium, goo, Phoenixpedia entries and more). Other languages are unchanged.
- New strings in this version (Harsh Attrition, Void Omens 20–37, suppression, squad orders) are English only.
