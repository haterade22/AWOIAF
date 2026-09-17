#!/usr/bin/env python3
"""Consolidate <module>/AssetSources/MapAssets down to exactly two folders: icons/ and textures/.

Why: the v1.5.3 Scene Editor crashes whenever it has to create a folder while mirroring
AssetSources/ into Assets/. The ADOD map sources came as 60 nested folders (13 deep). End state: the
editor only ever needs to create `icons` and `textures`.

Rules (applied to every file under MapAssets, recursively):
  .fbx                              -> icons/<same filename>            (case kept)
  .png .dds .tga .jpg .jpeg .tif .tiff .bmp -> textures/<filename lowercased>
  desktop.ini                       -> deleted (Google-Drive folder-icon junk)
  .zip .7z .rar                     -> moved up to AssetSources/ (out of MapAssets, no new folder)
  anything else                     -> HARD ERROR (decide by hand; never silently moved or dropped)
Name collisions (case-insensitive, after the rename):
  byte-identical copies             -> one survives (the one already at the target, else the shallowest)
  different content                 -> the in-place / shallowest keeps the name, the others get
                                       _alt, _alt2, ... before the extension; every such case is printed
Then every now-empty folder under MapAssets other than icons/ and textures/ is removed.

Contract: the whole plan is built and validated (unique targets, nothing outside the two folders)
before the first write; dry-run by default; idempotent. Moves are same-volume os.replace onto paths
verified not to exist, so nothing is ever overwritten.

Usage:
    python tools/awoiaf_map/flatten_map_asset_sources.py            # DRY-RUN: full plan
    python tools/awoiaf_map/flatten_map_asset_sources.py --apply
    ... [--module-dir DIR]
"""
from __future__ import annotations

import argparse
import hashlib
import os
from dataclasses import dataclass, field
from pathlib import Path

GAME = Path(os.environ.get("BANNERLORD_GAME_DIR",
                           r"E:\Steam\steamapps\common\Mount & Blade II Bannerlord"))
DEFAULT_MODULE_DIR = GAME / "Modules" / "AWOIAF_Map"

MESH_EXTS = {".fbx"}
TEXTURE_EXTS = {".png", ".dds", ".tga", ".jpg", ".jpeg", ".tif", ".tiff", ".bmp"}
ARCHIVE_EXTS = {".zip", ".7z", ".rar"}
JUNK_NAMES = {"desktop.ini"}
KEEP_DIRS = ("icons", "textures")


@dataclass
class Plan:
    moves: list[tuple[Path, str]] = field(default_factory=list)       # (source, target rel to MapAssets)
    deletes: list[Path] = field(default_factory=list)                  # junk + identical duplicates
    archives_out: list[Path] = field(default_factory=list)             # moved to AssetSources/
    conflicts: list[tuple[Path, str]] = field(default_factory=list)    # (source, suffixed target)
    unknown: list[Path] = field(default_factory=list)


def _target_for(p: Path) -> str | None:
    ext = p.suffix.lower()
    if ext in MESH_EXTS:
        return f"icons/{p.name}"
    if ext in TEXTURE_EXTS:
        return f"textures/{p.name.lower()}"
    return None


