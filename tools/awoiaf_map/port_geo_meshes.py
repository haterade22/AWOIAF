#!/usr/bin/env python3
"""Port ADOD's compiled map-icon meshes (`*_geo.tpac`) into the AWOIAF_Map module with ADOD's
material assignments intact.

Why: the fbx files carry raw DCC material-slot names (`base.001`, `DW_House_07`, `STANDARD MTL`, ...);
ADOD's artists assigned real materials by hand in the editor, and those assignments live only in the
compiled meshes — 33 geo tpacs, 120 metameshes, 227 sub-meshes, 94 distinct materials (all of which
generate_materials.py already recreated). Re-importing the fbx would throw the assignments away.

A geo tpac = one Geometry asset (the fbx import record: `$BASE/...` source path, xxHash64 of the fbx
bytes, the metameshes it produced, referenced texture names) + N Metamesh assets (sub-meshes with
material GUIDs) + data segments addressed by ABSOLUTE file offset. Layouts from szszss/TpacTool
(Geometry.cs, Metamesh.cs, Mesh.cs, ClothingMaterial.cs, BoundingBox.cs), verified on the real files.

What changes per file:
  Geometry  ResourceFile -> $BASE/Modules/AWOIAF_Map/AssetSources/MapAssets/icons/<fbx>, where <fbx> is
            the file in our icons/ whose xxHash64 equals the recorded fbx checksum. If our copy is a
            different export (10 of 33 — the pristine subfolder version was the compiled one), the
            matching source fbx is copied in as <stem>_alt.fbx so the editor sees an unchanged source
            and does NOT re-import. ReferencedTextures lowercased (our texture names are lowercase).
  Metamesh  every sub-mesh Material / SecondMaterial GUID: ADOD material -> our material of the same
            name; GUIDs of no ADOD material are left untouched.
  Package   fresh package GUID; per-asset checknum recomputed; header dataOffset and every segment
            offset shifted by the index-size delta; segment bytes copied verbatim. Asset GUIDs kept.

Usage:
    python tools/awoiaf_map/port_geo_meshes.py            # DRY-RUN
    python tools/awoiaf_map/port_geo_meshes.py --apply
    ... [--adod-root DIR] [--module-dir DIR]
"""
from __future__ import annotations

import argparse
import os
import struct
import uuid
from dataclasses import dataclass, field
from pathlib import Path

import tpac

GAME = Path(os.environ.get("BANNERLORD_GAME_DIR",
                           r"E:\Steam\steamapps\common\Mount & Blade II Bannerlord"))
DEFAULT_ADOD_ROOT = Path(r"E:\LOTRAOMAssets\A Dance of Dragons - Map")   # has Assets/ + AssetSources/
DEFAULT_MODULE_DIR = GAME / "Modules" / "AWOIAF_Map"
MODULE_FOLDER = "AWOIAF_Map"

GEOMETRY_TYPE = uuid.UUID("3eba3679-debd-4c7a-8634-f121f6325e33").bytes_le
METAMESH_TYPE = uuid.UUID("a08f8b97-197c-4bea-b95b-53846cae834e").bytes_le
ZERO_GUID = b"\x00" * 16
SEGMENT_TAIL = 16 + 16 + 8 + 4 + 1   # guid, type guid, u64, u32, u8 after the three sizes


# ---------------------------------------------------------------- package (multi-asset, with segments)

@dataclass
class Segment:
    offset: int
    actual: int
    storage: int
    tail: bytes          # guid + type guid + u64 + u32 + u8, verbatim


@dataclass
class Asset:
    type_guid: bytes
    asset_guid: bytes
    version: int
    name: str
    meta: bytes
    checknum: int
    segments: list[Segment]
    deps: bytes          # i32 count + 48-byte entries, verbatim


@dataclass
class Package:
    package_guid: bytes
    assets: list[Asset] = field(default_factory=list)
    index_len: int = 0   # bytes after the 36-byte header up to the data section


def _rs(b: bytes, q: int) -> tuple[str, int]:
    n = struct.unpack_from("<i", b, q)[0]
    q += 4
    return b[q:q + n].decode("utf-8"), q + n


def _sized(s: str) -> bytes:
    e = s.encode("utf-8")
    return struct.pack("<i", len(e)) + e


