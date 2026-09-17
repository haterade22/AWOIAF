# AWOIAF — A World of Ice and Fire

A Game of Thrones total-conversion mod for **Mount & Blade II: Bannerlord** — Westeros at the
outbreak of the **War of the Five Kings (298–300 AC)**.

> **Setting change (2026-09-16):** the mod moves from Robert's Rebellion (282–283 AC) to the War of the
> Five Kings. The README describes the new setting; the faction data, the Rebellion escalation feature and
> the era-canonical companions still reflect the old one until [#4](https://github.com/haterade22/AWOIAF/issues/4)
> lands.

> **Formerly DOTS — Dawn of the Stag.** The repository was renamed to `AWOIAF` on 2026-09-16 to match the
> campaign-map module (`AWOIAF_Map`). The code, module Id, DLL and namespaces are still `DOTS` until the
> identity rename in [#3](https://github.com/haterade22/AWOIAF/issues/3) lands — wherever this README says
> `DOTS`, it means the current build artefacts, not a different project.

**Engine target:** the code pins **v1.4.5**; the 1.5.3 migration (3 compile errors) is in progress. The map
module already loads in the **v1.5.3** Scene Editor. Latest tag: [`v0.1.0`](https://github.com/haterade22/AWOIAF/releases/tag/v0.1.0).

## What is it

AWOIAF recasts Bannerlord as Westeros in the year King Robert dies. Five crowns are claimed at once:
the boy-king **Joffrey** on the Iron Throne with Lannister steel behind him, his uncles **Stannis** on
Dragonstone and **Renly** with the Stormlands and the Reach, **Robb Stark** proclaimed King in the North
at Riverrun, and **Balon Greyjoy** crowning himself again on Pyke. Dorne and the Vale hold back, the
wildlings gather beyond the Wall, and across the Narrow Sea the last Targaryen has dragons. Play any of
the great houses across a custom Westeros + Essos map, recruit house-specific troop trees, pursue a
tiered career, exploit per-region war resources, and fight toward the war's set-piece battles — the
Whispering Wood, the Green Fork, the Blackwater. Every kingdom, clan, lord, and troop is being rebuilt
for the world of A Song of Ice and Fire.

**By the numbers:** 49 feature modules · 35 GameModel overrides · 30+ Harmony patch categories ·
2,382 unit tests · 85 feature/architecture docs.

> The mod is built on a mature engine framework bootstrapped from a prior total conversion; the
> **Game of Thrones content (cultures, troops, lords, careers) is being authored** — see the
> [CHANGELOG](CHANGELOG.md) and the [roadmap](docs/roadmap.md). The default branch is **`master`**.

## Quick Start (Developers)

**Prerequisites**

- Mount & Blade II: Bannerlord installed (code targets **v1.4.5**; **v1.5.3** migration in progress)
- Visual Studio 2022 (or the .NET SDK + MSBuild) — targets .NET Framework 4.7.2
- `BANNERLORD_GAME_DIR` environment variable pointing at your game install
  (the `setup-dev-env.ps1` script configures this)

**Build & test**

```powershell
git clone https://github.com/haterade22/AWOIAF    # clones master (default branch)
cd AWOIAF

.\setup-dev-env.ps1        # configure BANNERLORD_GAME_DIR + dependencies
.\build.ps1                # build the mod
.\build.ps1 -RunTests      # build + run the test suite
dotnet test DOTS.Tests     # tests only
```

A successful build deploys the module into your game's `Modules/` folder. Enable **DOTS** in the
Bannerlord launcher and start a **new campaign** (existing saves are not supported).

`DOTS.sln` at the root contains both `Main` (mod code) and `DOTS.Tests`. Tests run with MSTest +
NSubstitute. Shared build settings live in [`Directory.Build.props`](Directory.Build.props).

## The campaign map — `AWOIAF_Map`

The Westeros + Essos campaign map is a separate, external Bannerlord module, **`AWOIAF_Map`**
("A World of Ice and Fire - Map"), seeded from the *A Dance of Dragons* map: 1,562 settlements, its own
materials, map-icon meshes and distance cache. It is ~9 GB and is **not in this repository** —
`tools/awoiaf_map/create_module.py` rebuilds it from the ADOD source folder, and the rest of
`tools/awoiaf_map/` (schema migration, `.tpac` material/mesh porting, asset-source retargeting) keeps it
loadable without any ADOD module. Status, evidence and the deferred work (in-game load, Westeros trim) are in
[`docs/features/awoiaf-map.md`](docs/features/awoiaf-map.md).

## Project Structure

```
AWOIAF/                       # repo (folder may still be named DOTS locally)
├── Main/                     # Mod source (.NET Framework 4.7.2)
│   ├── Features/             # 49 feature modules (CareerSystem, SpecialResources, CulturalFeats, …)
│   ├── Core/                 # Core infrastructure + IoC
│   ├── Adapters/             # Sealed-type adapters (IHeroAdapter, etc.)
│   └── _Module/              # Bannerlord module files (SubModule.xml, ModuleData, GUI)
├── DOTS.Tests/               # Unit tests (MSTest + NSubstitute, 2,382 tests)
├── docs/
│   ├── adrs/                 # Architecture Decision Records (10)
│   ├── features/             # Feature documentation (85 files)
│   └── migration/            # Bannerlord version-migration tracking
├── tools/                    # Rebalancing + localization scripts; awoiaf_map/ = map-module tooling
├── .claude/                  # Claude Code config (skills, agents, rules, hooks, memory)
├── .codex/                   # Codex adversarial-reviewer config
├── CLAUDE.md                 # AI instruction file (authoritative project reference)
├── AGENTS.md                 # Codex review instructions
└── build.ps1                 # Build script
```

## Architecture

All mod logic follows one pattern:

```
[HarmonyPatch / GameModel / CampaignBehavior] → IHookInterface → Service → IAdapter
```

Services never touch TaleWorlds sealed types directly — they work through adapter interfaces, which
keeps business logic fully unit-testable.

**Non-negotiable rules:**

- TDD mandatory (red → green → refactor)
- Entry points under 150 lines — delegate to services
- No `#region`, no `[Obsolete]`, no `#if DEBUG` (except IoC registration)
- Adapter pattern for any TaleWorlds sealed type
- Research TaleWorlds internals before implementing — never guess signatures

See the [Architecture Decision Records](docs/adrs/) for the full set of design constraints.

## Features

### Factions

**The War of the Five Kings** splits Westeros between five claimants, with Dorne, the Vale, the Wall
and the lands beyond the realm standing apart.

| The Five Kings | Neutral / Unaligned |
|----------------|---------------------|
| **Joffrey Baratheon** — the Iron Throne · Crownlands + **House Lannister** (Westerlands · golden lion · *Hear Me Roar*) | **House Martell** — Dorne · red sun & spear · *Unbowed, Unbent, Unbroken* |
| **Stannis Baratheon** — Dragonstone · crowned stag in a burning heart · *Ours is the Fury* | **House Arryn** — the Vale · falcon & moon · *As High as Honor* (under Lysa, the Vale stays home) |
| **Renly Baratheon** — Stormlands + **House Tyrell** (the Reach · golden rose · *Growing Strong*) | **The Night's Watch** — the Wall · sworn to no crown |
| **Robb Stark**, King in the North — the North (*Winter is Coming*) + **House Tully** (Riverlands · *Family, Duty, Honor*) | **The Free Folk** — beyond the Wall · Mance Rayder's host |
| **Balon Greyjoy**, King of the Iron Islands — golden kraken · *We Do Not Sow* | **Essos** — Daenerys Targaryen, Dothraki khalasars & Free-City sellswords |

Roughly twelve to thirteen playable cultures across the Seven Kingdoms, the Wall, and Essos — house
troop trees, lords, and recruitment are being authored culture by culture.

### Headline systems

- **Career System** — pick a career path at character creation (man-at-arms, outrider, sworn sword,
  sellsword, …); progress a tiered choice tree, unlock passive bonuses + an active battlefield
  ability (press **V**).
- **War Resources** — per-region resources (Valyrian Steel, Dornish Wine, Reach grain, the gold of
  Casterly Rock, Ironborn plunder, …) that gate elite troop upgrades; XML-driven with many-to-one
  region/house mappings.
- **House Feats** — lore-driven culture feats (Northern winter-hardiness, Dornish skirmish speed,
  Reach prosperity, Westerlands gold income, Ironborn raiding), each backed by a GameModel override.
- **The Five Kings' War** — scripted phased escalation from Robert's death and Ned Stark's arrest into
  open war between the claimants, toward the Blackwater; configurable via JSON + MCM. *(Engine carried
  over from the Rebellion version; the phases are being re-authored for this war — #4.)*
- **Named Companions** — era-canonical figures as recruitable wanderers (Brienne of Tarth, Bronn,
  Thoros of Myr, Ser Barristan Selmy, Jorah Mormont, …). *(Roster being re-authored for 298 AC — #4.)*

…and ~40 more systems (banner color persistence, settlement guards, custom battles, siege defense,
tournament armor, shader precompilation, and more). Each is documented under
[`docs/features/`](docs/features/). House and faction rules are enforced through **35 GameModel
overrides** and **30+ Harmony patch categories** — both registries are catalogued in
[CLAUDE.md](CLAUDE.md).

## How It's Built (AI-assisted pipeline)

AWOIAF is developed with a structured, AI-assisted engineering pipeline.

- **[Claude Code](https://docs.anthropic.com/en/docs/claude-code)** is integrated as more than a
  code generator: 33 custom slash-command skills, 5 specialized agents, 18 automated hooks,
  15 path-scoped rule files, persistent cross-session memory, and 7 MCP servers (symbolic code
  navigation, decompilation, git, GitHub). [CLAUDE.md](CLAUDE.md) is the authoritative reference
  every session loads.
- **Codex** (OpenAI) runs as an *independent adversarial reviewer* — it shares no session context
  with Claude, so it provides a genuine second opinion. 40+ reviews completed to date; review
  instructions live in [AGENTS.md](AGENTS.md).
- **Mandatory completion workflow** — every C# feature passes a 4-phase gate before merge:
  build + internal `/deep-review` → Codex adversarial review → self-review of the fixes →
  closeout (issue, feature doc, CHANGELOG).

## Installing to Play (non-developers)

AWOIAF ships as a set of modules. Required alongside the core `DOTS` module (name pending #3):

- Companion modules: **AWOIAF_Map** (the campaign map, provided separately), **A Dance of Dragons
  Armory** (Westerosi equipment) and **DOTS.Dependencies**
- BUTR dependencies: **Harmony** and **Mod Configuration Menu (MCM)**

Place all modules in your Bannerlord `Modules/` directory, enable them in the launcher, and start a
**new campaign** — existing saves are not supported.

## Contributing

1. Read [CLAUDE.md](CLAUDE.md) for coding standards and conventions
2. Write tests first — TDD is mandatory
3. Use the adapter pattern for any TaleWorlds sealed type
4. Keep Harmony patches and entry points thin (< 150 lines); delegate to services
5. Research TaleWorlds behavior before implementing — decompile, don't guess

## License

**Code** (C# mod source): [MIT License](https://opensource.org/licenses/MIT)

**Content** (art, lore, data, and XML assets derived from the works of George R. R. Martin):
[CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/) — non-commercial, attribution
required, share-alike.

This mod is an unofficial fan project. It is not affiliated with, endorsed by, or sponsored by
George R. R. Martin, HBO, Warner Bros. Discovery, or TaleWorlds Entertainment. *A Song of Ice and
Fire* and *Game of Thrones* are trademarks of their respective owners.

## Acknowledgments

- **[A Dance of Dragons](https://www.nexusmods.com/mountandblade2bannerlord/mods/6116)** — the campaign map
  and the Westerosi armory AWOIAF builds on.
- **[The Old Realms (TOR)](https://www.moddb.com/mods/the-old-realms)** — the Career System and
  War Resources were inspired by TOR's Warhammer total conversion. Their career-progression and
  resource-gating designs served as the reference architecture, adapted for a Game of Thrones
  setting.
- **TAOM (Tales from the Age of Men)** — the mod was bootstrapped from the TAOM Bannerlord architecture
  (IoC/DryIoc, the adapter pattern, the TDD infrastructure, and the carried-forward feature modules).
