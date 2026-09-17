#!/usr/bin/env python3
"""
Audit settlement scene_name references against the SceneObj folders of the modules that will
actually be enabled.

Motivation: a settlement (or hideout) whose scene_name has no SceneObj folder in an enabled module
crashes when the player enters a battle/encounter there. TaleWorlds renames/removes scenes between
versions, and the AWOIAF_Map (seeded from ADOD's 1.2.12 map) references custom scenes that must be
ported into the module itself (tools/awoiaf_map/port_adod_scenes.py).

Outputs:
  1. CRASH SUSPECTS: scene_names referenced by the map module with NO folder in any enabled module.
  2. The same check for vanilla SandBox (engine baseline sanity).
  3. Map-vs-vanilla diff (custom scenes the map uses).
Exit code 1 when the map module has crash suspects — usable as a gate.

Usage:
    python tools/audit_scene_names.py                          # AWOIAF_Map, default enabled set
    python tools/audit_scene_names.py --module "AWOIAF_Map" --enabled Native SandBox SandBoxCore CustomBattle StoryMode
"""
from __future__ import annotations

import argparse
import os
import re
from pathlib import Path

GAME = Path(os.environ.get("BANNERLORD_GAME_DIR",
                           r"E:\Steam\steamapps\common\Mount & Blade II Bannerlord"))
MODULES = GAME / "Modules"
DEFAULT_MODULE = "AWOIAF_Map"
DEFAULT_ENABLED = ("Native", "SandBox", "SandBoxCore", "CustomBattle", "StoryMode")

SCENE_RE = re.compile(r'scene_name(?:_\d)?="([^"]+)"')
SETTLEMENT_RE = re.compile(r'<Settlement\b[^>]*\bid="([^"]+)"[^>]*>(.*?)</Settlement>', re.DOTALL)


def scenes_in(text: str) -> dict[str, list[str]]:
    """scene_name -> settlement ids referencing it (scene_name, scene_name_1..3)."""
    out: dict[str, list[str]] = {}
    for m in SETTLEMENT_RE.finditer(text):
        sid, body = m.group(1), m.group(2)
        for sm in SCENE_RE.finditer(body):
            out.setdefault(sm.group(1), []).append(sid)
    return out


def scene_folders(modules_dir: Path, enabled: list[str]) -> dict[str, str]:
    """lowercased SceneObj folder name -> first enabled module providing it (Windows lookup is
    case-insensitive, so a case-only difference is NOT a crash)."""
    found: dict[str, str] = {}
    for mod in enabled:
        so = modules_dir / mod / "SceneObj"
        if not so.is_dir():
            continue
        for child in so.iterdir():
            if child.is_dir():
                found.setdefault(child.name.lower(), mod)
    return found


def missing_scenes(refs: dict[str, list[str]], folders: dict[str, str]) -> dict[str, list[str]]:
    return {s: ids for s, ids in refs.items() if s.lower() not in folders}


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--module", default=DEFAULT_MODULE, help="map module folder under Modules/")
    ap.add_argument("--enabled", nargs="+", default=list(DEFAULT_ENABLED),
                    help="other enabled module folders whose SceneObj count")
    args = ap.parse_args(argv)
    enabled = [*args.enabled, args.module]

    folders = scene_folders(MODULES, enabled)
    print(f"SceneObj folders across enabled modules {enabled}: {len(folders)}\n")

    map_xml = MODULES / args.module / "ModuleData" / "settlements.xml"
    van_xml = MODULES / "SandBox" / "ModuleData" / "settlements.xml"
    refs = {}
    for label, p in (("map", map_xml), ("vanilla_SandBox", van_xml)):
        if not p.is_file():
            raise SystemExit(f"missing: {p}")
        refs[label] = scenes_in(p.read_text(encoding="utf-8-sig", errors="replace"))
        print(f"{label}: {len(refs[label])} distinct scene_names ({p})")

    rc = 0
    for label in ("map", "vanilla_SandBox"):
        miss = missing_scenes(refs[label], folders)
        print("\n" + "=" * 70)
        print(f"CRASH SUSPECTS [{label}] — referenced but NO SceneObj folder in any enabled module: {len(miss)}")
        print("=" * 70)
        for s, ids in sorted(miss.items(), key=lambda kv: -len(kv[1])):
            print(f"  {s:42s} n={len(ids):4d}  <- {', '.join(ids[:4])}{' ...' if len(ids) > 4 else ''}")
        if miss and label == "map":
            rc = 1
    only_map = sorted(set(refs["map"]) - set(refs["vanilla_SandBox"]))
    print("\n" + "=" * 70)
    print(f"DIFF — map scenes not used by vanilla: {len(only_map)}")
    print("=" * 70)
    for s in only_map:
        print(f"  [{folders.get(s.lower(), 'MISSING')}] {s}")
    return rc


if __name__ == "__main__":
    raise SystemExit(main())
