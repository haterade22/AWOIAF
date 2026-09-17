# CHANGELOG — DOTS (Dawn of the Stag)

## 2026-09-15

### feat: AWOIAF_Map — the campaign-map module, seeded from the ADOD map (editor-load milestone)

- **The map module never existed on disk.** `DOTS_Map` in CLAUDE.md was the bootstrap rename of TAOM's
  LOTR map; the 2026-07-14 ADOD adoption generated the faction layer against `A Dance of Dragons - Map`
  but that module was never copied into `Modules\` (not in the Recycle Bin either — the bin held only
  today's LOTR/TAOM cleanup). The DOTS-prepared copy was intact at `E:\LOTRAOMAssets\A Dance of Dragons - Map`.
- **New module `<game>/Modules/AWOIAF_Map/`** (Id `AWOIAF_Map`, "A World of Ice and Fire - Map", v1.0.0)
  built by `tools/awoiaf_map/create_module.py`: robocopy minus `RuntimeDataCache`/nested duplicate/`*.prev`
  (1,257 files, ~8.7 GB), rendered `SubModule.xml`, `project.mbproj` retargeted. Manifest snapshot at
  `docs/reference/awoiaf-map-snapshot/`. **Decompile evidence (v1.5.3 `ModuleInfo`/`ModuleHelper`):** vanilla
  parses only `DependedModules/DependedModule` (`Optional=` honoured, ordering by presence) and never
  `DependedModuleMetadatas`; `ModuleType` is read, `<Official>` is not — so DOTS is declared
  `<DependedModule Id="DOTS" Optional="true"/>` and the module validates in the editor with DOTS absent.
- **`settlements.xml` migrated to v1.5.3** (`tools/awoiaf_map/migrate_settlements_schema.py`): the ADOD
  1.2.12 `<Hideout scene_name=…>` is dead on 1.5.3 (`Hideout.Deserialize` reads only meshes;
  `HideoutCampaignBehavior:666` and the map tooltip dereference `Settlement.LocationComplex` → NRE on all
  222 hideouts) → each hideout gains a `hideout_complex` Locations block; six hero-city menu meshes with no
  installed tpac (`menuWinterfell`, `menuKingsLanding`, …) remapped to vanilla placeholders. Every other
  ADOD attribute is still read by the 1.5.3 deserializers (audited: 0 missing required attrs across
  520/819/222). Byte-faithful (BOM+CRLF), idempotent, post-checked, atomic.
- **Every settlement scene now resolves inside the module** (`tools/awoiaf_map/port_adod_scenes.py` +
  `remap_stale_scene_names.py`): 58 scene names had no folder in an enabled module (774 settlements;
  `WesterosiGenericKeep` alone 561 castles) → the 115 ADOD mission scenes with their AssetPackages/Prefabs/
  NavMeshPrefabs/Atmospheres ported additively (flora_kinds merged 9+8), two ADOD typos remapped
  (`reach_westerlands_villagee`, `corspe_lake`→`adod_corpse_lake`). `audit_scene_names.py` → 0 crash suspects.
- **Scene tools re-pointed**: `audit_scene_names.py` (`--module`/`--enabled`, enabled-modules-only lookup,
  exit-1 gate) and `remap_stale_scene_names.py` (`--module`, `scene_name_1..3`, LOTR-era REMAP retired);
  `build_adod_distance_cache.py` / `audit_adod_map_refs.py` / `generate_adod_factions.py` target
  `Modules/AWOIAF_Map`. Gates green: faction audit (229 clans / 29 kingdoms), transcoder dry-run
  (1,562 ids, bit-exact), `validate_moduledata.py` PASS.
- **Editor `RGL WARNING: Unable to locate source file $BASE/Modules/A Dance of Dragons/AssetSources/...`**
  (`tools/awoiaf_map/retarget_tpac_sources.py`): editor-form `*_tex.tpac` files hold only metadata + an
  Int32-length-prefixed source path naming the module the asset was originally compiled in (499 x
  `A Dance of Dragons`, 1 x `ADOD_IAF Map`); the compiled pixels were in the 1.2.12 RuntimeDataCache, so
  the 1.5.3 editor recompiles from PNG. Fixed by an equal-length in-place rewrite
  (`A Dance of Dragons/` -> `AWOIAF_Map/././././`; `$BASE/` is a raw string substitution) - 500 tpacs,
  sizes unchanged, every rewritten path verified to resolve before writing. Lesson recorded: `AssetSources`
  is load-bearing for editor-form modules.
- **Editor crashed on every folder creation while mirroring the 59 nested ADOD `AssetSources/MapAssets`
  folders** (2026-09-16, `tools/awoiaf_map/flatten_map_asset_sources.py`): consolidated to exactly `icons/`
  (39 fbx) + `textures/` (491 png/dds, all lowercase). 25 byte-identical duplicates collapsed, 35 Google-Drive
  `desktop.ini` deleted, 2 archives moved up to `AssetSources/`, the two genuinely different `TheReach.fbx` /
  `TheWesterlands.fbx` exports both kept (`_alt`). The editor had already replaced the 2026-09-15 `Assets/`
  tpacs with its own re-import, so no tpac source paths needed rewriting for this.
- **Materials recreated** (2026-09-16, `tools/awoiaf_map/generate_materials.py` + `tpac.py`): the editor
  re-import gave every texture a new GUID and materials reference textures by GUID, so ADOD's 115
  `*_mtl.tpac` were rewritten (texture GUID → name → our lowercased texture → our GUID; fresh package/asset
  GUIDs; checknum recomputed) with name/shader/blend/flags/floats byte-identical. `.tpac` findings, verified
  on real files: TpacTool's layout holds; the per-asset checknum TpacTool writes as 0 is
  **xxHash64(seed 0) over u64 metadataSize + metadata**; `dataOffset` = index size after the 36-byte
  header; material metadata unchanged between 1.2.12 and 1.5.3. 22 slot refs resolve to vanilla Native
  textures and were kept. 114 written, 1 hand-made kept, 0 re-check failures.
- **Meshes ported with ADOD's material assignments** (`tools/awoiaf_map/port_geo_meshes.py`): the fbx
  material-slot names are mostly raw DCC names (166 of 228), so re-import can never auto-bind — the hand
  assignments live in the 33 compiled `*_geo.tpac`. Ported with material GUIDs rebound by name (237 refs),
  checknums recomputed, segment offsets shifted, data verbatim; Geometry fbx checksum (xxHash64 of the file)
  kept satisfied by pointing 10 records at the compiled export copied in as `<stem>_alt.fbx`.
- **Editor crash (10:26) root-caused and fixed**: 140 orphaned ADOD-scene prefabs/nav-mesh prefabs/atmospheres
  left behind after `AssetPackages/` was emptied → `original_meta_mesh_pointers_for_prefabs_[im] != nullptr`
  assert at startup, native crash on opening the resource browser. Removed (byte-identical to source).
- **Editor crash #2 (12:38) = same as #1 (10:26) once the prefabs were gone**: last line `compile_shader:
  water_simulation.rs, main_cs`; the only material on that shader is ADOD's `river`, referenced by nothing.
  Deleted from the module (`SKIP_MATERIALS` keeps it out). Vanilla name collisions un-collided: `grass`→`adod_grass`,
  stray `cube` metamesh→`adod_cube` (`tools/awoiaf_map/rename_tpac_assets.py`, GUIDs kept).
- **Main_map loads in the v1.5.3 editor.** Terrain layers were blank: 23 `<terrain>` texture refs in
  ADOD's mixed case vs our lowercase textures (case-sensitive lookup) → `tools/awoiaf_map/fix_terrain_texture_case.py`.
- Tests: +76 (`test_create_awoiaf_map_module` 14, `test_migrate_settlements_schema` 12,
  `test_port_adod_scenes` 8, `test_scene_name_tools` 5, `test_retarget_tpac_sources` 8,
  `test_flatten_map_asset_sources` 6, `test_tpac` 6, `test_generate_materials` 5, `test_port_geo_meshes` 5,
  `test_rename_tpac_assets` 4, `test_fix_terrain_texture_case` 3); tools suite 215 green.
- Feature doc: `docs/features/awoiaf-map.md` (evidence table, residue, deferred list).
- **Scene damage at 13:00 → full restore (2026-09-16).** An editor save left `scene.xscene` with 749 entities
  renamed/removed and `navmesh.bin` 8× smaller (cause not reproduced by a plain save). `SceneObj/Main_map` +
  `SceneEditData/Main_map/terrain_ed.bin` restored from the pristine ADOD copy, terrain-case fix re-applied;
  damaged files parked in `SceneObj/_damaged_13-00/`. A later accidental save (13:18) verified clean, and the
  editor's per-save rewrite of `settlements.xml` is byte-identical to the migrated file (1,562 settlements,
  222 hideout blocks, BOM+CRLF) — the editor now co-owns that file: edit it only with the editor closed.
- **Editor "Compute settlement cache" works on this map: ~2 h 22 min for 1,562 settlements** (run 1
  14:06→16:28, run 2 16:35→19:12, 2026-09-16): face→closest-settlement 23 min (133,142 navmesh faces),
  settlement-to-settlement 36 min, fortification neighbours 83 min (520), then `Serialize` →
  `DistanceCaches/settlements_distance_cache_Default.bin` (32,982,574 bytes, CRCs match the current scene).
  The 13:32 crash blamed on it was **not** the compute: Windows event log shows `0xe0434352` (unhandled .NET
  exception, not a native AV), the log never reached the compute's first `Found distance cache at:` line, and
  the sibling `SavePositions` / `CheckPositions` handlers in the same `OnEditorVariableChanged` run bare in a
  native→managed callback (only the compute is try/catch'd). Type unknown — no dump retained (`LocalDumps`
  not set for `TaleWorlds.MountAndBlade.Launcher.exe`). Also verified: the cache format is byte-identical
  1.4.8→1.5.3 (`NavigationCache`/`SandBoxNavigationCache`), `Deserialize` reads the two scene CRCs and never
  compares them, and on game load the *last* active module with a `DistanceCaches/*_Default.bin` wins.
- `Main/Properties/launchSettings.json`: launch profiles now list `AWOIAF_Map` (was `ADODMap`).
- **Committed with the build gate bypassed (user's call):** `check-build-before-commit.sh` blocks every commit
  while DOTS has the 3 known 1.5.3 compile errors; nothing in this commit is C#. The gate is back in force
  once the 1.5.3 migration lands. Tagged `v0.1.0`.
- **Repo renamed `haterade22/DOTS` → `haterade22/AWOIAF`** (old URL redirects); tag `v0.1.0` + GitHub release;
  README retitled *AWOIAF — A World of Ice and Fire* with a map-module section; the code-level identity rename
  is scoped in issue #3 (not started — module Id/DLL/namespaces stay `DOTS` for now).
- `Dependencies/_Module/bin/Win64_Shipping_Client/`: the 28 vendored BUTR runtime DLLs (ButterLib/MCM 1.4.x
  shims, BUTR.CrashReport, Serilog, Microsoft.Extensions.*, System.*) are now tracked — the 2026-07-14
  allow-list existed but the files were never added.
- Scene carries **two** `settlement_scripts` entities with `SettlementPositionScript` (inherited from ADOD;
  vanilla has one) — every scene save logs two `opening settlements.xml` rewrites. Harmless so far; trim later.
- **Module folder renamed `AWOIAF Map` → `AWOIAF_Map`** (editor: "Space in scene path! Need to have underscore
  instead!"). Same byte length, so `retarget_tpac_sources.py` re-pointed all 528 `$BASE/Modules/…` source paths
  in place (padding unchanged), `project.mbproj` `XMLDirectory` updated, tool defaults/tests/docs/memory
  follow (98 replacements). Id and display name unchanged.
- `tools/awoiaf_map/restore_texture_flags.py` (dry-run default): re-adds texture flags the re-import dropped
  (`for_terrain`/`dont_degrade`/`for_skybox_sun`) using ADOD's tex tpacs as reference. The user set the flag on
  the 23 terrain-layer textures in the editor; the tool still reports 104 `for_terrain` + 2 others on textures
  no terrain layer currently uses — left alone. No tests yet.

**Scope decisions (user):** editor-load only this pass; **no trimming** (full 1,562-settlement map);
DOTS C# untouched. **Engine drift recorded:** the install is **v1.5.3** (DOTS pins `v1.4.5.*`);
DOTS has 3 compile errors against it (`DefaultExecutionRelationModel`/`TraitLevelingHelper.OnLordExecuted`
deleted, `GovernorDifferentCultureLoyaltyEffect` signature) — TAOM's `8b9f0a23` is the recipe. Deferred with
the C# retarget of `RuntimeCacheRebuildService`/`CacheRebuildConfig`/`cache_rebuild_config.json`/
`launchSettings.json` (still say "A Dance of Dragons - Map"/`ADODMap`), `/verify-bindings`, the Westeros
trim, and the CLAUDE.md `DOTS_Map` rows. NavalDLC must stay disabled.

Not-tested: the editor open of `Main_map` itself (user-side); in-game load is out of scope this pass.

## 2026-07-14

### fix: vendor the BUTR runtime DLLs the bootstrap dropped + VS launch profiles for the ADOD set

- **DOTS.Dependencies was un-launchable** ("Cannot find ... Bannerlord.ButterLib.dll" on boot):
  the DR3 architecture vendors the Workshop-only BUTR runtime DLLs in
  `Dependencies/_Module/bin/Win64_Shipping_Client/` (ButterLib + Implementation.1.4.0/1.4.1,
  MBOptionScreen.v1.4.0/1.4.1, ModuleLoader, MCM.UI.Adapter.MCMv5, BUTR.CrashReport family,
  Serilog + Microsoft.Extensions support set — see `docs/migration/dr3-maintenance.md` Category 2).
  The TAOM bootstrap carried the `.gitignore` ALLOWLIST for these files but not the binaries, so
  the deployed module shipped only `DOTS.Dependencies.dll` while its SubModule.xml declared the
  full stack. Mirrored TAOM's curated set (28 DLLs, both binaries folders; identical
  brand-normalized SubModule.xml verified); build now deploys 34 files. Same set is
  proven-in-use on the same v1.4.7 install by TAOM.
- **VS 2026 F5 launch**: `Main/Properties/launchSettings.json` profiles retargeted from the
  retired LOTR module list (`Alliance.Wargs*LOTRLOME_Armory*DOTS_Map`) to the ADOD set —
  `DOTS.Dependencies*Native*SandBoxCore*CustomBattle*Sandbox*StoryMode*ADODArmoryReleaseVersion*ADOD_Beasts*DOTS*ADODMap`
  (note the armory's module Id is `ADODArmoryReleaseVersion`, not its folder name). Explicit
  `_MODULES_` list also guarantees NavalDLC stays out of the load (see the distance-cache
  constraint). Added `debugEngines: managed-framework,native` from TAOM's newer profile so
  breakpoints bind on .NET Framework 4.7.2. `$(GameFolder)`/`$(GameBinariesFolder)`/`$(ModuleId)`
  plumbing was already in place from the bootstrap.

Not-tested: F5 launch end-to-end (user-side; this was the boot attempt that surfaced the
missing-ButterLib error).

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
