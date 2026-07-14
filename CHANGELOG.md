# CHANGELOG — DOTS (Dawn of the Stag)

## 2026-07-14

### fix: deep-review findings on the ADOD map adoption + cache transcoder (8-agent review)

`/deep-review` (5 core + tooling-correctness + faction-semantics + adversarial-transcoder agents)
over the full uncommitted changeset. RCA: `docs/reviews/rca-adod-map-cache-2026-07-14.md`.
All confirmed findings fixed in-session and every gate re-run green:

- **H1:** 21/229 generated lords were clones of vanilla `main_hero` (all-zero skills) —
  `parse_lord_templates` now excludes `main_hero` + skill-less shells; regenerated (0 remain).
- **DF-1:** The Thenns (`clan_sturgia_5`), ruling clan of `Kingdom.freefolk`, derived into
  Battania via its battania-tagged placeholder fiefs — SPEC culture/kingdom override + a
  generator assert (every kingdom's ruling clan must land in its own kingdom) +
  `audit_adod_map_refs.py` now statically recovers spclans.xslt retargets and ruler-checks all
  37 kingdoms (closes its documented XSLT blind spot). Verified via real `XslCompiledTransform`.
- **Tooling HIGHs (A.1/A.2/A.2b/B.1):** generator now stages all outputs in memory and writes
  explicit CRLF bytes only after every validation passes (atomic-per-run, platform-independent);
  overwrite guard is structural (marker / empty placeholder / identity stub) instead of
  size-heuristics that silently clobbered small hand-edited files; transcoder now writes
  temp → validates (incl. sampled bit-exact distance comparison vs legacy) → swaps, so a failed
  run can never leave the map module without a working cache.
- **DF-2:** `war_of_the_ring.json` `enabled:false` was outranked by the MCM default —
  `DotsSettings.WarOfTheRingEnabled` default flipped to `false` (hint documents precedence).
- **MED/LOW:** SPEC tier hints now emitted as spclans.xslt `@tier` overrides (Thenns tier 5);
  name-pool exhaustion fails loud; ruler names deduped; `parse_map` Town-count assert;
  BodyProperties face-age synced to assigned lord age; audit prints load diagnostics.
- **Tests:** 24 new unit tests across `tools/tests/test_{generate_adod_factions,`
  `build_adod_distance_cache,audit_adod_map_refs}.py` pin every fix (139 tool tests green).
  Full C# suite 2,880 passed / 0 failed; `validate_moduledata --warnings-as-errors` PASS.
- Deferred (recorded in RCA): pre-existing `.Count()` verification-path enumeration
  (outside changeset); ~25 dangling LOTR equipmentsets SubModule registrations (bootstrap
  leftover); M2 kingdom-owner precedent deviation documented as deliberate.

### feat: offline distance-cache build for the ADOD map (run BEFORE the game) + rebuild retarget

**Without this, a campaign on the ADOD map hard-crashes on the loading screen.** Decompile-verified
(installed engine is **v1.4.7**, not the documented 1.4.5; `NavigationCache<T>` identical across
1.4.5→1.4.7): the engine probes `<Module>/ModuleData/DistanceCaches/settlements_distance_cache_<NavType>.bin`
across all active modules (LAST match wins, `SandBox.View SettlementPositionScript`); the map's
shipped cache is legacy-format at the legacy path and is never read; the last match is then a
**Calradia** bin → unknown-id NRE inside a swallowed catch → no cache registered →
`CalculateAverageDistanceBetweenTowns` NRE at load. The in-game MCM rebuild needs a loaded
campaign, so it can't bootstrap (TAOM never solved pre-campaign builds; its editor path was removed).

- **`tools/build_adod_distance_cache.py`** (NEW — the pre-game builder the user asked for; dry-run
  default, `--apply` writes): transcodes the map's legacy bin (byte-validated: complete 1,562-id
  half-matrix, 1,219,141 pairs, exact settlements.xml match) into the modern v1.4.7 grammar at
  `A Dance of Dragons - Map/ModuleData/DistanceCaches/settlements_distance_cache_Default.bin`.
  Distances + closest-face section verbatim (incl. 1e30 unreachable sentinels); pairs re-grouped to
  `NavigationCacheElement.Sort` ordinal-canonical order (required — `Deserialize` re-Sorts by ref);
  fortification neighbors approximated with a Gabriel graph on path distances (~3.5 avg degree vs
  vanilla ~4.3; the first in-game MCM rebuild replaces with exact path-walk data); zero-CRC header
  (shipping reader discards it). Output written + independently re-parsed to EOF: **applied,
  32,964,238 bytes, validation OK**.
