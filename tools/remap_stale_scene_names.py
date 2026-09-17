#!/usr/bin/env python3
"""
Remap stale settlement scene_name references in the map module's settlements.xml to scenes that
actually exist on disk. Found via tools/audit_scene_names.py.

History:
  - 2026-05-28 (DOTS_Map, LOTR era): 4 vanilla house interiors renamed in v1.4.5 + 4 custom-scene
    typos/absences. That module is retired; those entries are gone.
  - 2026-09-15 (AWOIAF_Map, ADOD-derived): after tools/awoiaf_map/port_adod_scenes.py brought the 115
    ADOD scenes into the module, two ADOD typos remained with no scene anywhere:
      reach_westerlands_villagee -> reach_westerlands_village   (1 village, double 'e')
      corspe_lake                -> adod_corpse_lake            (castle_NoxixIronIslands_nox3, 4 refs)
Every replacement is verified to exist as a SceneObj folder (case-insensitive) in an enabled module
before anything is written; scene_name and scene_name_1..3 are all remapped; I/O is byte-faithful
(BOM + CRLF preserved).

Usage:
    python tools/remap_stale_scene_names.py --dry-run
    python tools/remap_stale_scene_names.py --apply [--backup] [--module "AWOIAF_Map"]
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

REMAP = {
    "reach_westerlands_villagee": "reach_westerlands_village",
    "corspe_lake": "adod_corpse_lake",
}


def scene_folders_lower(modules_dir: Path, enabled: list[str]) -> set[str]:
    out: set[str] = set()
    for mod in enabled:
        so = modules_dir / mod / "SceneObj"
        if so.is_dir():
            out |= {c.name.lower() for c in so.iterdir() if c.is_dir()}
    return out


def remap_text(text: str, remap: dict[str, str]) -> tuple[str, dict[str, int]]:
    """Replace scene_name / scene_name_N attribute values per `remap`; returns (text, counts)."""
    counts: dict[str, int] = {}
    for old, new in remap.items():
        pat = re.compile(r'(scene_name(?:_\d)?=")' + re.escape(old) + '"')
        text, n = pat.subn(lambda m: m.group(1) + new + '"', text)
        if n:
            counts[old] = n
    return text, counts


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--backup", action="store_true")
    ap.add_argument("--module", default=DEFAULT_MODULE)
    ap.add_argument("--enabled", nargs="+", default=list(DEFAULT_ENABLED))
    args = ap.parse_args(argv)
    if not (args.dry_run or args.apply):
        ap.error("pass --dry-run or --apply")

    folders = scene_folders_lower(MODULES, [*args.enabled, args.module])
    bad = [new for new in REMAP.values() if new.lower() not in folders]
    if bad:
        print("ABORT — replacement scene(s) not found in any enabled module's SceneObj:")
        for b in bad:
            print(f"  {b}")
        return 1
    print(f"All {len(set(REMAP.values()))} replacement scenes verified present on disk.\n")

    path = MODULES / args.module / "ModuleData" / "settlements.xml"
    if not path.is_file():
        raise SystemExit(f"missing: {path}")
    raw = path.read_bytes()
    had_bom = raw.startswith(b"\xef\xbb\xbf")
    text, counts = remap_text(raw.decode("utf-8-sig"), REMAP)
    print(f"== {path} ==")
    for old, n in counts.items():
        print(f"  {old} -> {REMAP[old]}  ({n})")
    total = sum(counts.values())
    print(f"  total replacements: {total}")
    if args.apply and total:
        if args.backup:
            path.with_suffix(".xml.bak_scenes").write_bytes(raw)
        tmp = path.with_suffix(".xml.tmp")
        tmp.write_bytes((b"\xef\xbb\xbf" if had_bom else b"") + text.encode("utf-8"))
        os.replace(tmp, path)
        print("  [APPLIED]")
    elif args.dry_run:
        print("  [DRY RUN]")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
