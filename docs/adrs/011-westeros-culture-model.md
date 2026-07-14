# ADR-011: Westeros Culture Model (region = culture, great house = clan)

**Status**: Accepted — **amended 2026-07-14** (ADOD map adoption; see the Amendment section: the
culture/kingdom id tables below are superseded by the map's ids)

**Date**: 2026-06-13

**Priority**: **Mandatory**

## Context

DOTS was bootstrapped (2026-06-10) from the LOTR mod "TAOM" and is being re-themed into a Game of
Thrones total conversion set during **Robert's Rebellion (282–283 AC)**. The bootstrap zeroed all
gameplay data (cultures, kingdoms, clans, troops, characters, strings) but left the engine framework
and the still-LOTR-named theme-bound C# (CulturalFeats, VolunteerRecruitment, WarOfTheRing,
LandmarkService).

Before any content is authored, we need a single, written decision on the GoT faction model:
- How many cultures, and what their StringIds are.
- How the nine great houses map onto Bannerlord's "culture" concept.
- Which cultures reuse a vanilla engine culture vs. are fully custom.
- How the LOTR-hardcoded C# is de-LOTR'd.

Bannerlord models **culture as "the people of a land"** (vanilla `empire`, `vlandia`, `sturgia` are
regions, not dynasties). Settlements, notables, and recruitment key off `CultureObject.StringId`. A
region's smallfolk don't change when its ruling house is overthrown — Winterfell raises Northern levies
whether a Stark or a Bolton holds it.

## Decision

### 1. Region = culture; great house = clan

Cultures are **regions** (`north`, `vale`, …); great houses are **clans** within them
(`clan_north_1` = House Stark). Same-region houses share troop trees and equipment, differentiated by
heraldry + lords. This collapses the nine houses onto a smaller culture set and matches the engine's
land-based culture model.

### 2. The 12-culture roster (Robert's Rebellion)