def parse_package(data: bytes) -> Package:
    if data[:4] != b"TPAC" or struct.unpack_from("<I", data, 4)[0] != 2:
        raise ValueError("not a TPAC v2 file")
    pkg = Package(data[8:24])
    count = struct.unpack_from("<I", data, 0x18)[0]
    data_offset = struct.unpack_from("<I", data, 0x1C)[0]
    pos = 0x24
    for _ in range(count):
        t, g = data[pos:pos + 16], data[pos + 16:pos + 32]
        pos += 32
        ver = struct.unpack_from("<I", data, pos)[0]
        pos += 4
        name, pos = _rs(data, pos)
        msize = struct.unpack_from("<Q", data, pos)[0]
        pos += 8
        meta = data[pos:pos + msize]
        pos += msize
        chk = struct.unpack_from("<Q", data, pos)[0]
        pos += 8
        nseg = struct.unpack_from("<i", data, pos)[0]
        pos += 4
        segs = []
        for _ in range(nseg):
            off, act, sto = struct.unpack_from("<QQQ", data, pos)
            pos += 24
            segs.append(Segment(off, act, sto, data[pos:pos + SEGMENT_TAIL]))
            pos += SEGMENT_TAIL
        ndep = struct.unpack_from("<i", data, pos)[0]
        deps = data[pos:pos + 4 + ndep * 48]
        pos += 4 + ndep * 48
        pkg.assets.append(Asset(t, g, ver, name, meta, chk, segs, deps))
    pkg.index_len = pos - 0x24
    if pkg.index_len != data_offset:
        raise ValueError(f"index size {pkg.index_len} != header dataOffset {data_offset}")
    return pkg


def serialize_package(package_guid: bytes, assets: list[Asset], src: bytes, old_index_len: int) -> bytes:
    """Re-emit the index (checknums recomputed, offsets shifted) + the source data section verbatim."""
    def index_bytes(delta: int) -> bytes:
        out = b""
        for a in assets:
            out += a.type_guid + a.asset_guid + struct.pack("<I", a.version) + _sized(a.name)
            out += struct.pack("<Q", len(a.meta)) + a.meta + struct.pack("<Q", tpac.checknum(a.meta))
            out += struct.pack("<i", len(a.segments))
            for s in a.segments:
                out += struct.pack("<QQQ", s.offset + delta, s.actual, s.storage) + s.tail
            out += a.deps
        return out
    new_len = len(index_bytes(0))
    delta = new_len - old_index_len
    index = index_bytes(delta)
    header = b"TPAC" + struct.pack("<I", 2) + package_guid + struct.pack("<III", len(assets), len(index), 0)
    return header + index + src[0x24 + old_index_len:]


# ---------------------------------------------------------------- Geometry asset

def parse_geometry(meta: bytes) -> tuple[str, int, list[tuple[bytes, bytes]], list[str]]:
    q = 4
    rf, q = _rs(meta, q)
    chk = struct.unpack_from("<Q", meta, q)[0]
    q += 8
    n = struct.unpack_from("<i", meta, q)[0]
    q += 4
    using = [(meta[q + 32 * i:q + 32 * i + 16], meta[q + 32 * i + 16:q + 32 * i + 32]) for i in range(n)]
    q += 32 * n
    n2 = struct.unpack_from("<i", meta, q)[0]
    q += 4
    texs = []
    for _ in range(n2):
        s, q = _rs(meta, q)
        texs.append(s)
    return rf, chk, using, texs


def rebuild_geometry(meta: bytes, new_resource_file: str) -> bytes:
    """Same fields; ResourceFile replaced; texture names lowercased; trailing bytes (if any) kept."""
    rf, chk, using, texs = parse_geometry(meta)
    q = 4
    _rf, q = _rs(meta, q)
    q += 8 + 4 + 32 * len(using) + 4
    for _ in range(len(texs)):
        _s, q = _rs(meta, q)
    trailer = meta[q:]
    out = meta[:4] + _sized(new_resource_file) + struct.pack("<Q", chk)
    out += struct.pack("<i", len(using)) + b"".join(t + g for t, g in using)
    out += struct.pack("<i", len(texs)) + b"".join(_sized(t.lower()) for t in texs) + trailer
    return out


# ---------------------------------------------------------------- Metamesh asset

@dataclass
class MeshRef:
    name: str
    material: bytes
    second: bytes
    material_off: int
    second_off: int


def _skip_strlist(meta: bytes, q: int) -> int:
    n = struct.unpack_from("<I", meta, q)[0]
    q += 4
    for _ in range(n):
        _s, q = _rs(meta, q)
    return q