- **Retarget** of the in-game rebuild output from defunct `DOTS_Map` to the ADOD map module:
  `RuntimeCacheRebuildService.ResolveCacheOutputPath`, `CacheRebuildConfig` defaults (3 paths),
  `configs/cache_rebuild_config.json`, path test, MCM hint/comment (now states the offline tool is
  the first-time bootstrap; ~25-90 min estimate for 1,562 settlements).
- **New-game NRE exposure fixed (faction-layer follow-up):** `InitialChildGeneration` iterates ALL
  major-faction clans, and the 32 new cultures had no child/teen equipment templates → the exact
  `HeroCreator.CreateChild` NRE pinned by `ChildGenerationCultures_*` (RCA 2026-06-02).
  `generate_adod_factions.py` now also emits `equipmentsets/dots_child_equipment_templates.xml` +
  `dots_lord_template_equipment.xml` (192 rosters each: 6 child + 6 teen vanilla-clone variants ×
  32 cultures, noble variants carry `IsLordTemplate`). The test's dead LOTR pin (goblin /
  mistymountainorcs — failing since the bootstrap zeroing, pre-existing) is re-pinned data-driven:
  every custom culture used by `characters/clans.xml` or the `spclans.xslt` retargets must ship
  child/teen/lord rosters.
- **Map data-quality flag:** 10 settlements are off-navmesh/unreachable (`castle_NoxixRedwyne_nox1`,
  6 hideouts, `retirement_retreat`, `village_A2_2`) — reported by the tool; ADOD scene fix needed
  eventually.