def _digest(p: Path) -> str:
    h = hashlib.sha1()
    with p.open("rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def _depth(root: Path, p: Path) -> int:
    return len(p.relative_to(root).parts)


def _with_suffix(target: str, n: int) -> str:
    stem, dot, ext = target.rpartition(".")
    tag = "_alt" if n == 1 else f"_alt{n}"
    return f"{stem}{tag}{dot}{ext}" if dot else f"{target}{tag}"


def build_plan(map_assets: Path) -> Plan:
    plan = Plan()
    groups: dict[str, list[Path]] = {}
    for p in sorted(map_assets.rglob("*")):
        if not p.is_file():
            continue
        if p.name.lower() in JUNK_NAMES:
            plan.deletes.append(p)
            continue
        if p.suffix.lower() in ARCHIVE_EXTS:
            plan.archives_out.append(p)
            continue
        target = _target_for(p)
        if target is None:
            plan.unknown.append(p)
            continue
        groups.setdefault(target.lower(), []).append(p)
    if plan.unknown:
        listing = "\n  ".join(str(p.relative_to(map_assets)) for p in plan.unknown)
        raise SystemExit(f"unhandled file types under {map_assets} — decide by hand:\n  {listing}")

    claimed: set[str] = set()
    for key, sources in groups.items():
        # canonical: already at its target path, else the shallowest (ties: sorted order)
        in_place = [s for s in sources if str(s.relative_to(map_assets)).replace("\\", "/").lower() == key]
        canonical = in_place[0] if in_place else min(sources, key=lambda s: (_depth(map_assets, s), str(s)))
        target = _target_for(canonical)
        assert target is not None
        digests = {s: _digest(s) for s in sources} if len(sources) > 1 else {}
        n = 0
        for s in sources:
            if s is canonical:
                if str(s.relative_to(map_assets)).replace("\\", "/") != target:
                    plan.moves.append((s, target))
                claimed.add(target.lower())
                continue
            if digests[s] == digests[canonical]:
                plan.deletes.append(s)
                continue
            n += 1
            alt = _with_suffix(target, n)
            while alt.lower() in claimed or alt.lower() in groups:
                n += 1
                alt = _with_suffix(target, n)
            claimed.add(alt.lower())
            plan.moves.append((s, alt))
            plan.conflicts.append((s, alt))

    # validation: unique targets, all inside the two folders, no target already occupied by a non-source
    targets = [t for _s, t in plan.moves]
    if len({t.lower() for t in targets}) != len(targets):
        raise SystemExit("plan has duplicate targets — refusing")
    for t in targets:
        if t.split("/", 1)[0] not in KEEP_DIRS or "/" in t.split("/", 1)[1]:
            raise SystemExit(f"plan target escapes icons/ or textures/: {t}")
    sources = {s.resolve() for s, _t in plan.moves}
    for _s, t in plan.moves:
        existing = map_assets / t
        if existing.exists() and existing.resolve() not in sources:
            # case-only rename of the same file is fine; a different existing file is not
            if not any(s.resolve() == existing.resolve() for s, tt in plan.moves if tt == t):
                raise SystemExit(f"target already occupied by an unplanned file: {t}")
    return plan


def apply_plan(map_assets: Path, plan: Plan) -> None:
    asset_sources = map_assets.parent
    # 1. moves (same volume); a case-only rename goes through a temp name so NTFS accepts it
    for src, target in plan.moves:
        dst = map_assets / target
        dst.parent.mkdir(exist_ok=True)
        if dst.exists() and dst.resolve() == src.resolve():
            tmp = dst.with_name(dst.name + ".casetmp")
            os.replace(src, tmp)
            os.replace(tmp, dst)
        else:
            if dst.exists():
                raise SystemExit(f"refusing to overwrite {dst}")
            os.replace(src, dst)
    # 2. archives out of MapAssets
    for a in plan.archives_out:
        dst = asset_sources / a.name
        if dst.exists():
            raise SystemExit(f"refusing to overwrite {dst}")
        os.replace(a, dst)
    # 3. junk + identical duplicates
    for d in plan.deletes:
        d.unlink()
    # 4. remove empty folders bottom-up (never the two keepers)
    for d in sorted((p for p in map_assets.rglob("*") if p.is_dir()), key=lambda p: -len(p.parts)):
        if d.parent == map_assets and d.name in KEEP_DIRS:
            continue
        leftovers = list(d.iterdir())
        if leftovers:
            raise SystemExit(f"folder not empty after consolidation: {d} ({leftovers[:3]})")
        d.rmdir()


def check_end_state(map_assets: Path) -> None:
    entries = sorted(p.name for p in map_assets.iterdir())
    if entries != sorted(KEEP_DIRS) or not all((map_assets / k).is_dir() for k in KEEP_DIRS):
        raise SystemExit(f"post-check: MapAssets should contain exactly {KEEP_DIRS}, has {entries}")
    for p in (map_assets / "icons").iterdir():
        if not p.is_file() or p.suffix.lower() not in MESH_EXTS:
            raise SystemExit(f"post-check: non-fbx in icons/: {p.name}")
    for p in (map_assets / "textures").iterdir():
        if not p.is_file() or p.suffix.lower() not in TEXTURE_EXTS or p.name != p.name.lower():
            raise SystemExit(f"post-check: bad entry in textures/: {p.name}")


def main(argv: list[str] | None = None) -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--module-dir", type=Path, default=DEFAULT_MODULE_DIR)
    ap.add_argument("--apply", action="store_true", help="perform the moves (default: dry-run)")
    args = ap.parse_args(argv)
    map_assets = args.module_dir / "AssetSources" / "MapAssets"
    if not map_assets.is_dir():
        raise SystemExit(f"missing: {map_assets}")

    plan = build_plan(map_assets)
    mode = "APPLY" if args.apply else "DRY-RUN"
    total = sum(1 for p in map_assets.rglob("*") if p.is_file())
    print(f"[{mode}] {map_assets}: {total} files, "
          f"{sum(1 for p in map_assets.rglob('*') if p.is_dir())} folders")
    print(f"[{mode}] moves: {len(plan.moves)}  deletes (junk + identical dups): {len(plan.deletes)}  "
          f"archives out: {len(plan.archives_out)}  name conflicts kept as _alt: {len(plan.conflicts)}")
    for s, t in plan.conflicts:
        print(f"[{mode}]   CONFLICT {s.relative_to(map_assets)}  ->  {t}   (different content, both kept)")
    for a in plan.archives_out:
        print(f"[{mode}]   archive  {a.relative_to(map_assets)}  ->  AssetSources/{a.name}")
    dups = [d for d in plan.deletes if d.name.lower() not in JUNK_NAMES]
    for d in dups:
        print(f"[{mode}]   dup      {d.relative_to(map_assets)}  (identical copy elsewhere, removed)")
    if not plan.moves and not plan.deletes and not plan.archives_out:
        print(f"[{mode}] nothing to do")
        check_end_state(map_assets)
        return
    if not args.apply:
        print("[DRY-RUN] nothing written. Re-run with --apply.")
        return
    apply_plan(map_assets, plan)
    check_end_state(map_assets)
    print(f"[APPLY] done — icons: {sum(1 for _ in (map_assets / 'icons').iterdir())} fbx, "
          f"textures: {sum(1 for _ in (map_assets / 'textures').iterdir())} files")


if __name__ == "__main__":
    main()
