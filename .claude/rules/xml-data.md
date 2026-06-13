---
paths:
  - "Main/_Module/ModuleData/**/*.xml"
  - "Main/_Module/ModuleData/characters/**"
  - "Main/_Module/ModuleData/factionmap/**"
---

# XML Data File Rules

## File Types
- **XSLT transforms** (`*.xslt`) — Modify vanilla XML at load time (see xslt.md rule)
- **New entity XML** (`characters/*.xml`, `dots_*.xml`) — Entities not in vanilla
- **JSON config** (`factionmap/*.json`) — Feature-specific data

## Culture NPC Naming Convention
Each culture has 26 notable NPCs in `characters/npcs_{culture}.xml`:
- `spc_notable_{culture}_0` through `_4b` — Merchants (10)
- `spc_notable_{culture}_5/_6/_7` — Preachers (3)
- `spc_notable_{culture}_8/_9` — Artisans (2)
- `spc_notable_{culture}_gl1/_10/_11/_gl4/_12/_13` — Gang Leaders (6)
- `spc_notable_{culture}_21/_22` — Rural Notables (2)
- `spc_{culture}_headman_1/_2/_3` — Headmen (3)

**TWO-LAYER REGISTRATION (mandatory):** NPCs with `is_template="true"` are only reachable when the culture's `<notable_templates>` block in `Main/_Module/ModuleData/dots_spcultures.xml` (or its XSLT override) lists them via `<template name="NPCCharacter.<id>" />`. Adding an NPC to `npcs_{culture}.xml` is necessary but **not sufficient** — both layers are required, or the engine ignores the new NPC and reuses an existing template (producing clone notables with identical names/traits).

To extend a pool (e.g. add a 3rd Rural Notable `_23` to support a higher notable-count target):
1. Define `<NPCCharacter id="spc_notable_{culture}_23" …>` in `characters/npcs_{culture}.xml`.
2. Add `<template name="NPCCharacter.spc_notable_{culture}_23" />` inside that culture's `<notable_templates>` block in `dots_spcultures.xml`.

The same applies to additional Preachers, Headmen, or any new notable beyond the base 26. Why: the engine populates the spawn pool from `<notable_templates>` (read by vanilla `NotablesCampaignBehavior` / `HeroCreator.CreateNotable`), NOT by enumerating `npcs_*.xml`. RCA: `docs/reviews/rca-cultural-feats-3pack-2026-05-31.md`. Memory: `feedback_notable_template_two_layer_registration`.

## Culture Attribute References
Culture XML attributes (`merchant_notary`, `artisan_notary`, etc.) must reference the FIRST NPC of each occupation type.

## Region Codes
Westeros (GoT) region codes, used for `lord_<CODE><clanN>_<lordN>` and settlement-id prefixes. Full culture/kingdom/allegiance table: [ADR-011](../../docs/adrs/011-westeros-culture-model.md).

`NO`=The North · `VA`=The Vale · `RV`=The Riverlands · `WE`=The Westerlands · `RE`=The Reach · `ST`=The Stormlands · `CR`=The Crownlands · `DO`=Dorne · `IR`=Iron Islands · `NW`=Night's Watch · `FF`=Free Folk (beyond the Wall) · `ES`=Essos (Dothraki / Free Cities)

**Provisional until the map lands:** settlement-id region prefixes are finalized when the user-provided Westeros map module arrives (the settlements + FactionMap-landmarks phase). Lord/hero id prefixes (`lord_<CODE>...`) use the codes above now.

## Config ID Cross-Reference (MANDATORY)

After writing ANY XML/JSON config containing culture, kingdom, or settlement IDs, cross-reference EVERY ID against this table before moving on.

### Culture StringIds (runtime values)

| Type | StringIds | Note |
|------|-----------|------|
| **Custom cultures** | `vale` (Arryn), `riverlands` (Tully), `stormlands` (Baratheon), `crownlands` (Targaryen), `ironborn` (Greyjoy), `nightswatch` | Use the region StringId |
| **Vanilla-base cultures** | `sturgia` (The North / Stark), `vlandia` (Westerlands / Lannister), `empire` (Reach / Tyrell), `aserai` (Dorne / Martell), `khuzait` (Dothraki / Essos), `battania` (Free Folk) | **Keep the vanilla engine StringId**; the display name is XSLT-renamed |