- **⚠ NavalDLC must be disabled in the launcher for DOTS play** — active NavalDLC demands
  `_Naval`/`_All` caches on any non-Sandbox map and crashes on its own Calradia bins (#120).

Verified: transcoder dry-run + `--apply` + independent modern-grammar re-parse OK; all 4 XSLT/paths
sites green; `dotnet build` 0 errors; **full test suite 2,880 passed / 0 failed** (was 1 pre-existing
failure); `audit_adod_map_refs.py` OK; `validate_moduledata.py --warnings-as-errors` PASS.
Not-tested: in-game campaign load (boot test pending — also gates issue #1).
Research: SettlementPositionScript (shipping SandBox.View.dll), NavigationCache`1
Serialize/Deserialize/FinalizeCacheInitialization, NavigationCacheElement`1.Sort,
SandBoxNavigationCache (v1.4.7 via dots-src + ilspycmd). Doc: docs/features/editor-cache-rebuild.md
"Bootstrap on the ADOD Westeros map".

### feat: adopt the ADOD Westeros map — full culture/kingdom/clan faction layer (Robert's Rebellion)

DOTS now loads the external map module **"A Dance of Dragons - Map"** (`ADODMap`, 1,562 settlements /
520 fiefs, Westeros + Essos). The map registers only `settlements.xml` (its XSLT deletes vanilla
settlements — same pattern as TAOM_Map); every `culture=`/`owner=` ref must resolve against DOTS
definitions. Per user decision the map's ids are adopted **verbatim** (map stays pristine) and the
**Robert's Rebellion (283 AC) political layout is designed in**, not stubbed:

- **Load order:** `ADODMap/SubModule.xml` (external, edited in place per TAOM_Map precedent) now
  declares `DependedModule`/`DependedModuleMetadata` on `DOTS` (`LoadBeforeThis`). DOTS SubModule
  registers the new `dots_adod_cultures` SPCultures node.
- **Cultures (43 referenced):** 11 vanilla + `Stormlander` (hand culture in `dots_spcultures.xml`,
  **renamed from `stormlands`** to match the map id) + **31 generated clones** in
  `dots_adod_cultures.xml` (`Valeman`, `Ironborn`, `freefolk`, `nightswatch`, `Dothraki`, `Braavosi`,
  `Yi-Tish`, `Old Valyrian`, … incl. ids with spaces/apostrophes — engine-safe, tooling must slugify).
  `spcultures.xslt` display renames redone to the **map's** region mapping: `battania`=The North,
  `sturgia`=The Riverlands, `khuzait`=The Reach, `empire`=The Crownlands (Winterfell is battania,
  Riverrun is sturgia, Highgarden is khuzait, King's Landing is empire on this map).
- **Clans (303 map owners):** 229 generated (`characters/clans.xml` + one leader lord each in
  `lords.xml` — vanilla-lord template clones — + `heroes.xml` rows; heroes.xml is mandatory or clan
  owners crash). 74 vanilla clans retargeted via generated `spclans.xslt` (house name, culture,
  kingdom, map home). Great houses hand-identified from their seats: House Stark=`clan_battania_1`,
  Targaryen=`ADODhouse_1`, Lannister/Tully/Arryn/Tyrell/Martell/Baratheon/Greyjoy + ~180 more
  canon houses; Essos + long tail named from per-culture pools.
- **Kingdoms (37):** 8 vanilla ids repurposed to realms via generated `spkingdoms.xslt`
  (`empire`=Iron Throne, `empire_w`=Stormlands, `empire_s`=Vale, …) + 29 generated custom kingdoms
  (`ironislands`, `nightswatch`, `freefolk`, 9 Free Cities, `dothraki`, `yi_ti`, …) in
  `dots_spkingdoms.xml`. Era-correct rulers (Aerys II, Eddard Stark, Jon Arryn, Hoster Tully,
  Robert Baratheon, Tywin, Mace, Doran, Quellon Greyjoy, LC Qorgyle) via generated `lords.xslt`
  renames + generated lords. **Rebellion wars set statically** in `spkingdoms.xslt` relationships
  (rebels North/Vale/Riverlands/Stormlands vs loyalists Iron Throne/Reach/Dorne; 12 war pairs);
  `diplomacy/war_of_the_ring.json` disabled (stale LOTR ids) pending GoT phased-escalation redesign.
- **Toolchain:** `tools/generate_adod_factions.py` (parses map + vanilla + SPEC, emits all generated
  files, dry-run default, marker-guarded overwrites) + hand-authored politics SPEC
  `tools/data/westeros_factions_spec.json` + `tools/audit_adod_map_refs.py` (resolution gate:
  map cultures/owners, hero/lord/clan/kingdom consistency). Validator wired: `dots_adod_cultures.xml`
  added to `dots_schema.py` culture_files + `DOTS_spcultures.json` applies_to.
- **Docs:** ADR-011 amended (map-dictated ids supersede the planned `vale`/`riverlands`/…);
  `.claude/rules/xml-data.md` culture tables rewritten.

Verified: `audit_adod_map_refs.py` OK (229 clans / 29 kingdoms / 229 heroes+lords consistent);
`validate_moduledata.py --warnings-as-errors` PASS (48 cultures, no broken refs); all 4 XSLTs
transform-tested under .NET `XslCompiledTransform` against installed vanilla (House Stark rename,
Greyjoy→ironislands, Iron Throne rename, 24 war entries, Eddard Stark, Crownlands/Riverlands all
land); `dotnet build Main` 0 errors.

Not-tested: in-game campaign load (needs live game). **Please boot-test:** enable DOTS + ADODMap
(+ A Dance of Dragons Armory), new sandbox campaign → map renders, first day ticks, Encyclopedia
lists the kingdoms/houses, save+load once. Known follow-ups: culture `start_point_position` values
still vanilla-map coords (CC start placement); `mountain_bandits` (vanilla bandit clan) owns the 10
Vale mountain-clan castles as shipped by the map — watch for oddities; heraldry banner_keys,
localization pass, and lords fleshing (spouses/heirs/skills) are later phases.

## 2026-06-13

### feat: Phase A — first custom culture (Stormlands / House Baratheon) proof-of-life

Authored the first GoT custom culture in `DOTS_spcultures.xml`, the title house of Robert's Rebellion.
Approach (ADR-011): clone vanilla `vlandia`'s full, known-valid `<Culture>` structure (72 attributes +
18 child elements) and change only the identity — `id="stormlands"`, name "The Stormlands", a Baratheon/
Robert's-Rebellion description, and Baratheon black/gold colours. **Every** troop / NPC / party-template /
equipment ref still points at vanilla `vlandia` objects, so the culture loads on vanilla content and the
validator's cross-ref sweep resolves cleanly. Westeros troops, armour (A Dance of Dragons), house names,
lords, and a Stormlands kingdom/clans layer on in later phases.

- Also fixed a bootstrap bug: `DOTS_spcultures.xml` was reset to root `<Cultures/>`, but it's registered
  as `<XmlName id="SPCultures">` and vanilla uses `<SPCultures>` — a populated `<Cultures>` root would
  not have merged. Now `<SPCultures>`.
- `validate_moduledata.py`: `PASS`, registry now reports **17 cultures** (the 16 vanilla + `stormlands`),
  no broken refs, no duplicate id.
- Names display via inline `{=dots_culture_*}default`; registering the `{=dots_culture_*}` keys (these +
  the 6 vanilla-base renames) into the strings pipeline is Phase E (localization).

Not-tested: in-game campaign load (no live game here). It is a structural clone of a culture that loads,
with only identity attributes changed and all refs resolving — low load-risk — but **please load-test**
(pick "The Stormlands" at character creation; confirm it appears with the Baratheon name/colours).

### chore: zero stale LOTRLOME equipmentsets the bootstrap missed (validator now clean)

With the validator working again, it surfaced 898 `UNKNOWN_CULTURE` errors: the bootstrap zeroed
`troops/` and `characters/` but left the LOTRLOME-generated `equipmentsets/` files, which still keyed
660+ rosters to dead LOTR cultures (gondor/mordor/…). Zeroed the 5 offending files to empty
`<EquipmentRosters />` stubs (consistent with the bootstrap's treatment of troops/characters; no C#
hard-references their roster ids): `DOTS_char_creation_equipment`, `DOTS_lord_template_equipment`,
`DOTS_child_equipment_templates`, `DOTS_wanderer_equipment`, `DOTS_equipment_sets_dolguldur`. These
are regenerated from A Dance of Dragons Armory in Phase B. `validate_moduledata.py` now reports
`PASS` — clean baseline for content authoring. (Remaining per-culture `dots_equipment_sets_*.xml` still
reference LOTRLOME items but don't error while LOTRLOME is installed; cleaned in Phase B.)

### fix: repair the moduledata validator tool-suite (broken since the bootstrap)

The TAOM→DOTS bootstrap rename uppercased the schema/query/MCP module filenames
(`dots_schema.py`→`DOTS_schema.py`, …) but left every `import dots_schema` lowercase. Python's
case-sensitive import breaks on Windows' case-insensitive filesystem, so `validate_moduledata.py`,
`dots_query.py`, `dots_mcp_server.py`, and their tests had been failing collection since 2026-06-10.

- Renamed the 4 misnamed files back to lowercase to match the imports, `.mcp.json`, and CLAUDE.md:
  `DOTS_schema.py`→`dots_schema.py`, `DOTS_query.py`→`dots_query.py`,
  `DOTS_mcp_server.py`→`dots_mcp_server.py`, `tests/test_DOTS_query.py`→`test_dots_query.py`
  (+ `test_DOTS_mcp_server.py`→`test_dots_mcp_server.py`). Zero code changes for the rename.
- `dots_schema.load_schemas`: read schema JSON with `utf-8-sig` so the UTF-8 BOM on the
  `tools/schemas/*.json` files no longer trips Python 3.14's stricter `json.load`.
- Result: all 38 `tools/tests` pass; `validate_moduledata.py` runs. It immediately surfaced 898
  pre-existing `UNKNOWN_CULTURE` errors — the LOTRLOME-generated `equipmentsets/*.xml` files were not
  zeroed at bootstrap and still reference dead LOTR cultures (cleanup tracked for the content phases).

### docs: re-skin presentation layer LOTR → Game of Thrones (Robert's Rebellion)

Re-themed the public-facing documentation from the inherited Lord of the Rings framing to Game of
Thrones: Robert's Rebellion (282–283 AC). **No code or gameplay data changed** — this is a
documentation + stats-reconciliation pass. The LOTR gameplay data remains zeroed from the bootstrap
(authoring begins separately); these docs now describe the GoT target and the accurate engine inventory.

**README.md:**
- New title/subtitle and "What is it" framing (Westeros at the outbreak of Robert's Rebellion).
- Replaced the LOTR faction table with the GoT roster: rebels (Baratheon/Stark/Arryn/Tully, Lannister
  joining late), loyalists (Targaryen/Tyrell/Martell), unaligned (Greyjoy/Night's Watch/Free Folk/Essos)
  — ~12–13 cultures with sigil/region/words.
- Reframed the headline systems: War of the Ring → Robert's Rebellion phased escalation; Special
  Resources → regional War Resources (Valyrian Steel, Dornish Wine, …); Cultural Feats → House Feats;
  Named Companions → era-canonical lords (Eddard, Jaime, Barristan, …). Removed the stripped Race & Age
  and Warg Combat bullets.
- Updated the companion-modules list: adopt **A Dance of Dragons Armory**; retired LOTRLOME_Armory and
  Alliance.Wargs; map provided separately. Corrected the default-branch claim (`master`, not a
  nonexistent `bannerlord-1.4.5`).
- License/Acknowledgments: non-affiliation with GRRM / HBO / Warner Bros. Discovery / TaleWorlds;
  retained the The Old Realms (TOR) architecture credit and added the TAOM bootstrap provenance.

**Stats reconciliation (verified against the working tree, 2026-06-13):**
- Feature modules: **49** (README previously said 50; CLAUDE header says "36 carried forward").
- GameModel overrides: **35** (README previously said 37). Class names are `Dots*Model` — confirmed
  correct, no casing change made.
- Unit tests: **2,382** `[TestMethod]`/`[DataTestMethod]` (+558 `[DataRow]` cases ≈ the ~2,880
  executed-case figure the bootstrap entry reports; README previously said 2,200+).
- ADRs: **10** (README said 11). Feature docs: **85** (README said 74).

**CLAUDE.md** (edited under explicit user authorization — config-protection hook covers this file):
- Added a "Re-skin status" banner scoping the LOTR-named reference tables (engine GoT-ready; names
  reflect retired/zeroed data) and recording the GoT model (region = culture, great house = clan).
- Relabeled `Patch12_WarOfTheRing` (→ Robert's Rebellion engine; code symbol id retained); removed the
  stripped-HeroRace clause from the NamedCompanions key-path; repointed Equipment & Armory to A Dance of
  Dragons Armory and marked the LOTRLOME prefix/folder tables legacy.

**AGENTS.md / docs/roadmap.md / docs/INDEX.md:**
- AGENTS.md: reframed the reviewer-role line to GoT; preserved the historical LOTR review ledger with a
  caveat that its culture ids are TAOM-historical.
- roadmap.md: retitled to Westeros-authentic goals; removed the stripped RaceAge models from
  "Implemented"; re-scoped the race-based override opportunities to culture/house-based.
- INDEX.md: top re-skin note; fixed the main-menu string, the diplomacy line, and a companion example;
  flagged stripped-feature doc links as retained TAOM history.

### docs: Phase 1 harness de-LOTR — Westeros culture model (ADR-011) + authoring-gate rules

Established the GoT faction/culture model in the harness **before** any content authoring, so skills and
agents stop emitting LOTR culture ids. Still docs/config only — no C# or gameplay data changed.

- **ADR-011 (Westeros Culture Model, Mandatory):** new `docs/adrs/011-westeros-culture-model.md` +
  registered in the ADR index. Locks: **region = culture, great house = clan**; the 12-culture roster
  (**6 vanilla-base** — `sturgia`=North, `vlandia`=Westerlands, `empire`=Reach, `aserai`=Dorne,
  `khuzait`=Dothraki, `battania`=Free Folk — **+ 6 custom** — `vale`/`riverlands`/`stormlands`/
  `crownlands`/`ironborn`/`nightswatch`); de-LOTR the theme-bound C# by rename, not redesign; adopt
  A Dance of Dragons Armory.
- **`.claude/rules/xml-data.md`** (auto-loads on every ModuleData edit — the gate): replaced the LOTR
  culture StringId table, the "common mistake" examples, and the region codes with the Westeros set;
  added the great-house-is-a-clan rule and the vanilla-base-keeps-its-engine-id warning.
- **`.claude/memory/MEMORY.md`:** re-skin banner; GoT region codes; notable-template culture notes
  re-pointed to the 6+6 roster; flagged the TAOM-era lords-system pointer.
- **`/new-culture` + `docs/ai-includes/new-culture-authoring.md`, `/author-armor`, `/lord-skills`:**
  re-skin banners pointing at ADR-011; armor flow retargeted to A Dance of Dragons Armory (LOTRLOME
  pipeline marked legacy pending Phase B); lord/lore examples swapped to Westeros.

### feat: Phase A (start) — vanilla-base culture display-renames + clan_heraldry cleanup

First content increment of the faction skeleton (ADR-011). First gameplay-data change of the conversion.

- **`spcultures.xslt`:** display-rename the 6 vanilla-base cultures to their Westeros names via XSLT —
  `sturgia`→The North, `vlandia`→The Westerlands, `empire`→The Reach, `aserai`→Dorne, `khuzait`→Dothraki,
  `battania`→The Free Folk — with short Westerosi `text` descriptions. Only `@name`/`@text` are
  overridden; every other vanilla attribute/child passes through the identity transform. Verified by
  applying the transform to the installed vanilla `spcultures.xml` (lxml): all 6 renamed, all 16
  cultures + critical passthrough attrs (`is_main_culture`/`faction_banner_key`/`basic_troop`) preserved.
  Runtime StringIds stay vanilla. The `{=dots_culture_*}` keys carry inline defaults (localization is Phase E).
- **`clan_heraldry/`:** removed the 21 dead LOTR JSONs (they referenced bootstrap-zeroed clans/troops/
  templates and have no runtime consumer — only the heraldry-generator tooling reads them). Added a
  README documenting the GoT regeneration plan (keyed on the future Westeros clans).

Not-tested: in-game load (no live game here) — runtime verification deferred. The 6 custom cultures,
kingdoms, clans, and lords (the bulk of Phase A) remain to be authored.

## 2026-06-10

### feat: bootstrap DOTS from TAOM architecture (36 features, GoT blank-slate)

Ported the full TAOM (Tales from the Age of Men) Bannerlord mod architecture into DOTS
(Dawn of the Stag), a new total overhaul mod set during Robert's Rebellion in Game of Thrones.

**What was ported:**
- Complete TAOM mod architecture: IoC/DryIoc, adapter pattern (ADR-007), TDD infrastructure, 36 gameplay features
- All source files mass-renamed TAOM→DOTS (namespaces, class names, module IDs, XML prefixes)
- Build infrastructure: `DOTS.sln`, `DOTS.csproj`, `DOTS.Tests.csproj`, `Directory.Build.props`, `build.ps1`
- 12-language localization infrastructure (stub files, `language_data.xml` for all 12 languages)
- Full test suite: 2880 tests passing

**What was stripped (LOTR/race-specific, not applicable to GoT human-only setting):**
- `HeroRace` + `RaceAge` features — GoT is human-only; race system stripped entirely
- `Warg` feature — LOTR creature; will be replaced with direwolf feature
- `Spider` feature — LOTR creature
- `Elephant` feature — LOTR creature; will be replaced with GoT war horses/siege
- `NativeSkinFixes` — LOTR custom-skeleton mesh fix; not needed for human-only GoT
- `IRaceManager` interface and `RaceManager` implementation deleted from `Core.Domain`
- Race-gated GameModel overrides removed (`DotsAgeModel`, `DotsPregnancyModel`, etc.)

**What was zeroed (LOTR content, GoT authoring required):**
- `troops/` — all LOTR troop XMLs cleared; GoT house troops to be authored
- `characters/` — LOTR lords/clans/companions cleared; GoT characters to be authored
- `Languages/` — 12 x 7 language stub files created (English strings to be populated)
- `career_system/dots_career_choices.xml` — empty; GoT career choices to be authored
- `charactercreation/cultures.json` + `career_menu.json` — empty stubs for bootstrap
- `factionmap/factions.json` — empty; GoT great houses to be authored
- XSLTs cleaned to vanilla passthrough (LOTR injections removed)

**BehaviorTrees/BehaviorTreeWrapper** kept as inlined libraries for future GoT creatures (direwolves, dragons).
