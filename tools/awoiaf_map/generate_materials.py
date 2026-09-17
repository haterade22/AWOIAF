#!/usr/bin/env python3
"""Recreate ADOD's map materials inside the AWOIAF_Map module, bound to OUR re-imported textures.

Why not copy ADOD's `*_mtl.tpac` files: a material references its textures by asset GUID, and the
textures were re-imported into this module by the v1.5.3 editor, so every one of them has a new GUID.
A copied material would point at GUIDs that exist in no loaded module.

What this does, per ADOD material (`<adod Assets>/**/*_mtl.tpac`, 115 files):
  1. resolve each texture-slot GUID -> ADOD texture NAME (from ADOD's `*_tex.tpac` index)
  2. look the name up, lowercased, in OUR `*_tex.tpac` index -> our GUID   (missing = hard error)
  3. write `<module>/Assets/MapAssets/textures/<name>_mtl.tpac` = the ADOD material with fresh
     package + asset GUIDs, texture GUIDs swapped, and the checknum recomputed (xxHash64 over
     `u64 metadataSize` + metadata — see tpac.py). Name, shader, blend mode, flags, shader-material
     flags and every float are preserved byte-for-byte, so the result carries exactly ADOD's settings.
Texture GUIDs that are NOT ADOD map textures (vanilla / other-module textures) are kept as-is and
reported; with external resolution on (default) each is looked up in the enabled modules' packed
tpac indices so the report says where it lives — or that it is unresolved.

Existing materials in the output folder are kept (the user may have authored them by hand).
Dry-run by default; the whole plan is validated before the first write; idempotent.

Usage:
    python tools/awoiaf_map/generate_materials.py            # DRY-RUN + report
    python tools/awoiaf_map/generate_materials.py --apply
    ... [--adod-assets DIR] [--module-dir DIR] [--out DIR] [--game-dir DIR] [--no-resolve-external]
"""
from __future__ import annotations

import argparse
import os
import uuid
from pathlib import Path

import tpac

GAME = Path(os.environ.get("BANNERLORD_GAME_DIR",
                           r"E:\Steam\steamapps\common\Mount & Blade II Bannerlord"))
DEFAULT_ADOD_ASSETS = Path(r"E:\LOTRAOMAssets\A Dance of Dragons - Map\Assets")
DEFAULT_MODULE_DIR = GAME / "Modules" / "AWOIAF_Map"
ENABLED_MODULES = ("Native", "SandBox", "SandBoxCore", "CustomBattle", "StoryMode")

# Materials deliberately NOT generated. `river` (shader water_simulation) crashed the v1.5.3 editor
# twice on 2026-09-16 at "compile_shader: water_simulation.rs, main_cs" (~4 min after the resource
# browser opened); no mesh or scene entity references it (the map's water is a terrain layer).
# Deleted from the module on the user's call (2026-09-16); ADOD's original remains in the source folder.
SKIP_MATERIALS = {"river"}
# Materials generated under a different name because ADOD's name collides with a vanilla asset and
# would override it module-wide (2026-09-16: `grass` overrode Native/materials.tpac:grass and the
# editor logged "Unable to find DiffuseMap of material grass"). Meshes bind by GUID, so the name is free.
RENAMED_MATERIALS = {"grass": "adod_grass"}


def index_textures_by_guid(assets_root: Path) -> dict[bytes, str]:
    out: dict[bytes, str] = {}
    for p in assets_root.rglob("*_tex.tpac"):
        a = tpac.read_single_asset(p.read_bytes())
        if a.type_guid == tpac.TEXTURE_TYPE:
            out[a.asset_guid] = a.name
    return out


def index_textures_by_name(assets_root: Path) -> dict[str, bytes]:
    out: dict[str, bytes] = {}
    where: dict[str, Path] = {}
    for p in sorted(assets_root.rglob("*_tex.tpac")):
        a = tpac.read_single_asset(p.read_bytes())
        if a.type_guid != tpac.TEXTURE_TYPE:
            continue
        key = a.name.lower()
        if key in out and out[key] != a.asset_guid:
            raise SystemExit(f"two textures named \"{key}\" in {assets_root}: {where[key]} and {p}")
        out[key] = a.asset_guid
        where[key] = p
    return out


def resolve_external(guids: set[bytes], game_dir: Path, module_dir: Path) -> dict[bytes, str]:
    """GUID -> 'Module/file.tpac: asset_name' for GUIDs found in enabled modules' tpac indices."""
    found: dict[bytes, str] = {}
    roots = [game_dir / "Modules" / m for m in ENABLED_MODULES] + [module_dir]
    for root in roots:
        for p in list(root.glob("AssetPackages/*.tpac")) + list(root.rglob("Assets/**/*.tpac")):
            try:
                with p.open("rb") as f:
                    head = f.read(0x24)
                    if len(head) < 0x24 or head[:4] != b"TPAC":
                        continue
                    f.seek(0)
                    data = f.read(tpac.index_size(head))
                for _t, g, name in tpac.iter_asset_index(data):
                    if g in guids and g not in found:
                        found[g] = f"{root.name}/{p.name}: {name}"
            except Exception as e:  # noqa: BLE001 — a corrupt/foreign tpac must not abort the report
                print(f"  (skipped {p}: {e})")
    return found


