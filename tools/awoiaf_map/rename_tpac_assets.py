#!/usr/bin/env python3
"""Rename assets inside the AWOIAF_Map module's editor-form tpacs (name field only, GUIDs kept).

Why: ADOD shipped two assets whose names collide with vanilla ones and override them module-wide —
the stray Blender default-cube metamesh `cube` inside `riverrun_geo.tpac` (overrides
`Native/meshes_shared_2.tpac: cube`; Main_map never places it) and the material `grass` (overrides
`Native/materials.tpac: grass`, which makes the editor resolve its textures before ours register:
"Unable to find DiffuseMap of material grass"). Meshes reference materials by GUID and the scene
references metameshes by name, so renaming these two touches nothing else.

Mechanics: the name lives outside the checknum'd metadata, so only the index size changes — the
package is re-emitted with segment offsets shifted by the delta and data copied verbatim
(port_geo_meshes.serialize_package). For a single-asset `<name>_mtl.tpac` the file is renamed too.

Usage:
    python tools/awoiaf_map/rename_tpac_assets.py --rename grass=adod_grass --rename cube=adod_cube
    python tools/awoiaf_map/rename_tpac_assets.py ... --apply           (default: dry-run)
"""
from __future__ import annotations

import argparse
import os
from pathlib import Path

import port_geo_meshes as pg
import tpac

GAME = Path(os.environ.get("BANNERLORD_GAME_DIR",
                           r"E:\Steam\steamapps\common\Mount & Blade II Bannerlord"))
DEFAULT_ASSETS = GAME / "Modules" / "AWOIAF_Map" / "Assets"


def rename_assets(data: bytes, renames: dict[str, str]) -> bytes:
    pkg = pg.parse_package(data)
    hit = False
    for a in pkg.assets:
        if a.name in renames:
            a.name = renames[a.name]
            hit = True
    if not hit:
        raise SystemExit(f"none of {sorted(renames)} found in package")
    return pg.serialize_package(pkg.package_guid, pkg.assets, data, pkg.index_len)


def main(argv: list[str] | None = None) -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--assets", type=Path, default=DEFAULT_ASSETS)
    ap.add_argument("--rename", action="append", required=True, metavar="OLD=NEW")
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args(argv)
    renames = dict(r.split("=", 1) for r in args.rename)
    mode = "APPLY" if args.apply else "DRY-RUN"

    plan: list[tuple[Path, Path, bytes, list[str]]] = []
    for p in sorted(args.assets.rglob("*.tpac")):
        data = p.read_bytes()
        try:
            names = [n for _t, _g, n in tpac.iter_asset_index(data)]
        except Exception:  # noqa: BLE001 — not a tpac we understand; leave it alone
            continue
        hits = [n for n in names if n in renames]
        if not hits:
            continue
        out = rename_assets(data, renames)
        target = p
        if len(names) == 1 and p.name.startswith(names[0] + "_"):
            target = p.with_name(renames[names[0]] + p.name[len(names[0]):])
        plan.append((p, target, out, hits))
    print(f"[{mode}] renames: {renames}   files affected: {len(plan)}")
    for p, target, _o, hits in plan:
        print(f"[{mode}]   {p.name} -> {target.name}   assets: {', '.join(f'{h}->{renames[h]}' for h in hits)}")
    if not plan:
        print(f"[{mode}] nothing to do")
        return
    if not args.apply:
        print("[DRY-RUN] nothing written. Re-run with --apply.")
        return
    for p, target, out, _h in plan:
        if target != p and target.exists():
            raise SystemExit(f"refusing to overwrite {target}")
        tmp = p.with_suffix(".tpac.tmp")
        tmp.write_bytes(out)
        os.replace(tmp, target)
        if target != p:
            p.unlink()
        back = pg.parse_package(target.read_bytes())
        for a in back.assets:
            if a.checknum != tpac.checknum(a.meta):
                raise SystemExit(f"post-check failed on {target}")
    print(f"[APPLY] rewrote {len(plan)} files")


if __name__ == "__main__":
    main()