Four of Bannerlord's vanilla cultures are near-perfect playstyle matches; two more cover the
Andal-feudal houses; the rest are custom. **Vanilla-base cultures keep the vanilla engine StringId in
data and are display-renamed via `spcultures.xslt`** (per `.claude/rules/xml-data.md` — using a lore
name as the `culture=` value for a vanilla-base culture is the #1 recurring bug).

| Runtime `culture` StringId | Base | Display / Ruling house | Region code | Kingdom id | Rebellion side |
|----------------------------|------|------------------------|-------------|------------|----------------|
| `sturgia` | vanilla | The North / House Stark | `NO` | `kingdom_north` | Rebel |
| `vlandia` | vanilla | The Westerlands / House Lannister | `WE` | `kingdom_westerlands` | Rebel (joins late, sacks King's Landing) |
| `empire` | vanilla | The Reach / House Tyrell | `RE` | `kingdom_reach` | Loyalist |
| `aserai` | vanilla | Dorne / House Martell | `DO` | `kingdom_dorne` | Loyalist |
| `khuzait` | vanilla | Dothraki & Essos | `ES` | `kingdom_essos` | Mercenary / beyond-map |
| `battania` | vanilla | The Free Folk (Wildlings) | `FF` | `kingdom_freefolk` | Beyond-map |
| `vale` | custom | The Vale / House Arryn | `VA` | `kingdom_vale` | Rebel |
| `riverlands` | custom | The Riverlands / House Tully | `RV` | `kingdom_riverlands` | Rebel |
| `stormlands` | custom | The Stormlands / House Baratheon | `ST` | `kingdom_stormlands` | Rebel (leader) |
| `crownlands` | custom | The Crownlands / House Targaryen | `CR` | `kingdom_crownlands` | Loyalist (the throne) |
| `ironborn` | custom | Iron Islands / House Greyjoy | `IR` | `kingdom_ironborn` | Neutral / opportunistic |
| `nightswatch` | custom | The Night's Watch | `NW` | `kingdom_nightswatch` | Non-belligerent |

Plus bandit/neutral cultures (vanilla `looters`, Vale mountain clans, sellsword companies) configured
via `bandit_management`, replacing the 5 LOTR bandit cultures.

Rationale for the vanilla mappings: `sturgia` (cold infantry) → North; `aserai` (desert spears) →
Dorne; `khuzait` (horse archers) → Dothraki; `battania` (forest tribes) → Free Folk; `vlandia`
(feudal knights/crossbows) → Westerlands; `empire` (central, balanced) → Reach. The two title houses of
the rebellion — Baratheon (the Stag) and Targaryen (the throne) — are **custom** for distinct identity.
The specific vanilla-base assignment for the Andal-feudal houses (Westerlands/Reach) is a convenience
and may be refined during authoring.

### 3. Theme-bound C# is de-LOTR'd by rename, not redesign

- **CulturalFeats:** mechanically rename the ~200 `FeatObject`s (`_gondor*` → `_westerlands*`, etc.) in
  one atomic commit. Feats bind to cultures in (currently empty) XML and dispatch by `FeatObject`
  reference, so the rename is value-preserving. *Not* migrated to data-driven config as part of the
  conversion (a later, separate `/new-feature`).
- **TroopProgression:** generalize `GondorRecruitmentJsonLoader` → `RecruitmentPoolJsonLoader`; move
  `VolunteerRecruitmentService`'s hardcoded pools to per-culture `recruitment_pools/{culture}.json`.
- **Diplomacy:** rename `WarOfTheRing*` → `RobertsRebellion*`; reconfigure war declarations (rebels vs
  loyalists) in `diplomacy/*.json`. No logic redesign.
- **FactionMap:** move `LandmarkService` defs to `factionmap/landmarks.json`; author Westeros
  coordinates against the user-provided map (gated, later phase).

### 4. Armory

Adopt the installed **A Dance of Dragons Armory** (Westerosi items by region under `ModuleData/ADOD-Assets/`)
as the item source now; a DOTS-owned armory is a later track. The LOTR `LOTRLOME_Armory` +
`Alliance.Wargs` modules are retired.

## Consequences

### Positive
- One written source of truth for every culture/kingdom/region id — closes the recurring "LOTR id
  leaked into config" bug class at design time.
- Region = culture lets same-region houses reuse troop trees/equipment, roughly halving culture-authoring.
- Rename-not-redesign keeps the ~2,880-case test suite green and the build stable through the conversion.

### Negative
- Six custom cultures still require full definition (`.claude/rules/xml-data.md` notable-template +
  ~80-attribute work each).
- Vanilla-base cultures inherit some vanilla flavor (sounds, default fallbacks) until troop trees fully override.

### Neutral
- Settlement region-code prefixes finalize only when the user-provided Westeros map lands (a later, map-gated phase).

## Alternatives Considered

### Alternative 1: House = culture (nine house-cultures)
- **Pros**: 1:1 with the great houses; intuitive.
- **Cons**: Andal houses share an aesthetic → near-duplicate cultures; nine custom cultures to author;
  fights the engine's land-based culture model (a conquered fief would "change people").
- **Why rejected**: more authoring for less engine-alignment; the house identity is better carried by clans.

### Alternative 2: All-custom cultures (no vanilla bases)
- **Pros**: full control of every culture's stats.
- **Cons**: discards the four near-perfect vanilla playstyle matches (sturgia/aserai/khuzait/battania)
  and doubles the definition work.
- **Why rejected**: the vanilla bases are a free, accurate fit for North/Dorne/Dothraki/Free Folk.

### Alternative 3: Data-drive CulturalFeats during the conversion
- **Pros**: cleaner long-term architecture.
- **Cons**: rewrites the hottest GameModel dispatch path for zero content benefit while the data is
  empty; large test churn.
- **Why rejected**: out of scope for a re-skin; tracked as a future `/new-feature`.

## Migration Strategy

Phase 1 (this ADR + the harness rules) precedes all content authoring. The vanilla-base cultures are
display-renamed in `spcultures.xslt`; custom cultures are authored in `DOTS_spcultures.xml`. The
theme-bound C# renames happen in their respective content phases (feats with careers; recruitment with
troop trees; diplomacy with the start-state). See the conversion roadmap (the approved plan +
`docs/roadmap.md`).

## Amendment (2026-07-14) — ADOD map adoption fixes the ids

DOTS adopted the external map module **"A Dance of Dragons - Map"** (`ADODMap`, 1,562 settlements,
520 fiefs) and, per user decision, **adopts the map's culture/clan StringIds verbatim** (the map
stays pristine and updatable). This supersedes §2's planned roster where they conflict:

