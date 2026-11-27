# The TFTV page: structure and conventions

The wiki runs MediaWiki 1.34. The page is one long article (~250 KB, revision 17683 of 2026-02-09 when
this skill was written). Run `wiki_sections.py list` for the current outline; get conventions from the
page itself when in doubt, and match the surrounding section.

## Current structure (top level)

Header (quotes, "Last update", version, "Wiki Pending") · Introduction (Phoenix Rising credits, Features,
Objectives) · Oneiric Delirium (Delirium Infection, treatment, Touched by the Void, Void Omens ruleset) ·
Exploration (Passenger modules) · Injuries, bionics and mutations · Diplomacy · Research · Infestation ·
Pandoran Evolution · Blood and Titanium · Legacy of the Ancients · Festering Skies (Hammerfall, Hatching,
Behemoth, Air combat) · Corrupted Horizons · Other changes (Base scan, Medbay, Marketplace, Poison, Acid,
Goo, Food and Mutagen harvesting, Shock) · Compatibility with other mods · \*\*\*spoilers re Revenant
rewards\*\*\* · Classes (factions, then one `==` per class with one `===` per skill) · Appendix (Weapons
tables by type, Armor head/body/vests/legs, Mutations, Bionics, Reworked Vehicles and modules per vehicle,
Diplomacy table, Training Centres, Void Omens list).

The page explains each system in prose near the top and keeps the reference tables in the Appendix.
Keep that split: rules and explanations go in the system's section; stat rows go in the Appendix table.

## Default plan for content the page lacks

Adjust these to what the release actually contains. Each new section starts with one or two sentences
saying what it is and when it applies.

- `=Campaign settings=` after Introduction: difficulty levels (Story Mode to Etermes) and what each
  changes; new-game options and mod options, each with its in-game name, its default, and whether it can
  be changed mid-campaign. A table is fine: Option · Default · When it can change · Effect.
- Topics in "Wiki Pending" (Festering Skies and Behemoth, Human Enemies, Revenants, Special starts,
  Touched by the Void, Legacy of the Ancients rework) go into their existing sections, or into new `=…=`
  sections next to them (e.g. `=Human enemies=` after Pandoran Evolution, `=Revenants=` after it, without
  the spoilers, which stay in the spoiler section).
- `==Vanilla bug fixes==` at the end of Other changes: a bulleted list grouped by `===Geoscape===` /
  `===Tactical===`, one short player-facing line per fix, no patch numbers.
- `=Aircraft Rework (Beta)=` after Festering Skies: how to enable it (new-game prompt; the game restarts;
  saves remember the mode), then `==` subsections per system (Aircraft and modules, Vehicles and ammunition,
  Drills, Personnel, Base activation and Outposts, Training, Incidents, Affinities, …). Where the beta
  replaces a main-branch rule described elsewhere, add one line at that place:
  `''With the [[#Aircraft Rework (Beta)|Aircraft Rework Beta]]: …''`.
- Appendix tables get new rows for new items (e.g. Hazmat and Ablative vests replace the old vest rows);
  new drill or affinity reference tables go under Aircraft Rework, or in its subpage if there is one.

## Wikitext conventions used on the page

Headings: `=Top=`, `==Sub==`, `===Subsub===`, no spaces inside the `=`. Avoid two headings with the same
text (the Appendix already repeats "Void Omens" and "Diplomacy"; anchors then reach only the first).

Text: `<b>…</b>` and `<i>…</i>` are used more than `'''`/`''` (both work). `<br>` for line breaks. Bullets
are `*`, numbered lists `#`. "Vanilla" is capitalised. Write in second person ("you"), present tense.
**No Markdown**: no `**bold**`, no `[text](url)` (external links are `[https://… text]`), no backticks.

Comparisons: state the TFTV rule, then the vanilla one, e.g. `Medbay heals 16 HP per hour (Vanilla: …)`
or a sentence such as "In Vanilla, …; in TFTV, …". The Objectives section shows the house style.

Class skills (copy the exact shape):
```
===<span id="Quick Aim"></span>[[file:quick_aim.png|40px|link=|Quick Aim]] Quick Aim===
<code> The AP cost of the next shot with proficient weapon is reduced by 1, but suffer -30% Accuracy. Limited to 2 uses per turn</code><br>
<code>AP: 0, WP: 3</code>

Prose on how to use it.
```
The `<code>` line is the in-game description (take it from
`TFTV_AbilitiesEffectsStatusesTactical_Localization.csv` / `PR_BC_Localization.csv`); the second line has
the costs, or `Passive Skill`.

Tables: `{| class="wikitable sortable"` … `|}`; rows start `|- <span id="anchor">`; cells
`| style="text-align:center;" |value`; the item cell is `[[File:<icon>.png|70px]]` + `<b>Name</b>`. Keep
the column order of the table you edit, and fill every column (use `-` for none). Common inline icons:
`[[File:materials.png|…]]`, `[[File:tech.png|…]]`, `[[File:time.png|…]]`, `[[File:proteanmutane.png|…]]`,
`[[File:livingcrystal.png|…]]`, `[[File:orichalcum.png|…]]`; factions `[[File:1pp.png|30px|link=Phoenix Project]]`,
`[[File:NJ.png|30px|link=New Jericho]]`, `[[File:Anu.png|30px|…]]`, `[[File:Synedrion.png|30px|…]]`.
Copy the size and link parameters from a neighbouring row.

Links: to vanilla reference pages with anchors, e.g. `[[damage_types#Shred_Damage|Shred]]`,
`[[Combat#burst|Burst]]`, `[[New_Jericho_Research#Armor_piercing_technology_.28900.29|Armor piercing technology]]`
(legacy anchor encoding: `(` → `.28`, `)` → `.29`). Within the page: `[[#Section name|text]]` or
`[[Terror_from_the_Void#Assault|here]]`. `wiki_check.py` verifies every one of these.

Images: `[[File:Name.png|thumb]]` beside a section, as in Oneiric Delirium. Only files already on the
wiki render. A new file must be uploaded first: use the file name from `Dist/Assets/Textures/` and list it
in `wiki/IMAGES.md` (`File name · source path in repo · used in section · why`).

Spoilers: story outcomes, reward questlines and final-mission details belong in a section whose
heading says spoilers (like `=***spoilers re Revenant rewards***=`). Rules sections should not give away
story beats.
