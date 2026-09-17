#!/usr/bin/env python3
"""Port ADOD's mission scenes into the AWOIAF_Map module so every settlement scene_name resolves
without loading any ADOD module.

Why: the ADOD map's settlements.xml references 58 scene_names that are not vanilla (audit 2026-09-15:
774 of 1,562 settlements — WesterosiGenericKeep alone backs 561 castle locations). 56 of them live in
"A Dance of Dragons - Scenes" (Id ADOD_Scenes); the other two are ADOD typos handled by
tools/remap_stale_scene_names.py. Remapping 56 custom scenes to vanilla would gut the map's look, so the
scenes come into OUR module instead.

What is copied (robocopy /E /XC /XN /XO — additive only, an existing target file is never overwritten):
  SceneObj/*          minus Backups, ADOD_Menu_* and main_menu_a (ADOD's main-menu replacements)
  AssetPackages/*.tpac, Prefabs/, NavMeshPrefabs/, Atmospheres/
Not copied: EmAssetPackages/ (editor-form duplicates of the packed tpacs), Shaders/ (engine cache),
ModuleData/project.mbproj, SubModule.xml.
Merged: ModuleData/flora_kinds.xml — the map's file keeps its bytes; the scenes' <flora_kind> entries are
appended before </flora_kinds>; a name defined in both with different bodies is a hard error.

Known residue (recorded, not fixed here): ~99 of 4,412 prefab names referenced by these scenes resolve
in no installed module (vanilla prefabs renamed/removed since 1.2.12, e.g. aserai_castle_tower_roof_1,
SpawnPointDebugView). The engine logs and skips a missing prefab entity; expect a few absent props.

Usage:
    python tools/awoiaf_map/port_adod_scenes.py            # DRY-RUN
    python tools/awoiaf_map/port_adod_scenes.py --apply
    ... [--source DIR] [--target DIR]
"""
from __future__ import annotations

import argparse
import os
import re
import subprocess
from pathlib import Path

GAME = Path(os.environ.get("BANNERLORD_GAME_DIR",
                           r"E:\Steam\steamapps\common\Mount & Blade II Bannerlord"))
DEFAULT_SOURCE = Path(r"E:\LOTRAOMAssets\ADOD asset package\Modules\A Dance of Dragons - Scenes")
DEFAULT_TARGET = GAME / "Modules" / "AWOIAF_Map"

COPY_FOLDERS = ("SceneObj", "AssetPackages", "Prefabs", "NavMeshPrefabs", "Atmospheres")
SCENEOBJ_EXCLUDED_DIRS = ("Backups", "ADOD_Menu_*", "main_menu_a")
ADDITIVE_FLAGS = ("/E", "/XC", "/XN", "/XO", "/NFL", "/NDL", "/NJH", "/NJS", "/R:2", "/W:2")

_KIND_RE = re.compile(r"<flora_kind\s+name=\"([^\"]+)\"[^>]*>.*?</flora_kind>", re.DOTALL)


# ---------------------------------------------------------------- flora_kinds merge

def _kinds(text: str) -> dict[str, str]:
    return {m.group(1): m.group(0) for m in _KIND_RE.finditer(text)}


def merge_flora_kinds(existing: str, incoming: str) -> str:
    """Append incoming <flora_kind> blocks missing from `existing`; existing bytes are preserved."""
    nl = "\r\n" if "\r\n" in existing else "\n"
    have, new = _kinds(existing), _kinds(incoming)
    to_add = []
    for name, block in new.items():
        if name in have:
            if re.sub(r"\s+", " ", have[name]) != re.sub(r"\s+", " ", block):
                raise SystemExit(f"flora_kind \"{name}\" exists in both files with different bodies — merge by hand")
            continue
        to_add.append(block)
    if not to_add:
        return existing
    close = "</flora_kinds>"
    idx = existing.rfind(close)
    if idx < 0:
        raise SystemExit("existing flora_kinds.xml has no </flora_kinds> close tag")
    body = nl.join("\t" + b.replace("\r\n", "\n").replace("\n", nl) for b in to_add) + nl
    return existing[:idx] + body + existing[idx:]