### Vanilla-base culture → region mapping (map-dictated, replaces §2 rows)

| Runtime `culture` StringId | Display (spcultures.xslt) | Evidence |
|---|---|---|
| `battania` | **The North** / House Stark | Winterfell is `Culture.battania` |
| `sturgia` | **The Riverlands** / House Tully | Riverrun is `Culture.sturgia` |
| `vlandia` | The Westerlands / House Lannister | unchanged |
| `khuzait` | **The Reach** / House Tyrell | Highgarden is `Culture.khuzait` |
| `empire` | **The Crownlands** / Iron Throne | King's Landing is `Culture.empire` |
| `aserai` | Dorne / House Martell | unchanged |

### Custom cultures use the map's ids verbatim (case-sensitive, spaces/apostrophes preserved)

`Stormlander` (hand-authored in `dots_spcultures.xml`, renamed from the planned `stormlands`),
plus 31 generated clones in `dots_adod_cultures.xml`: `Valeman`, `Ironborn`, `freefolk`,
`nightswatch`, `valyrian`, `Dothraki`, `Braavosi`, `Pentoshi`, `Myrish`, `Lyseni`, `Tyroshi`,
`Volantene`, `Norvoshi`, `Qohorik`, `Lorathi`, `Ghiscari`, `Qartheen`, `Yi-Tish`, `Lengii`,
`Ibbenese`, `Summer Islander`, `Naathi`, `Sothoryi`, `Shrykemen`, `Carcosans`, `N'ghai`,
`Hyrkenese`, `Basilisk Corsair`, `Old Valyrian`, `Sarnori`, `Asshai`. The planned `vale` /
`riverlands` / `crownlands` / `ironborn` ids are **dead** — never author against them.

### Kingdoms (37 total)

The 8 vanilla kingdom ids are repurposed to realms via `spkingdoms.xslt` (`empire`=Iron Throne,
`battania`=The North, `sturgia`=Riverlands, `vlandia`=Westerlands, `khuzait`=Reach, `aserai`=Dorne,
`empire_w`=Stormlands, `empire_s`=Vale) — the planned `kingdom_<region>` ids are dead. 29 custom
kingdoms (`ironislands`, `nightswatch`, `freefolk`, `braavos`, … `asshai`) are generated into
`dots_spkingdoms.xml`. Robert's Rebellion wars (rebels: North/Vale/Riverlands/Stormlands vs
loyalists: Iron Throne/Reach/Dorne) are declared statically in `spkingdoms.xslt` relationships;
`diplomacy/war_of_the_ring.json` is disabled pending a GoT phased-escalation redesign.

### Clans (303 map owners)

74 vanilla clans are retargeted (name/culture/kingdom/home) via `spclans.xslt`; 229 clans +
leader lords + heroes are generated into `characters/{clans,lords,heroes}.xml`. Great-house
identities (House Stark = `clan_battania_1`, House Targaryen = `ADODhouse_1`, …), era-correct
rulers (Eddard Stark, Aerys II, …), and name pools live in the hand-authored SPEC.

### Source of truth

`tools/data/westeros_factions_spec.json` (politics SPEC) + `tools/generate_adod_factions.py`
(generator; regenerates all generated files) + `tools/audit_adod_map_refs.py` (resolution gate).
§2's settlement region-code prefix scheme is obsolete — settlement ids are fixed by the ADOD map.
Map-forced anachronisms (Castamere/Tarbeck Hall thriving → Houses Reyne/Tarbeck alive; populated
Old Valyrian cities → "The Valyrian Remnant") are deliberate alt-history.

## References

- `.claude/rules/xml-data.md` — culture StringId + region-code tables (the enforced, auto-loaded copy)
- `CLAUDE.md` "Re-skin status" banner + GameModel/Harmony reference tables
- `CHANGELOG.md` 2026-06-13 (presentation re-skin) and the conversion roadmap
- A Dance of Dragons Armory — `…/Modules/A Dance of Dragons Armory/ModuleData/ADOD-Assets/`
