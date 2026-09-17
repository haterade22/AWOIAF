#!/usr/bin/env python3
"""Create the AWOIAF_Map module (Id AWOIAF_Map) from the ADOD map source folder.

DOTS's campaign map is derived from "A Dance of Dragons - Map" (Id ADODMap, Bannerlord 1.2.12-era).
This tool seeds a NEW module folder from that source so the map can be opened in the v1.5.3 Scene
Editor and loaded in-game under DOTS's own module identity. It never modifies the source.

What it copies (robocopy /E, never /MIR — nothing at the target is ever deleted):
  SceneObj/ SceneEdit/ SceneEditData/ Assets/ AssetSources/ Prefabs/ ModuleData/
Excluded: RuntimeDataCache/ (engine-regenerated — but only FROM AssetSources/, which is therefore
load-bearing for editor-form assets; see retarget_tpac_sources.py), the nested "A Dance of Dragons - Map" duplicate,
and *.prev backup artifacts. The legacy ModuleData/settlements_distance_cache.bin IS kept — it is the
INPUT of tools/build_adod_distance_cache.py (needed again after any future settlement trim).

What it writes:
  SubModule.xml            — v1.5.3 vanilla-launcher form. Decompile evidence (TaleWorlds.ModuleManager
                             .ModuleInfo.LoadWithFullPath, installed v1.5.3 via dots-src): only
                             <DependedModules><DependedModule Id= DependentVersion= Optional=/> is parsed;
                             <DependedModuleMetadatas> is never read by vanilla. ModuleHelper
                             .GetDependentModulesOf sorts by DependedModules present in the load set, so
                             Optional="true" on DOTS = load after DOTS when present, no error when absent
                             (editor-only sessions).
  ModuleData/project.mbproj — the source file with only <XMLDirectory> retargeted.

Usage:
    python tools/awoiaf_map/create_module.py                       # DRY-RUN: plan + rendered manifests
    python tools/awoiaf_map/create_module.py --apply               # copy + write manifests
    python tools/awoiaf_map/create_module.py --source X --target Y # override defaults

Guards: refuses to run when the target exists (even empty) and when a required source file is missing —
both BEFORE any byte is copied (RCA 2026-07-14 findings 4/5: structural guard + no partial state).
"""
from __future__ import annotations

import argparse
import os
import re
import subprocess
from pathlib import Path

MODULE_FOLDER = "AWOIAF_Map"
MODULE_ID = "AWOIAF_Map"
MODULE_NAME = "A World of Ice and Fire - Map"
MODULE_VERSION = "v1.0.0"

SOURCE_FOLDER_NAME = "A Dance of Dragons - Map"
DEFAULT_SOURCE = Path(r"E:\LOTRAOMAssets") / SOURCE_FOLDER_NAME
GAME = Path(os.environ.get("BANNERLORD_GAME_DIR",
                           r"E:\Steam\steamapps\common\Mount & Blade II Bannerlord"))
DEFAULT_TARGET = GAME / "Modules" / MODULE_FOLDER

HARD_DEPENDENCIES = ("Native", "SandBoxCore", "Sandbox", "CustomBattle", "StoryMode")
OPTIONAL_DEPENDENCIES = ("DOTS",)

EXCLUDE_DIRS = ("RuntimeDataCache", SOURCE_FOLDER_NAME)
EXCLUDE_FILES = ("*.prev",)

REQUIRED_SOURCE_FILES = (
    "SubModule.xml",
    "SceneObj/Main_map/scene.xscene",
    "ModuleData/settlements.xml",
    "ModuleData/project.mbproj",
    "ModuleData/settlements_distance_cache.bin",
    "ModuleData/DistanceCaches/settlements_distance_cache_Default.bin",
)

CRLF = "\r\n"


# ---------------------------------------------------------------- rendering

