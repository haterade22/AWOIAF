# AWOIAF_Map (campaign-map module)

> **Status (2026-09-15): EDITOR-LOAD MILESTONE.** The module exists at
> `<game>/Modules/AWOIAF_Map/` (Id `AWOIAF_Map`, "A World of Ice and Fire - Map"), seeded from the
> ADOD map, schema-migrated for v1.5.3, with every settlement scene resolvable inside the module. It is
> a **data-only external module** — no DOTS code is required to open it in the Scene Editor. In-game
> campaign load with DOTS is a later pass (see *Deferred*).

## Overview

DOTS's Westeros campaign map. A new module folder built by repo tooling from the external
"A Dance of Dragons – Map" (`ADODMap`, Bannerlord 1.2.12-era, 1,562 settlements) plus the mission
scenes from "A Dance of Dragons – Scenes". The tools are idempotent, dry-run by default, and never
touch the ADOD source folders.

## Why This Exists

- The `DOTS_Map` module the older docs mention was only the bootstrap rename of TAOM's LOTR map and
  was never created; DOTS adopted the ADOD map on 2026-07-14 and generated its whole faction layer
  (31 cultures / 229 clans+lords / 29 kingdoms) against that map's settlement ids — but the map module
  itself never made it into `Modules\`.
- The ADOD files are 1.2.12-era. Against the installed **v1.5.3** engine two things were fatal:
  hideouts carried their scene as `<Hideout scene_name=…>` (dead attribute; `Settlement.LocationComplex`
  stays null → NRE in `HideoutCampaignBehavior:666` and the map tooltip `TooltipRefresherCollection:925`),
  and 58 settlement scene names existed only in ADOD's Scenes module (774 settlements affected).
- The user wants a module with its own identity ("A World of Ice and Fire"), not a reinstalled ADOD.

## Architecture

### Design Challenge

Produce a self-identified map module from ~9 GB of external ADOD data, keep every edit reproducible
and byte-faithful (the repo cannot hold the module), and know — from decompile evidence, not guesses —
which 1.2.12 XML constructs v1.5.3 still reads.

### Solution Approach

Three dry-run/apply tools under `tools/awoiaf_map/`, each with unit tests on synthetic trees, run in
order; two existing scene tools re-pointed at the module. Everything the tools author is snapshotted in
the repo (`docs/reference/awoiaf-map-snapshot/`).

```
E:\LOTRAOMAssets\A Dance of Dragons - Map        E:\LOTRAOMAssets\ADOD asset package\Modules\
   (outer folder; DOTS-prepared Jul 14)             A Dance of Dragons - Scenes
          │ create_module.py --apply                        │ port_adod_scenes.py --apply
          ▼                                                 ▼