def parse_metamesh(meta: bytes) -> list[MeshRef]:
    q = 0
    ver = struct.unpack_from("<I", meta, q)[0]
    q += 4 + 16 + 4                    # version, "material" guid (not a material — left alone), float
    _s, q = _rs(meta, q)               # UnknownString
    q += 16                            # ClothMetamesh
    if ver >= 1:
        q += 4
        cloth = struct.unpack_from("<I", meta, q)[0]
        q += 4
        if cloth > 0:
            _s, q = _rs(meta, q)
    n = struct.unpack_from("<i", meta, q)[0]
    q += 4
    meshes = []
    for _ in range(n):
        q += 1 + 4 + 4                 # IsCompleteMesh, Lod, UnknownUint1
        second_off = q
        second = meta[q:q + 16]
        q += 16
        sub = struct.unpack_from("<I", meta, q)[0]
        q += 4 + 16                    # subVersion, mesh guid
        name, q = _rs(meta, q)
        q += 4                         # UnknownUInt2
        q = _skip_strlist(meta, q)     # Flags
        material_off = q
        material = meta[q:q + 16]
        q += 16
        q += 64 + 20 + 4 + 52 + 4      # 4 vec4, 5 ints, bbox type, BoundingBox, UnknownInt2
        q = _skip_strlist(meta, q)     # MaterialFlags
        q += 4                         # UnknownFloat1
        _cn, q = _rs(meta, q)          # ClothingMaterial name
        q += 36 + 4 + 2                # 9 floats, UnknownInt3, 2 bools
        if sub >= 1:
            q += 8 + 1                 # ReadExtraData (2 floats) + bool
        meshes.append(MeshRef(name, material, second, material_off, second_off))
    q += 16                            # Original
    nv = struct.unpack_from("<i", meta, q)[0]
    q += 4 + 16 * nv + 2               # Variations + 2 bools
    if q != len(meta):
        raise ValueError(f"metamesh metadata parse ended at {q} of {len(meta)} bytes")
    return meshes


def remap_metamesh(meta: bytes, guid_map: dict[bytes, bytes]) -> tuple[bytes, int, int]:
    out = bytearray(meta)
    remapped = unknown = 0
    for m in parse_metamesh(meta):
        for g, off in ((m.material, m.material_off), (m.second, m.second_off)):
            if g == ZERO_GUID:
                continue
            if g in guid_map:
                out[off:off + 16] = guid_map[g]
                remapped += 1
            else:
                unknown += 1
    return bytes(out), remapped, unknown


# ---------------------------------------------------------------- port

def port_geo(src: bytes, guid_map: dict[bytes, bytes], new_resource_file: str) -> tuple[bytes, dict]:
    pkg = parse_package(src)
    report = {"materials_remapped": 0, "materials_unknown": 0, "metameshes": 0}
    new_assets = []
    for a in pkg.assets:
        if a.type_guid == GEOMETRY_TYPE:
            meta = rebuild_geometry(a.meta, new_resource_file)
        elif a.type_guid == METAMESH_TYPE:
            meta, r, u = remap_metamesh(a.meta, guid_map)
            report["materials_remapped"] += r
            report["materials_unknown"] += u
            report["metameshes"] += 1
        else:
            meta = a.meta
        new_assets.append(Asset(a.type_guid, a.asset_guid, a.version, a.name, meta, 0, a.segments, a.deps))
    out = serialize_package(uuid.uuid4().bytes_le, new_assets, src, pkg.index_len)
    # post-conditions: re-parse, checknums, segment bytes identical
    back = parse_package(out)
    for old, new in zip(pkg.assets, back.assets):
        if new.checknum != tpac.checknum(new.meta):
            raise ValueError(f"{new.name}: checknum mismatch after rewrite")
        for s_old, s_new in zip(old.segments, new.segments):
            if out[s_new.offset:s_new.offset + s_new.storage] != src[s_old.offset:s_old.offset + s_old.storage]:
                raise ValueError(f"{new.name}: segment bytes changed")
    return out, report


def material_guid_map(adod_assets: Path, our_assets: Path) -> dict[bytes, bytes]:
    ours: dict[str, bytes] = {}
    for p in our_assets.rglob("*_mtl.tpac"):
        a = tpac.read_single_asset(p.read_bytes())
        ours[a.name] = a.asset_guid
    out: dict[bytes, bytes] = {}
    for p in adod_assets.rglob("*_mtl.tpac"):
        a = tpac.read_single_asset(p.read_bytes())
        if a.name in ours:
            out[a.asset_guid] = ours[a.name]
    return out


