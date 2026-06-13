# CHANGELOG — DOTS (Dawn of the Stag)

## 2026-06-13

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
