#!/usr/bin/env python3
"""Lowercase the terrain-layer texture names in the AWOIAF_Map scene so they match the module's textures.

Symptom (Scene Editor, v1.5.3): "Terrain layer (swamp) has invalid texture named (Water_v08_Height) in
(heightmap) slot" x23 — the layers render blank. `scene.xscene` <terrain> layers reference textures by
name in ADOD's original mixed case (`Grass003_4K-PNG_Color`), while the textures were re-imported with
lowercase filenames, and terrain-layer lookup is case-sensitive (asset overrides are not — that is why
the same names resolved elsewhere).

Rule: inside the <terrain>...</terrain> block only, for every <texture ... name="X"/> where X != X.lower()
and X.lower() is a texture in <module>/Assets, rewrite the name to X.lower(). Names that are already
lowercase, vanilla names, "" and "none" are untouched. Same byte length, so nothing else moves; the file's
BOM/line endings are preserved. The binary terrain files carry no texture names (checked 2026-09-16).

Run with the editor CLOSED: it holds the scene in memory and would write the old names back on save.

Usage:
    python tools/awoiaf_map/fix_terrain_texture_case.py            # DRY-RUN
    python tools/awoiaf_map/fix_terrain_texture_case.py --apply
    ... [--module-dir DIR]
"""
from __future__ import annotations

import argparse
import os
import re
from pathlib import Path

GAME = Path(os.environ.get("BANNERLORD_GAME_DIR",
                           r"E:\Steam\steamapps\common\Mount & Blade II Bannerlord"))
DEFAULT_MODULE_DIR = GAME / "Modules" / "AWOIAF_Map"

_TEX_RE = re.compile(r'(<texture type="[a-z]+" name=")([^"]*)(")')


def module_textures(assets: Path) -> set[str]:
    return {p.name[:-len("_tex.tpac")] for p in assets.rglob("*_tex.tpac")}


def fix_scene_text(scene: str, textures: set[str]) -> tuple[str, list[tuple[str, str]]]:
    i0 = scene.find("<terrain ")
    i1 = scene.find("</terrain>", i0)
    if i0 < 0 or i1 < 0:
        raise SystemExit("no <terrain> block in scene")
    changes: list[tuple[str, str]] = []

    def sub(m: re.Match) -> str:
        name = m.group(2)
        low = name.lower()
        if name != low and low in textures:
            changes.append((name, low))
            return m.group(1) + low + m.group(3)
        return m.group(0)

    block = _TEX_RE.sub(sub, scene[i0:i1])
    out = scene[:i0] + block + scene[i1:]
    if len(out) != len(scene):
        raise SystemExit("post-check: scene length changed")
    return out, changes


def main(argv: list[str] | None = None) -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--module-dir", type=Path, default=DEFAULT_MODULE_DIR)
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args(argv)
    scene_path = args.module_dir / "SceneObj" / "Main_map" / "scene.xscene"
    if not scene_path.is_file():
        raise SystemExit(f"missing: {scene_path}")
    raw = scene_path.read_bytes()
    had_bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw.decode("utf-8-sig")
    out, changes = fix_scene_text(text, module_textures(args.module_dir / "Assets"))
    mode = "APPLY" if args.apply else "DRY-RUN"
    print(f"[{mode}] {scene_path}: {len(changes)} terrain texture refs to lowercase")
    for old, new in changes:
        print(f"[{mode}]   {old} -> {new}")
    if not changes:
        print(f"[{mode}] nothing to do")
        return
    if not args.apply:
        print("[DRY-RUN] nothing written. Close the editor, then re-run with --apply.")
        return
    tmp = scene_path.with_suffix(".xscene.tmp")
    tmp.write_bytes((b"\xef\xbb\xbf" if had_bom else b"") + out.encode("utf-8"))
    os.replace(tmp, scene_path)
    print(f"[APPLY] wrote {scene_path}")


if __name__ == "__main__":
    main()