def main(argv: list[str] | None = None) -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--adod-root", type=Path, default=DEFAULT_ADOD_ROOT, help="folder holding ADOD's Assets/ + AssetSources/")
    ap.add_argument("--module-dir", type=Path, default=DEFAULT_MODULE_DIR)
    ap.add_argument("--apply", action="store_true", help="write geo tpacs (+ _alt fbx copies); default: dry-run")
    args = ap.parse_args(argv)
    adod_assets, adod_sources = args.adod_root / "Assets", args.adod_root / "AssetSources"
    our_assets = args.module_dir / "Assets"
    icons = args.module_dir / "AssetSources" / "MapAssets" / "icons"
    out_dir = our_assets / "MapAssets" / "icons"
    for p in (adod_assets, adod_sources, our_assets, icons):
        if not p.is_dir():
            raise SystemExit(f"missing: {p}")

    guid_map = material_guid_map(adod_assets, our_assets)
    our_fbx = {tpac.xxh64(f.read_bytes()): f for f in sorted(icons.glob("*.fbx"))}
    src_fbx: dict[int, Path] = {}
    for f in sorted(adod_sources.rglob("*.fbx")):
        src_fbx.setdefault(tpac.xxh64(f.read_bytes()), f)

    mode = "APPLY" if args.apply else "DRY-RUN"
    print(f"[{mode}] material guid map: {len(guid_map)}   our fbx: {len(our_fbx)}   source fbx: {len(src_fbx)}")
    plan: list[tuple[Path, bytes, dict, str]] = []
    copies: list[tuple[Path, Path]] = []
    kept: list[str] = []
    errors: list[str] = []
    for geo in sorted(adod_assets.rglob("*_geo.tpac")):
        src = geo.read_bytes()
        pkg = parse_package(src)
        geometry = next((a for a in pkg.assets if a.type_guid == GEOMETRY_TYPE), None)
        if geometry is None:
            errors.append(f"{geo.name}: no Geometry asset")
            continue
        rf, chk, _u, _t = parse_geometry(geometry.meta)
        stem = Path(rf).stem
        target = out_dir / geo.name
        if target.exists():
            kept.append(geo.name)
            continue
        if chk in our_fbx:
            fbx_name = our_fbx[chk].name
        elif chk in src_fbx:
            alt = icons / f"{stem}_alt.fbx"
            n = 1
            while alt.exists() and tpac.xxh64(alt.read_bytes()) != chk:
                n += 1
                alt = icons / f"{stem}_alt{n}.fbx"
            if not alt.exists():
                copies.append((src_fbx[chk], alt))
            fbx_name = alt.name
        else:
            errors.append(f"{geo.name}: no fbx with checksum {chk:016x} in {icons} or {adod_sources} (wants {Path(rf).name})")
            continue
        new_rf = f"$BASE/Modules/{MODULE_FOLDER}/AssetSources/MapAssets/icons/{fbx_name}"
        out, report = port_geo(src, guid_map, new_rf)
        plan.append((target, out, report, fbx_name))

    print(f"[{mode}] geo tpacs: {len(plan) + len(kept) + len(errors)}   to write: {len(plan)}   already present (kept): {len(kept)}   errors: {len(errors)}")
    print(f"[{mode}] fbx to copy as _alt: {len(copies)}")
    for s, d in copies:
        print(f"[{mode}]   {s.relative_to(adod_sources)}  ->  icons/{d.name}   (our {d.name.replace('_alt', '')} is a different export)")
    tot_r = sum(r["materials_remapped"] for _t, _o, r, _f in plan)
    tot_u = sum(r["materials_unknown"] for _t, _o, r, _f in plan)
    for target, _o, r, fbx_name in plan:
        print(f"[{mode}]   {target.name:32s} <- {fbx_name:28s} metameshes {r['metameshes']:3d}  material refs remapped {r['materials_remapped']:3d}  unknown {r['materials_unknown']}")
    print(f"[{mode}] material references: remapped {tot_r}, left untouched (no ADOD material) {tot_u}")
    for e in errors:
        print(f"[{mode}]   ERROR {e}")
    if errors:
        raise SystemExit(f"refusing: {len(errors)} geo(s) cannot be ported")
    if not plan and not copies:
        print(f"[{mode}] nothing to do")
        return
    if not args.apply:
        print("[DRY-RUN] nothing written. Re-run with --apply.")
        return
    for s, d in copies:
        tmp = d.with_suffix(".fbx.tmp")
        tmp.write_bytes(s.read_bytes())
        os.replace(tmp, d)
    out_dir.mkdir(parents=True, exist_ok=True)
    for target, out, _r, _f in plan:
        tmp = target.with_suffix(".tpac.tmp")
        tmp.write_bytes(out)
        os.replace(tmp, target)
        back = parse_package(target.read_bytes())
        for a in back.assets:
            for s in a.segments:
                if s.offset + s.storage > target.stat().st_size:
                    raise SystemExit(f"post-check: {target.name} segment out of range")
    print(f"[APPLY] wrote {len(plan)} geo tpacs to {out_dir} and copied {len(copies)} fbx")


if __name__ == "__main__":
    main()