# ---------------------------------------------------------------- copy plan

def robocopy_cmds(src: Path, dst: Path) -> list[list[str]]:
    cmds = []
    for folder in COPY_FOLDERS:
        cmd = ["robocopy", str(src / folder), str(dst / folder), *ADDITIVE_FLAGS]
        if folder == "SceneObj":
            cmd += ["/XD", *SCENEOBJ_EXCLUDED_DIRS]  # bare names: robocopy rejects wildcards in full paths
        cmds.append(cmd)
    return cmds


def run_robocopy(cmd: list[str]) -> None:
    rc = subprocess.run(cmd, check=False).returncode
    if rc >= 8:  # 0-7 are success bit flags
        raise SystemExit(f"robocopy failed ({rc}): {' '.join(cmd)}")


# ---------------------------------------------------------------- main

def main(argv: list[str] | None = None) -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--source", type=Path, default=DEFAULT_SOURCE)
    ap.add_argument("--target", type=Path, default=DEFAULT_TARGET)
    ap.add_argument("--apply", action="store_true", help="copy + merge (default: dry-run, writes nothing)")
    args = ap.parse_args(argv)
    src, dst = args.source, args.target

    for rel in ("SceneObj", "AssetPackages", "ModuleData/flora_kinds.xml"):
        if not (src / rel).exists():
            raise SystemExit(f"source is missing {rel} under {src}")
    if not (dst / "SubModule.xml").is_file() or not (dst / "SceneObj" / "Main_map").is_dir():
        raise SystemExit(f"target is not the AWOIAF_Map module (no SubModule.xml + SceneObj/Main_map): {dst}")

    flora_path = dst / "ModuleData" / "flora_kinds.xml"
    existing_raw = flora_path.read_bytes()
    had_bom = existing_raw.startswith(b"\xef\xbb\xbf")
    merged = merge_flora_kinds(existing_raw.decode("utf-8-sig"),
                               (src / "ModuleData" / "flora_kinds.xml").read_bytes().decode("utf-8-sig"))
    cmds = robocopy_cmds(src, dst)
    scenes = sorted(p.name for p in (src / "SceneObj").iterdir()
                    if p.is_dir() and p.name != "Backups" and not p.name.startswith("ADOD_Menu_") and p.name != "main_menu_a")

    mode = "APPLY" if args.apply else "DRY-RUN"
    print(f"[{mode}] source : {src}")
    print(f"[{mode}] target : {dst}")
    print(f"[{mode}] scenes to port: {len(scenes)} (excluding Backups / ADOD_Menu_* / main_menu_a)")
    for c in cmds:
        print(f"[{mode}] copy   : {' '.join(c)}")
    added = len(_kinds(merged)) - len(_kinds(existing_raw.decode('utf-8-sig')))
    print(f"[{mode}] flora_kinds.xml: +{added} kinds")
    if not args.apply:
        print("[DRY-RUN] nothing written. Re-run with --apply.")
        return

    for c in cmds:
        run_robocopy(c)
    if merged != existing_raw.decode("utf-8-sig"):
        tmp = flora_path.with_suffix(".xml.tmp")
        tmp.write_bytes((b"\xef\xbb\xbf" if had_bom else b"") + merged.encode("utf-8"))
        os.replace(tmp, flora_path)
        print(f"[APPLY] wrote {flora_path}")
    missing = [s for s in scenes if not (dst / "SceneObj" / s).is_dir()]
    if missing:
        raise SystemExit(f"post-check: {len(missing)} scenes not present after copy: {missing[:5]}")
    print(f"[APPLY] done — {len(scenes)} scenes present under {dst / 'SceneObj'}")


if __name__ == "__main__":
    main()