def main(argv: list[str] | None = None) -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--adod-assets", type=Path, default=DEFAULT_ADOD_ASSETS)
    ap.add_argument("--module-dir", type=Path, default=DEFAULT_MODULE_DIR)
    ap.add_argument("--out", type=Path, default=None, help="default: <module>/Assets/MapAssets/textures")
    ap.add_argument("--game-dir", type=Path, default=GAME)
    ap.add_argument("--no-resolve-external", action="store_true")
    ap.add_argument("--apply", action="store_true", help="write materials (default: dry-run)")
    args = ap.parse_args(argv)
    out_dir = args.out or (args.module_dir / "Assets" / "MapAssets" / "textures")
    if not args.adod_assets.is_dir():
        raise SystemExit(f"missing ADOD assets: {args.adod_assets}")
    if not (args.module_dir / "Assets").is_dir():
        raise SystemExit(f"missing module Assets: {args.module_dir / 'Assets'}")

    adod_by_guid = index_textures_by_guid(args.adod_assets)
    ours_by_name = index_textures_by_name(args.module_dir / "Assets")
    mode = "APPLY" if args.apply else "DRY-RUN"
    print(f"[{mode}] ADOD textures: {len(adod_by_guid)}   our textures: {len(ours_by_name)}   out: {out_dir}")

    plan: list[tuple[Path, bytes, str]] = []
    kept: list[str] = []
    missing: list[tuple[str, str]] = []
    external: dict[bytes, list[str]] = {}
    table: list[str] = []
    for src in sorted(args.adod_assets.rglob("*_mtl.tpac")):
        data = src.read_bytes()
        a = tpac.read_single_asset(data)
        if a.type_guid != tpac.MATERIAL_TYPE or a.name in SKIP_MATERIALS:
            continue
        guid_map: dict[bytes, bytes] = {}
        slot_names = []
        for slot, g, _off in tpac.material_texture_slots(a.meta):
            if g in adod_by_guid:
                name = adod_by_guid[g]
                ours = ours_by_name.get(name.lower())
                if ours is None:
                    missing.append((a.name, name))
                else:
                    guid_map[g] = ours
                slot_names.append(f"{slot}:{name.lower()}")
            else:
                external.setdefault(g, []).append(a.name)
                slot_names.append(f"{slot}:<ext {uuid.UUID(bytes_le=g).hex[:8]}>")
        s = tpac.material_summary(a.meta)
        table.append(f"  {a.name:40s} {s['blend']:14s} {'+'.join(s['flags']) or '-':26s} "
                     f"{'+'.join(s['shader_material_flags']):60s} {' '.join(slot_names)}")
        out_name = RENAMED_MATERIALS.get(a.name, a.name)
        target = out_dir / f"{out_name}_mtl.tpac"
        if target.exists():
            kept.append(out_name)
            continue
        new_bytes, _unmapped = tpac.retarget_material(data, guid_map)
        if out_name != a.name:
            import rename_tpac_assets
            new_bytes = rename_tpac_assets.rename_assets(new_bytes, {a.name: out_name})
        plan.append((target, new_bytes, out_name))

    print(f"[{mode}] materials: {len(table)}   materials to write: {len(plan)}   already present (kept): {len(kept)}")
    print(f"[{mode}] material table (name / blend / flags / shader-material flags / slot:texture):")
    for row in table:
        print(row)
    print(f"[{mode}] external texture refs: {len(external)} guid(s) used by "
          f"{sorted({m for ms in external.values() for m in ms})}")
    if external and not args.no_resolve_external:
        found = resolve_external(set(external), args.game_dir, args.module_dir)
        for g, mats in sorted(external.items(), key=lambda kv: kv[1]):
            print(f"    {uuid.UUID(bytes_le=g)}  <- {', '.join(sorted(set(mats)))}  ->  "
                  f"{found.get(g, 'UNRESOLVED in enabled modules')}")
    if missing:
        for mat, tex in missing:
            print(f"[{mode}]   MISSING our texture \"{tex.lower()}\" needed by material \"{mat}\"")
        raise SystemExit(f"refusing: {len(missing)} texture reference(s) have no re-imported texture")
    if not plan:
        print(f"[{mode}] nothing to write")
        return
    if not args.apply:
        print("[DRY-RUN] nothing written. Re-run with --apply.")
        return
    out_dir.mkdir(parents=True, exist_ok=True)
    for target, new_bytes, name in plan:
        tmp = target.with_suffix(".tpac.tmp")
        tmp.write_bytes(new_bytes)
        os.replace(tmp, target)
        back = tpac.read_single_asset(target.read_bytes())
        if back.name != name or back.checknum != tpac.checknum(back.meta):
            raise SystemExit(f"post-check failed on {target}")
    print(f"[APPLY] wrote {len(plan)} materials to {out_dir}")


if __name__ == "__main__":
    main()
