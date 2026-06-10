# CHANGELOG — DOTS (Dawn of the Stag)

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