**Common mistake:** Writing the GoT lore/region name for a **vanilla-base** culture. The `culture=` value for the North is `sturgia` (NOT `north`/`stark`); the Westerlands is `vlandia` (NOT `westerlands`/`lannister`); the Reach is `empire`; Dorne is `aserai`; the Dothraki are `khuzait`; the Free Folk are `battania`. Only the 6 CUSTOM cultures (`vale`/`riverlands`/`stormlands`/`crownlands`/`ironborn`/`nightswatch`) use their own name as the StringId. **Great houses are CLANS** (`clan_<region>_N`, e.g. `clan_north_1` = House Stark), not cultures — see [ADR-011](../../docs/adrs/011-westeros-culture-model.md).

### Checklist

| Step | What to check |
|------|---------------|
| 1 | Every `culture=` attribute uses a StringId from the table above |
| 2 | Every `kingdom=` attribute uses a kingdom ID from CLAUDE.md cheatsheet |
| 3 | Every `settlement=` attribute exists in `settlements.xml` |
| 4 | Every `troop=` attribute exists in `troops/troops_{culture}.xml` |

### Why this matters

This exact bug pattern was caught in 5+ Codex reviews during the LOTR era. Custom cultures use their region name as the StringId, which makes it easy to assume ALL cultures do — but vanilla-base cultures (North/Westerlands/Reach/Dorne/Dothraki/Free Folk) keep their vanilla engine IDs (`sturgia`/`vlandia`/`empire`/`aserai`/`khuzait`/`battania`).

## EquipmentRosters Schema (MANDATORY for `equipmentsets/*.xml`)

The standalone `<EquipmentRosters>` pattern (used by `Main/_Module/ModuleData/equipmentsets/dots_equipment_sets_*.xml`, mirroring vanilla `SandBoxCore/ModuleData/sandboxcore_equipment_sets.xml`) requires:

| Roster purpose | Inner `<EquipmentSet>` opening tag |
|---|---|
| **Battle** (default) | `<EquipmentSet>` — implicit, no attribute |
| **Civilian** | `<EquipmentSet equipmentType="Civilian">` — REQUIRED |

Without `equipmentType="Civilian"` on a civilian roster, the engine treats it as battle equipment regardless of the roster ID containing `_civ_` / `_civ_equipment`. There is NO `equipmentType="Battle"` in vanilla (verified zero matches across SandBoxCore) — battle is the implicit default.

**Why this matters:** Encyclopedia portraits, settlement-walk views, dialog scenes, and random equipment selection at hero spawn all key off this attribute, not off the roster ID. A misclassified civilian roster manifests as the wrong outfit in non-combat contexts — exactly the Faramir/Boromir bug pattern (memory: feedback_equipmenttype_civilian_required.md).

**Catch list before commit** — when editing any `dots_equipment_sets_*.xml`:
1. Grep for `<EquipmentRoster id="[^"]*_civ` matches in the file.
2. Verify each match's next `<EquipmentSet>` line has `equipmentType="Civilian"`.
3. Quick validator:
   ```powershell
   Get-ChildItem Main\_Module\ModuleData\equipmentsets\dots_equipment_sets_*.xml | ForEach-Object {
     $x = [xml](Get-Content $_.FullName -Raw)
     $civ = $x.SelectNodes('//EquipmentRoster[contains(@id, ''_civ'')]/EquipmentSet')
     $t = ($civ | Where-Object { $_.equipmentType -eq 'Civilian' }).Count
     Write-Host "$($_.Name): $t/$($civ.Count) civilian sets tagged"
   }
   ```
   Should report N/N for every file.

**This rule is for the STANDALONE roster pattern only.** Inline equipment under `<NPCCharacter><Equipments>...</Equipments>` (in `characters/*.xml` and `troops/troops_*.xml`) uses a different attribute (`civilian="true"` on `<EquipmentRoster>`) — that pattern is governed separately and is NOT affected by this rule.

## Formatting
- 2-space indentation (per .editorconfig)
- UTF-8 encoding
- CRLF line endings
