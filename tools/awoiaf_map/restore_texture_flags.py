#!/usr/bin/env python3
"""Restore the texture flags the editor re-import dropped, taking ADOD's original `*_tex.tpac` as the
reference (matched by lowercase name).

Symptom (Scene Editor, v1.5.3): "Scene has terrain texture grass003_4k-png_color without 'for_terrain'
flag at one of its layers" — terrain layers stay blank. Diff of all 467 shared textures (2026-09-16):
`for_terrain` missing on 127, `dont_degrade` on adod_iaf_vistamap, `for_skybox_sun` on 008_d; no other
flag or system-flag differences. Formats differ on two textures (compile-time; not touched here).

Texture metadata (TpacTool Texture.cs): u32 version, guid billboard material, u32, str source, u64, bool,
u32, **str[] flags**, ... — the flag list is re-emitted with the missing names appended; everything
after it is copied verbatim, the checknum is recomputed and the data segments shifted
(port_geo_meshes.serialize_package). Changing flags makes the editor recompile those textures once.

Run with the editor CLOSED. Dry-run by default; idempotent.

Usage:
    python tools/awoiaf_map/restore_texture_flags.py            # DRY-RUN
    python tools/awoiaf_map/restore_texture_flags.py --apply
    ... [--adod-assets DIR] [--module-dir DIR]
"""
from __future__ import annotations

import argparse
import os
import struct
from pathlib import Path

import port_geo_meshes as pg
import tpac

GAME = Path(os.environ.get("BANNERLORD_GAME_DIR",
                           r"E:\Steam\steamapps\common\Mount & Blade II Bannerlord"))
DEFAULT_ADOD_ASSETS = Path(r"E:\LOTRAOMAssets\A Dance of Dragons - Map\Assets")
DEFAULT_MODULE_DIR = GAME / "Modules" / "AWOIAF_Map"


def _rs(b: bytes, q: int) -> tuple[str, int]:
    n = struct.unpack_from("<i", b, q)[0]
    q += 4
    return b[q:q + n].decode("utf-8"), q + n


def _sized(s: str) -> bytes:
    e = s.encode("utf-8")
    return struct.pack("<i", len(e)) + e


def texture_flags(meta: bytes) -> tuple[list[str], int, int]:
    """(flags, start_offset_of_list, end_offset_of_list) within texture metadata."""
    q = 4 + 16 + 4
    _src, q = _rs(meta, q)
    q += 8 + 1 + 4
    start = q
    n = struct.unpack_from("<I", meta, q)[0]
    q += 4
    flags = []
    for _ in range(n):
        s, q = _rs(meta, q)
        flags.append(s)
    return flags, start, q


def with_flags(meta: bytes, flags: list[str]) -> bytes:
    _old, start, end = texture_flags(meta)
    body = struct.pack("<I", len(flags)) + b"".join(_sized(f) for f in flags)
    return meta[:start] + body + meta[end:]


def restore(data: bytes, reference_flags: list[str]) -> tuple[bytes, list[str]]:
    """Append every reference flag missing from the texture's flag list; returns (bytes, added)."""
    pkg = pg.parse_package(data)
    if len(pkg.assets) != 1 or pkg.assets[0].type_guid != tpac.TEXTURE_TYPE:
        raise ValueError("expected a single-texture tpac")
    a = pkg.assets[0]
    have, _s, _e = texture_flags(a.meta)
    added = [f for f in reference_flags if f not in have]
    if not added:
        return data, []
    a.meta = with_flags(a.meta, have + added)
    out = pg.serialize_package(pkg.package_guid, pkg.assets, data, pkg.index_len)
    back = pg.parse_package(out).assets[0]
    if texture_flags(back.meta)[0] != have + added or back.checknum != tpac.checknum(back.meta):
        raise ValueError(f"{a.name}: post-check failed")
    return out, added


def main(argv: list[str] | None = None) -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--adod-assets", type=Path, default=DEFAULT_ADOD_ASSETS)
    ap.add_argument("--module-dir", type=Path, default=DEFAULT_MODULE_DIR)
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args(argv)
    ref: dict[str, list[str]] = {}
    for p in args.adod_assets.rglob("*_tex.tpac"):
        a = tpac.read_single_asset(p.read_bytes())
        ref[a.name.lower()] = texture_flags(a.meta)[0]
    plan: list[tuple[Path, bytes, list[str]]] = []
    for p in sorted((args.module_dir / "Assets").rglob("*_tex.tpac")):
        data = p.read_bytes()
        a = tpac.read_single_asset(data)
        flags = ref.get(a.name.lower())
        if not flags:
            continue
        out, added = restore(data, flags)
        if added:
            plan.append((p, out, added))
    mode = "APPLY" if args.apply else "DRY-RUN"
    from collections import Counter
    tally = Counter(f for _p, _o, added in plan for f in added)
    print(f"[{mode}] reference textures: {len(ref)}   textures to update: {len(plan)}   flags to add: {dict(tally)}")
    for p, _o, added in plan:
        if len(added) > 1 or added != ["for_terrain"]:
            print(f"[{mode}]   {p.name}: +{added}")
    if not plan:
        print(f"[{mode}] nothing to do")
        return
    if not args.apply:
        print("[DRY-RUN] nothing written. Close the editor, then re-run with --apply.")
        return
    for p, out, _a in plan:
        tmp = p.with_suffix(".tpac.tmp")
        tmp.write_bytes(out)
        os.replace(tmp, p)
    print(f"[APPLY] updated {len(plan)} textures")


if __name__ == "__main__":
    main()