<game>\Modules\AWOIAF_Map\  ── SubModule.xml (rendered, snapshotted)
   ├─ SceneObj\Main_map + 115 ported mission scenes (Backups / ADOD_Menu_* / main_menu_a excluded)
   ├─ AssetPackages\ (3 ADOD scene tpacs)  Prefabs\  NavMeshPrefabs\  Atmospheres\
   ├─ Assets\ AssetSources\ SceneEdit\ SceneEditData\ Prefabs\          (map's own, copied)
   └─ ModuleData\ settlements.xml (migrated) · settlements.xslt · flora_kinds.xml (merged 9+8)
                  project.mbproj (XMLDirectory retargeted)
                  settlements_distance_cache.bin (legacy — transcoder INPUT, kept)
                  DistanceCaches\settlements_distance_cache_Default.bin (Jul 14 transcode, still valid)
          ▲                       ▲
          │ migrate_settlements_schema.py --apply    │ remap_stale_scene_names.py --apply
```

### Decompile evidence (installed v1.5.3 via `pwsh tools/dots-src.ps1 path …`)

| Finding | Where | Consequence |
|---|---|---|
| Vanilla `ModuleInfo.LoadWithFullPath` parses only `DependedModules/DependedModule` (`Id`, `DependentVersion`, `Optional`), `ModulesToLoadAfterThis`, `IncompatibleModules`, `SubModules`; **never** `DependedModuleMetadatas`. `ModuleHelper.GetDependentModulesOf` sorts by `DependedModules` present in the load set. | `TaleWorlds.ModuleManager.ModuleInfo/ModuleHelper` | `SubModule.xml` uses `<DependedModule Id="DOTS" Optional="true"/>`: orders after DOTS when present, validates without it (editor). `ModuleType` is read (enum default `Community`); `<Official>` is not. |
| `Settlement/Town/Village/Hideout.Deserialize` still read every attribute ADOD emits; `Town.prosperity` and `Village.hearth` are **required** on a new campaign; `gate_posX/Y`, `text`, `max_prosperity`, `port_posX/Y`, `ferry_target` optional. | `TaleWorlds.CampaignSystem.Settlements.*` | No attribute stripping needed; ADOD supplies all required ones (audited: 520 towns/castles, 819 villages, 222 hideouts, 0 missing). |
| `Hideout.Deserialize` reads only the three mesh attributes; the scene comes from `LocationComplex.GetLocationWithId("hideout_center")`. | `Hideout.cs`, `HideoutCampaignBehavior.cs:666`, `TooltipRefresherCollection.cs:925` | Migration T1: each of the 222 hideouts gains `<Locations complex_template="…hideout_complex"><Location id="hideout_center" scene_name=…/></Locations>`; the dead attribute is removed. |

### What the migration changed in `settlements.xml`

- **T1** 222 hideouts → `hideout_complex` Locations block (3 inserted lines each; tag edited in place).
- **T2** six hero-city menu meshes absent from any installed tpac remapped to vanilla placeholders:
  Storm's End `menuStormsEnd`→`menu_empire_seaside_1`; The Eyrie `menuEyrie`→`gui_bg_town_battania`;
  Winterfell `menuWinterfell`→`gui_bg_town_sturgia`; The Dreadfort `battania_town_3`→`gui_bg_town_battania`;
  King's Landing `menuKingsLanding`→`menu_empire_1`; Dragonstone `menuDragonstone`→`menu_empire_seaside_2`
  (wait meshes to the matching `wait_*_town`). Table: `MESH_REMAP` in the tool.
- **Scene typos** `reach_westerlands_villagee`→`reach_westerlands_village` (1), `corspe_lake`→`adod_corpse_lake` (4).
- Everything else byte-identical (BOM + CRLF preserved). Settlement ids untouched.

## Configuration

No repo config. Tool defaults: `BANNERLORD_GAME_DIR` (fallback `E:\Steam\…\Mount & Blade II Bannerlord`),
source folders under `E:\LOTRAOMAssets\`. All tools take `--source/--target` or `--xml/--vanilla` overrides.

## Key Files

| File | Purpose |
|---|---|
| `tools/awoiaf_map/create_module.py` | robocopy the ADOD map (minus `RuntimeDataCache`, nested duplicate, `*.prev`) + render `SubModule.xml` + retarget `project.mbproj`. Refuses an existing target. |
| `tools/awoiaf_map/migrate_settlements_schema.py` | T1 hideout Locations + T2 mesh remap; post-checks (well-formed, id sequence, every hideout has one `hideout_center`, all meshes vanilla, line-count arithmetic); temp+`os.replace` write. |
| `tools/awoiaf_map/port_adod_scenes.py` | additive robocopy (`/XC /XN /XO`) of `SceneObj/AssetPackages/Prefabs/NavMeshPrefabs/Atmospheres`; `flora_kinds.xml` merged by name. |
| `tools/awoiaf_map/retarget_tpac_sources.py` | equal-length in-place rewrite of the `$BASE/Modules/<orig module>/AssetSources/…` source path in each editor-form `*_tex.tpac` (500 files); refuses unless every path resolves. |
| `tools/awoiaf_map/flatten_map_asset_sources.py` | `AssetSources/MapAssets` → exactly `icons/` (fbx) + `textures/` (lowercased); dedupes identical copies, keeps differing same-name files as `_alt`, deletes `desktop.ini`, moves archives up. Editor crashes on folder creation motivated it (2026-09-16). |
| `tools/awoiaf_map/tpac.py` | `.tpac` reader/writer library: header, single asset, material slots, checknum (xxHash64), packed-index walker. |
| `tools/awoiaf_map/generate_materials.py` | ADOD's 115 `*_mtl.tpac` rewritten against our texture GUIDs into `Assets/MapAssets/textures/`. |
| `tools/awoiaf_map/port_geo_meshes.py` | ADOD's 33 `*_geo.tpac` meshes with material assignments rebound and fbx sources retargeted (checksum-matched). |
| `tools/awoiaf_map/fix_terrain_texture_case.py` | lowercase the 23 `<terrain>` texture refs (case-sensitive lookup); same length, BOM/CRLF kept. |
| `tools/awoiaf_map/restore_texture_flags.py` | re-add `for_terrain`/`dont_degrade`/`for_skybox_sun` from ADOD's tex tpacs (dry-run reports what is missing). |
| `tools/audit_scene_names.py` | `--module`/`--enabled`; exit 1 when the map references a scene with no `SceneObj` folder in an enabled module. |
| `tools/remap_stale_scene_names.py` | `--module`; verified remaps of `scene_name` and `scene_name_1..3`. |
| `tools/build_adod_distance_cache.py`, `tools/audit_adod_map_refs.py`, `tools/generate_adod_factions.py` | retargeted to `Modules/AWOIAF_Map` (folder constant only). |
| `docs/reference/awoiaf-map-snapshot/SubModule.xml` | byte-identical copy of the live manifest. |

## Dependencies

External source folders (not in repo): `E:\LOTRAOMAssets\A Dance of Dragons - Map` (outer),
`E:\LOTRAOMAssets\ADOD asset package\Modules\A Dance of Dragons - Scenes`. Engine v1.5.3 (modding kit
for the editor). **NavalDLC must be disabled** — with it enabled `SettlementPositionScript` demands
`_Naval`/`_All` caches and picks NavalDLC's Calradia bins (see `build_adod_distance_cache.py`).

## Tests

`tools/tests/test_create_awoiaf_map_module.py` (14), `test_migrate_settlements_schema.py` (12),
`test_port_adod_scenes.py` (8), `test_scene_name_tools.py` (5), `test_retarget_tpac_sources.py` (8),
`test_flatten_map_asset_sources.py` (6), `test_tpac.py` (6), `test_generate_materials.py` (5),
`test_port_geo_meshes.py` (5), `test_rename_tpac_assets.py` (4), `test_fix_terrain_texture_case.py` (3). Run
`python -m unittest discover -s tools/tests -p "test_*.py"`. Gates run green against the live module on
2026-09-15: `audit_adod_map_refs.py` (229 clans / 29 kingdoms consistent), `audit_scene_names.py`
(0 crash suspects, 215 scene names), `build_adod_distance_cache.py` dry-run (1,562 ids, 1,219,141 pairs,
bit-exact), `validate_moduledata.py` PASS.

## How to open the map in the Scene Editor

1. Launcher from `bin\Win64_Shipping_wEditor`: enable `Native, SandBoxCore, Sandbox, CustomBattle,
   StoryMode, AWOIAF_Map`; keep `TAOM_Map` (also ships a `Main_map`), `TAOM`, `LOTRLOME_Armory`,
   `NavalDLC` **off**.
2. Open `Main_map`, let the editor upgrade/re-save; regenerate navmesh/flora if prompted.
3. Check `rgl_log_*.txt` for scene errors; `SceneObj\Main_map\scene.xscene` gets a fresh mtime.
4. **Every save also rewrites `ModuleData\settlements.xml`** (`SettlementPositionScript.OnSceneSave` →
   `SaveSettlementPositions`, verified byte-identical to the migrated file on 2026-09-16). Edit that file, the
   scene, or any tpac only with the editor closed.
5. **The editor's "Compute settlement cache" (script panel → `ComputeAndSaveSettlementDistanceCache`) works:
   ~2 h 22 min** at 1,562 settlements (measured twice, 2026-09-16): face→closest-settlement 23 min (133,142
   faces), settlement-to-settlement 36 min, fortification neighbours 83 min (520), then it writes
   `DistanceCaches\settlements_distance_cache_Default.bin` (32,982,574 bytes) in one go at the end — killing the
   editor mid-run loses nothing. The `DistanceCaches` folder must exist first. Format is byte-identical
   1.4.8→1.5.3; `Deserialize` reads the two scene CRCs and never compares them; on game load the *last* active
   module with such a file wins (`SettlementPositionScript.ReadNavigationCacheForNavigationTypeOnGameLoad`),
   and a read failure falls into the in-game `GenerateCacheData()` — so never ship a truncated file.
   **The other two checkboxes (`SavePositions`, `CheckPositions`) are not try/catch'd**: a .NET exception in
   them is fatal (`0xe0434352` in the Windows event log, nothing in `rgl_log`) — that is what the 13:32 crash
   on 2026-09-16 was, not the compute. To capture the type next time, set WER `LocalDumps` for
   `TaleWorlds.MountAndBlade.Launcher.exe` (admin) and read the dump with the Store WinDbg's `cdb.exe` + SOS.
6. The folder is `AWOIAF_Map` (no space) — the editor warns "Space in scene path! Need to have underscore
   instead!" otherwise. Renamed 2026-09-16; `retarget_tpac_sources.py --module-name AWOIAF_Map` re-pointed the
   528 baked source paths (same length as `AWOIAF Map`, padding unchanged).

## How to rebuild the module from scratch

```powershell
Remove-Item -Recurse "E:\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\AWOIAF_Map"   # only if you mean it
python tools/awoiaf_map/create_module.py --apply
python tools/awoiaf_map/migrate_settlements_schema.py --apply
python tools/awoiaf_map/port_adod_scenes.py --apply
python tools/remap_stale_scene_names.py --apply
python tools/awoiaf_map/retarget_tpac_sources.py --apply
python tools/audit_scene_names.py && python tools/audit_adod_map_refs.py && python tools/build_adod_distance_cache.py
```

### Editor-form assets: `AssetSources` is load-bearing, not optional

Each `Assets/**/*_tex.tpac` (~538 bytes) holds only metadata — asset name, an **Int32-length-prefixed
source path** (`$BASE/Modules/<module>/AssetSources/…`) and an 8-byte source hash. The compiled pixels
lived in the 1.2.12 `RuntimeDataCache` (excluded from the module, invalid on 1.5.3 anyway), so the 1.5.3
editor recompiles every texture from its PNG. The recorded module is the one the asset was *originally*
compiled in: 499 tpacs said `A Dance of Dragons` (ADOD's main module), 1 said `ADOD_IAF Map` — hence
`RGL WARNING: Unable to locate source file $BASE/Modules/A Dance of Dragons/AssetSources/MapAssets/icons/…`.
Fix: `retarget_tpac_sources.py` pads the module segment with `./` no-op components to keep the byte length
(`A Dance of Dragons/` → `AWOIAF_Map/././././`); `$BASE/` is substituted as a raw string
(`TaleWorlds.Engine.Utilities.GetFullFilePathOfScene`-style `.Replace("$BASE/", …)`), and a `Modules\`
folder without `SubModule.xml` is skipped by `ModuleHelper.GetPhysicalModules`, so a directory junction
would also have worked — the patch keeps the module self-contained instead. Consequence for the copy
step: **never drop `AssetSources`** from an editor-form module (the plan's "runtime-only" copy option
would have broken the editor). The three ported scene packs under `AssetPackages/` also carry 284 source
paths (`A Dance of Dragons Scenes`, `A_Dance_of_Dragons_Scenes`, `HART_scenes`) but are packed with
compiled data and their sources were never shipped — if the editor ever asks for one of those, there is
no file on this machine to point it at.

### Materials (2026-09-16) — `.tpac` format facts that made it possible

The editor re-import gave every texture a **new asset GUID**, and a material references its textures by
GUID only (TpacTool `Material.cs`: `(i32 slot, guid texture)*`), so ADOD's `*_mtl.tpac` could not be
copied. `generate_materials.py` rewrote all 115 (114 written + the hand-made `nights_watch_trim` kept):
ADOD texture GUID → ADOD texture name → same name lowercased in our `*_tex.tpac` index → our GUID.
Verified on the real files: (1) the single-asset layout is TpacTool's (`TPAC`, v2, package GUID, count,
dataOffset = index size after the 36-byte header, reserve; type GUID, asset GUID, version, i32-prefixed
name, u64 metadataSize, metadata, i64 checknum, i32 segments, i32 deps); (2) the checknum TpacTool
writes as 0 is **xxHash64(seed 0) over the 8-byte metadataSize + metadata** — matched on an ADOD
1.2.12 material, a fresh 1.5.3 material and a 1.5.3 texture; (3) material metadata is byte-identical
between 1.2.12 and 1.5.3 (version 0 / subVersion 2), so ADOD's shader GUIDs (4, all vanilla), blend
modes (`no_alpha_blend` 109 / `factor` 6), `two_sided` / `multi_pass_alpha`, `use_specular` /
`alpha_test` / `use_parallaxmapping` / … and every float carry over untouched. 22 slot references
point at vanilla Native textures (`default_specular`, `empire_wall_b_*`, editor placeholders, water
detail maps) and were left as-is after resolving them in `Native/*.tpac` indices. Meshes (`_geo`) are
not generated — the editor's fbx import creates them and binds materials by name, which is why the
material names had to stay exactly ADOD's.

### Meshes (2026-09-16) — why the fbx re-import showed "missing materials"

The 39 fbx carry 228 material-slot names, of which only 62 are ADOD material names; the other 166 are raw
DCC names (`base.001`, `DW_House_07`, `STANDARD MTL`, `Material.025`, `179`, …). ADOD's artists assigned
real materials by hand in the editor, and those assignments exist only inside the compiled `*_geo.tpac`
(33 files, 120 metameshes, 227 sub-meshes, 94 distinct materials — all among the 115). Re-importing an fbx
therefore always shows unbound slots. `port_geo_meshes.py` ports the compiled meshes instead: material
GUIDs rebound by name (237 refs; 4 untouched — Yi-Ti trees + one King's Landing slot reference GUIDs from
no ADOD source), per-asset checknums recomputed, segment offsets shifted by the index delta, data verbatim.
The Geometry record stores **xxHash64 of the fbx bytes**; a mismatch makes the editor re-import, so each
record is pointed at a checksum-matching fbx. 23 matched our `icons/` copies; for 10 (Crownlands, Dorne,
FreeFolk, IronIslands, StormsEnd, TheNorth, TheRiverlands, TheStormlands, TheVale, TheWall) the file in
`icons/` is a newer export than the one ADOD compiled, so the compiled export was copied in as
`<stem>_alt.fbx` (same meaning as the two `_alt` from the flatten: ADOD's compiled/subfolder export).
Metamesh/mesh version numbers: ADOD (1,1,1) vs vanilla 1.5.3 (1,1,2) — same forward-compatible gating as
materials; revert = delete `Assets/MapAssets/icons/*_geo.tpac`.

### Editor crash 2026-09-16 10:26 (RCA)

`AssetPackages/` (the ported ADOD scene packs) was emptied in the morning, but the 122 scene prefabs +
11 nav-mesh prefabs + 7 atmospheres ported alongside them on the 15th were still in the module. At startup
the editor loaded those prefabs, logged `Unable to find metamesh kl_house_*` / `adodfakehouse*` /
`Unable to find material: hart_bricks_cool` and the assert `original_meta_mesh_pointers_for_prefabs_[im]
!= nullptr`; opening the resource browser three minutes later crashed the process natively. Fix: the 140
orphaned scene files were removed (byte-identical to the Scenes source, so re-portable); the map's own
three prefabs stay. Lesson: the scene port is one unit — packs, prefabs, nav-mesh prefabs and atmospheres
go in and come out together.

### Editor crashes 12:38 / 10:26 — the `river` material (RCA)

Both sessions with materials present died on the same last log line, `compile_shader: water_simulation.rs,
main_cs` (a native exception in `TaleWorlds.DotNet.AutoGenerated.dll`), ~4 min after the resource browser
opened; sessions without materials never compiled that shader. The only material on the `water_simulation`
shader is ADOD's `river` (no flags); nothing references it (0 meshes, 0 scene entities — the map's water is a
terrain layer). Deleted from the module (user's call) and listed in `SKIP_MATERIALS`; ADOD's original stays in the source folder.
Also fixed the two vanilla name collisions the editor warned about: material `grass` → `adod_grass`
(`RENAMED_MATERIALS`) and the stray `cube` metamesh in `riverrun_geo` → `adod_cube` (`rename_tpac_assets.py`;
GUIDs unchanged so nothing else moves). Remaining harmless overrides: the four `acacia_bark_1_*`,
`flora_color_variation`, `worldmap_wave_map` textures ADOD copied from vanilla under the same names.
**Map loads in the editor (2026-09-16 ~12:50).** Terrain layers came up blank: `scene.xscene` referenced
their textures in ADOD's original mixed case (`Grass003_4K-PNG_Color`) and terrain-layer lookup IS
case-sensitive (asset overrides are not). `fix_terrain_texture_case.py` lowercased the 23 refs (7 layers);
vanilla refs untouched; binary terrain files carry no names.

### AssetSources layout (2026-09-16)

`AssetSources/MapAssets/` is flat on purpose: `icons/` (39 fbx) and `textures/` (491 lowercase png/dds).
The editor crashed whenever it had to create a folder while mirroring the 59 nested ADOD folders; the
user re-imports through two hand-made folders instead. The 2026-09-15 `Assets/` tpacs (and their
retargeted source paths) were replaced by the editor's own re-import, so `retarget_tpac_sources.py` is
now only relevant to a fresh `create_module.py` copy. `TheReach_alt.fbx` / `TheWesterlands_alt.fbx` are
ADOD's second, smaller exports (both versions ship in the original); the two archives live in
`AssetSources/`. Fbx filenames were left as-is (several contain spaces / near-duplicates).

## Known residue

- ~99 of 4,412 prefab names referenced by the ported scenes resolve in no installed module (vanilla
  prefabs renamed/removed since 1.2.12, e.g. `aserai_castle_tower_roof_1`, `SpawnPointDebugView`); the
  engine skips a missing prefab entity — expect a few absent props inside those scenes.
- 10 settlements are off-navmesh in the ADOD scene (unreachable in the distance cache): `castle_NoxixRedwyne_nox1`,
  `hideout_forest_26/62`, `hideout_seaside_8/10/11/34/47`, `retirement_retreat`, `village_A2_2`.
- Six hero-city game-menu backgrounds are vanilla placeholders until ADOD's menu tpacs are located.
- An editor save at 13:00 on 2026-09-16 left the scene with 749 entities renamed/removed and the navmesh 8×
  smaller; not reproduced by a plain save. `SceneObj/Main_map` + `SceneEditData/Main_map/terrain_ed.bin` were
  restored from the pristine ADOD copy (terrain-case fix re-applied); the damaged files are parked in
  `SceneObj/_damaged_13-00/`. If it recurs, compare against that folder before saving again.
- 104 ADOD-flagged textures still lack `for_terrain` (plus `dont_degrade` on `ADOD_IAF_VistaMap`,
  `for_skybox_sun` on `008_d`); none is used by a terrain layer today. `restore_texture_flags.py --apply` fixes them.
- 7 metamesh material references dangle in ADOD's own `*_geo.tpac` sources (inherited, not introduced).
- Two `settlement_scripts` entities each carry `SettlementPositionScript` (ADOD's original has both; vanilla has
  one). Every scene save rewrites `settlements.xml` twice and both react to the script-panel checkboxes. Remove
  one during the trim pass.

## Deferred (explicitly)

- **In-game load.** Either (a) with DOTS: fix the 3 compile errors DOTS has against v1.5.3
  (`DefaultExecutionRelationModel` / `TraitLevelingHelper.OnLordExecuted` deleted → TAOM re-homed
  Execution onto `TraitLevelingHelper.OnBloodFeudStarted` + `ExecutionCampaignBehavior.GetBloodFeudStartRelationPenaltyToOtherClan`
  in TAOM commit `8b9f0a23`; `DotsSettlementLoyaltyModel.GovernorDifferentCultureLoyaltyEffect` signature),
  retarget `RuntimeCacheRebuildService.ResolveCacheOutputPath` / `CacheRebuildConfig` / `cache_rebuild_config.json` /
  `launchSettings.json` (`ADODMap`→`AWOIAF_Map`) from "A Dance of Dragons - Map", `/verify-bindings`
  (TAOM found `ClanPartyItemVM.UpdateProperties` went abstract — DOTS's `Patch17_TroopWeight` targets it),
  add `executioner=` to every culture; or (b) without DOTS: ship the faction-layer XML inside the module.
- Westeros trim (settlements + scene entities), settlement re-ID to region codes, Essos terrain.
- `CLAUDE.md` / `docs/reference/DOTS-map-settlement-naming.md` / `.claude/memory` still describe the retired
  `DOTS_Map`; point them here.

## GitHub Issue

Not yet opened (public artifact — needs the user's OK).