def render_submodule_xml() -> str:
    deps = [f'\t\t<DependedModule Id="{d}"/>' for d in HARD_DEPENDENCIES]
    deps += [f'\t\t<DependedModule Id="{d}" Optional="true"/>' for d in OPTIONAL_DEPENDENCIES]
    lines = [
        "<Module>",
        f'\t<Name value="{MODULE_NAME}"/>',
        f'\t<Id value="{MODULE_ID}"/>',
        f'\t<Version value="{MODULE_VERSION}"/>',
        '\t<DefaultModule value="false"/>',
        '\t<ModuleCategory value="Singleplayer"/>',
        '\t<ModuleType value="Community"/>',  # v1.5.3 reads ModuleType; <Official> is never read
        "\t<DependedModules>",
        *deps,
        "\t</DependedModules>",
        "\t<SubModules/>",
        "\t<Xmls>",
        "\t\t<XmlNode>",
        '\t\t\t<XmlName id="Settlements" path="settlements"/>',
        "\t\t\t<IncludedGameTypes>",
        '\t\t\t\t<GameType value="Campaign"/>',
        '\t\t\t\t<GameType value="CampaignStoryMode"/>',
        "\t\t\t</IncludedGameTypes>",
        "\t\t</XmlNode>",
        "\t</Xmls>",
        "</Module>",
    ]
    return CRLF.join(lines) + CRLF


_XMLDIR_RE = re.compile(r"<XMLDirectory>[^<]*</XMLDirectory>")


def render_mbproj(original: str) -> str:
    """Retarget only <XMLDirectory>; every other byte of the source mbproj is preserved."""
    new = f"<XMLDirectory>..\\Modules\\{MODULE_FOLDER}\\</XMLDirectory>"
    out, n = _XMLDIR_RE.subn(lambda _m: new, original, count=1)  # callable: backslashes stay literal
    if n != 1:
        raise SystemExit("project.mbproj has no <XMLDirectory> element — refusing to guess its layout")
    return out


# ---------------------------------------------------------------- guards

def validate_source(src: Path) -> list[str]:
    return [rel for rel in REQUIRED_SOURCE_FILES if not (src / rel).is_file()]


def guard_target(dst: Path) -> None:
    if dst.exists():
        raise SystemExit(f"target already exists: {dst}\n"
                         f"  This tool never overwrites or merges. Move/rename it first.")


# ---------------------------------------------------------------- copy

def robocopy_cmd(src: Path, dst: Path) -> list[str]:
    return [
        "robocopy", str(src), str(dst), "/E",
        "/XD", *[str(src / d) for d in EXCLUDE_DIRS],
        "/XF", *EXCLUDE_FILES,
        "/NFL", "/NDL", "/NJH", "/NJS", "/R:2", "/W:2",
    ]


def run_robocopy(cmd: list[str]) -> None:
    # robocopy exit codes: 0-7 success (bit flags), >= 8 failure
    rc = subprocess.run(cmd, check=False).returncode
    if rc >= 8:
        raise SystemExit(f"robocopy failed with exit code {rc}")


# ---------------------------------------------------------------- main

def main(argv: list[str] | None = None) -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--source", type=Path, default=DEFAULT_SOURCE)
    ap.add_argument("--target", type=Path, default=DEFAULT_TARGET)
    ap.add_argument("--apply", action="store_true", help="copy + write (default: dry-run, writes nothing)")
    args = ap.parse_args(argv)
    src: Path = args.source
    dst: Path = args.target

    missing = validate_source(src)
    if missing:
        raise SystemExit("source is missing required files:\n  " + "\n  ".join(missing) + f"\n  under {src}")
    guard_target(dst)

    # Stage everything in memory before any disk write (no partial state on failure).
    submodule_xml = render_submodule_xml()
    mbproj = render_mbproj((src / "ModuleData" / "project.mbproj").read_bytes().decode("utf-8"))
    cmd = robocopy_cmd(src, dst)

    mode = "APPLY" if args.apply else "DRY-RUN"
    print(f"[{mode}] source : {src}")
    print(f"[{mode}] target : {dst}")
    print(f"[{mode}] copy   : {' '.join(cmd)}")
    print(f"[{mode}] SubModule.xml:\n{submodule_xml}")
    if not args.apply:
        print("[DRY-RUN] nothing written. Re-run with --apply.")
        return

    run_robocopy(cmd)
    (dst / "SubModule.xml").write_bytes(submodule_xml.encode("utf-8"))
    (dst / "ModuleData" / "project.mbproj").write_bytes(mbproj.encode("utf-8"))
    print(f"[APPLY] wrote {dst / 'SubModule.xml'}")
    print(f"[APPLY] wrote {dst / 'ModuleData' / 'project.mbproj'}")
    print("[APPLY] done")


if __name__ == "__main__":
    main()
