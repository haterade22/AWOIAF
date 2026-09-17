#!/usr/bin/env python3
"""Retarget the asset-source paths baked into the AWOIAF_Map module's editor-form *_tex.tpac files.

Symptom (Scene Editor, v1.5.3):
    RGL WARNING: Unable to locate source file $BASE/Modules/A Dance of Dragons/AssetSources/MapAssets/
    icons/riverrun/the_twins_bridge_bridge_d.png of texture the_twins_bridge_bridge_d to compile

Why: each editor-form texture tpac (Assets/**/*_tex.tpac, ~538 bytes) holds only metadata — asset
name, an Int32-length-prefixed source path and an 8-byte source hash — not the pixels. Those were in
the 1.2.12 RuntimeDataCache (excluded from the module; invalid on 1.5.3 anyway), so the engine
recompiles from source. The recorded path names the module the asset was ORIGINALLY compiled in
("A Dance of Dragons", ADOD's main module — 499 tpacs; "ADOD_IAF Map" — 1), not this module.

How: rewrite the module segment IN PLACE with an equal-length replacement, padding with "./" no-op
path components ("A Dance of Dragons/" -> "AWOIAF_Map/././././"). The Int32 length prefix and every
byte outside the string are untouched, so the file structure cannot break. $BASE/ is substituted as a
raw string by the engine (TaleWorlds.Engine.Utilities: `.Replace("$BASE/", GetBasePath())`), so "./"
resolves through ordinary Win32 path normalisation.

Guards: refuses a module segment that cannot be padded to equal length; refuses (before ANY write) a
rewritten path that does not resolve to an existing file under <module>/AssetSources; idempotent;
dry-run by default. Revert = re-copy the original tpacs from the ADOD source folder.

Usage:
    python tools/awoiaf_map/retarget_tpac_sources.py            # DRY-RUN: plan + verification
    python tools/awoiaf_map/retarget_tpac_sources.py --apply
    ... [--module-dir DIR] [--game-dir DIR] [--module-name NAME]
"""
from __future__ import annotations

import argparse
import os
import struct
from pathlib import Path

GAME = Path(os.environ.get("BANNERLORD_GAME_DIR",
                           r"E:\Steam\steamapps\common\Mount & Blade II Bannerlord"))
MODULE_FOLDER = "AWOIAF_Map"
DEFAULT_MODULE_DIR = GAME / "Modules" / MODULE_FOLDER

MARKER = b"$BASE/Modules/"


def equal_length_segment(original: str, target: str) -> str:
    """Return `target` padded with '/.' components to the byte length of `original`."""
    diff = len(original.encode("utf-8")) - len(target.encode("utf-8"))
    if diff < 0:
        raise SystemExit(f"cannot retarget \"{original}\" -> \"{target}\": target is longer")
    if diff % 2:
        raise SystemExit(f"cannot retarget \"{original}\" -> \"{target}\": odd length difference {diff} "
                         f"cannot be padded with \"./\" components — handle by hand")
    return target + "/." * (diff // 2)


def patch_bytes(data: bytes, module_name: str) -> tuple[bytes, list[tuple[str, str]]]:
    """Rewrite every '$BASE/Modules/<module>/...' string in place. Returns (bytes, [(old, new), ...])."""
    out = bytearray(data)
    changes: list[tuple[str, str]] = []
    pos = 0
    while True:
        i = data.find(MARKER, pos)
        if i < 0:
            break
        if i < 4:
            raise SystemExit("marker found without room for a length prefix")
        (length,) = struct.unpack_from("<i", data, i - 4)
        end = i + length
        if length <= len(MARKER) or end > len(data):
            raise SystemExit(f"length prefix {length} at offset {i - 4} does not frame a path string")
        try:
            path = data[i:end].decode("utf-8")
        except UnicodeDecodeError:
            raise SystemExit(f"path at offset {i} is not UTF-8 — length prefix {length} looks wrong")
        if not all(32 <= ord(c) < 127 for c in path):
            raise SystemExit(f"path at offset {i} contains non-printable bytes — length prefix {length} looks wrong")
        rest = path[len(MARKER):]
        if "/" not in rest:
            raise SystemExit(f"path has no module segment: {path}")
        segment, tail = rest.split("/", 1)
        if segment != module_name:
            new_path = MARKER.decode() + equal_length_segment(segment, module_name) + "/" + tail
            new_bytes = new_path.encode("utf-8")
            assert len(new_bytes) == length
            out[i:end] = new_bytes
            changes.append((path, new_path))
        pos = end
    return bytes(out), changes


def resolve(base_path: Path, engine_path: str) -> Path:
    return Path(os.path.normpath(str(base_path / engine_path[len("$BASE/"):])))


def main(argv: list[str] | None = None) -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--module-dir", type=Path, default=DEFAULT_MODULE_DIR)
    ap.add_argument("--game-dir", type=Path, default=GAME, help="what $BASE/ resolves to")
    ap.add_argument("--module-name", default=MODULE_FOLDER)
    ap.add_argument("--apply", action="store_true", help="write patched tpacs (default: dry-run)")
    args = ap.parse_args(argv)
    assets = args.module_dir / "Assets"
    if not assets.is_dir():
        raise SystemExit(f"no Assets folder under {args.module_dir}")

    plan: list[tuple[Path, bytes, list[tuple[str, str]]]] = []
    unresolved: list[tuple[Path, str]] = []
    scanned = 0
    for tpac in sorted(assets.rglob("*.tpac")):
        scanned += 1
        data = tpac.read_bytes()
        if MARKER not in data:
            continue
        patched, changes = patch_bytes(data, args.module_name)
        if not changes:
            continue
        for _old, new in changes:
            target = resolve(args.game_dir, new)
            if not target.is_file():
                unresolved.append((tpac, new))
        plan.append((tpac, patched, changes))

    mode = "APPLY" if args.apply else "DRY-RUN"
    print(f"[{mode}] scanned {scanned} tpacs under {assets}")
    print(f"[{mode}] tpacs to patch: {len(plan)}")
    seen: dict[str, int] = {}
    for _t, _b, changes in plan:
        for old, _new in changes:
            seg = old[len(MARKER):].split("/", 1)[0]
            seen[seg] = seen.get(seg, 0) + 1
    for seg, n in sorted(seen.items(), key=lambda kv: -kv[1]):
        print(f"[{mode}]   {n:4d} x  {seg}/  ->  {equal_length_segment(seg, args.module_name)}/")
    if plan:
        t, _b, ch = plan[0]
        print(f"[{mode}] example {t.name}:\n           {ch[0][0]}\n        -> {ch[0][1]}")
    if unresolved:
        print(f"[{mode}] {len(unresolved)} rewritten paths do NOT resolve to a file, e.g.:")
        for t, new in unresolved[:10]:
            print(f"           {t.name}: {new}")
        raise SystemExit("refusing: every rewritten source path must resolve under the module's AssetSources")
    print(f"[{mode}] all rewritten paths resolve to existing source files")
    if not args.apply:
        print("[DRY-RUN] nothing written. Re-run with --apply.")
        return
    for tpac, patched, _changes in plan:
        tmp = tpac.with_suffix(tpac.suffix + ".tmp")
        tmp.write_bytes(patched)
        os.replace(tmp, tpac)
    print(f"[APPLY] patched {len(plan)} tpacs in place (sizes unchanged)")


if __name__ == "__main__":
    main()
