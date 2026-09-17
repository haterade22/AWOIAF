#!/usr/bin/env python3
"""Minimal Bannerlord .tpac support for DOTS tooling: single-asset packages (the editor-form
`*_tex.tpac` / `*_mtl.tpac` files), the material metadata layout, and the per-asset checknum.

Layout (from szszss/TpacTool `AssetPackage.cs`, fetched 2026-09-16; verified byte-for-byte against
ADOD 1.2.12 and fresh v1.5.3 files):

    u32 "TPAC"  u32 version(=2)  guid package  u32 resourceCount  u32 dataOffset  u32 reserve
    per asset:  guid type  guid asset  u32 version  str name  u64 metadataSize  bytes metadata
                i64 checknum  i32 segmentCount  [segments...]  i32 depCount  [3 x guid ...]
    str = i32 length + UTF-8 bytes.  guid = 16 bytes in .NET (bytes_le) order.

checknum: TpacTool calls it "unknown" and writes 0. It is **xxHash64(seed 0) over
`u64 metadataSize` + `metadata`** — matched exactly on two independent real materials
(0x4adb03c92bb54b83 and 0x3d99dbfb97f8c5fb). Recompute it after any metadata edit.

Material metadata (`Material.cs`): u32 version, guid billboard, u32 subVersion, u32, str[] flags, u32,
str[] vertexLayoutFlags, str blendMode, guid shader, i32 texCount, (i32 slot, guid texture)*,
f32 alphaTest, str[] shaderMaterialFlags, ExtraMaterialSettings floats. Textures are referenced by
GUID, never by name — which is why materials cannot simply be copied between modules.
"""
from __future__ import annotations

import struct
import uuid
from dataclasses import dataclass

MATERIAL_TYPE = uuid.UUID("1db01393-6902-4f19-83ba-b37a39830717").bytes_le
TEXTURE_TYPE = uuid.UUID("c974cbcb-5f1c-49f6-9a32-2b5b6c92c2e8").bytes_le

_M64 = 0xFFFFFFFFFFFFFFFF
_P1, _P2, _P3, _P4, _P5 = (0x9E3779B185EBCA87, 0xC2B2AE3D27D4EB4F, 0x165667B19E3779F9,
                           0x85EBCA77C2B2AE63, 0x27D4EB2F165667C5)


def _rotl(x: int, r: int) -> int:
    return ((x << r) | (x >> (64 - r))) & _M64


def xxh64(data: bytes, seed: int = 0) -> int:
    n, i = len(data), 0
    if n >= 32:
        v = [(seed + _P1 + _P2) & _M64, (seed + _P2) & _M64, seed & _M64, (seed - _P1) & _M64]
        while i + 32 <= n:
            for k in range(4):
                lane = struct.unpack_from("<Q", data, i)[0]
                i += 8
                v[k] = (_rotl((v[k] + lane * _P2) & _M64, 31) * _P1) & _M64
        h = (_rotl(v[0], 1) + _rotl(v[1], 7) + _rotl(v[2], 12) + _rotl(v[3], 18)) & _M64
        for vk in v:
            h ^= (_rotl((vk * _P2) & _M64, 31) * _P1) & _M64
            h = (h * _P1 + _P4) & _M64
    else:
        h = (seed + _P5) & _M64
    h = (h + n) & _M64
    while i + 8 <= n:
        k = struct.unpack_from("<Q", data, i)[0]
        i += 8
        h ^= (_rotl((k * _P2) & _M64, 31) * _P1) & _M64
        h = (_rotl(h, 27) * _P1 + _P4) & _M64
    while i + 4 <= n:
        h ^= (struct.unpack_from("<I", data, i)[0] * _P1) & _M64
        i += 4
        h = (_rotl(h, 23) * _P2 + _P3) & _M64
    while i < n:
        h ^= (data[i] * _P5) & _M64
        i += 1
        h = (_rotl(h, 11) * _P1) & _M64
    h ^= h >> 33
    h = (h * _P2) & _M64
    h ^= h >> 29
    h = (h * _P3) & _M64
    h ^= h >> 32
    return h


def checknum(meta: bytes) -> int:
    return xxh64(struct.pack("<Q", len(meta)) + meta)


@dataclass
class SingleAsset:
    package_guid: bytes
    type_guid: bytes
    asset_guid: bytes
    version: int
    name: str
    meta: bytes
    checknum: int
    meta_offset: int          # offset of metadata within the file
    tail: bytes               # everything after the checknum (segments + deps), preserved verbatim


def read_single_asset(data: bytes) -> SingleAsset:
    if data[:4] != b"TPAC":
        raise ValueError("not a TPAC file")
    if struct.unpack_from("<I", data, 4)[0] != 2:
        raise ValueError("unsupported TPAC version")
    count = struct.unpack_from("<I", data, 0x18)[0]
    if count != 1:
        raise ValueError(f"expected a single-asset tpac, found {count} assets")
    pos = 0x24
    type_guid, asset_guid = data[pos:pos + 16], data[pos + 16:pos + 32]
    pos += 32
    version = struct.unpack_from("<I", data, pos)[0]
    pos += 4
    nlen = struct.unpack_from("<i", data, pos)[0]
    pos += 4
    name = data[pos:pos + nlen].decode("utf-8")
    pos += nlen
    msize = struct.unpack_from("<Q", data, pos)[0]
    pos += 8
    meta = data[pos:pos + msize]
    meta_offset = pos
    pos += msize
    chk = struct.unpack_from("<Q", data, pos)[0]
    pos += 8
    return SingleAsset(data[8:24], type_guid, asset_guid, version, name, meta, chk, meta_offset, data[pos:])


def write_single_asset(package_guid: bytes, type_guid: bytes, asset_guid: bytes, name: str, meta: bytes,
                       version: int = 0, tail: bytes | None = None) -> bytes:
    """Assemble a single-asset tpac with no data segments and no dependencies (unless `tail` given)."""
    nb = name.encode("utf-8")
    body = (type_guid + asset_guid + struct.pack("<I", version) + struct.pack("<i", len(nb)) + nb
            + struct.pack("<Q", len(meta)) + meta + struct.pack("<Q", checknum(meta))
            + (tail if tail is not None else struct.pack("<ii", 0, 0)))
    # dataOffset = size of the index after the 36-byte header (== file length here, no data segments);
    # verified against editor-written 1.2.12 and 1.5.3 files (475-byte material -> 439).
    return b"TPAC" + struct.pack("<I", 2) + package_guid + struct.pack("<III", 1, len(body), 0) + body


def _read_sized(meta: bytes, p: int) -> tuple[str, int]:
    n = struct.unpack_from("<i", meta, p)[0]
    p += 4
    return meta[p:p + n].decode("utf-8"), p + n


def _skip_list(meta: bytes, p: int) -> int:
    n = struct.unpack_from("<I", meta, p)[0]
    p += 4
    for _ in range(n):
        _s, p = _read_sized(meta, p)
    return p


def material_texture_slots(meta: bytes) -> list[tuple[int, bytes, int]]:
    """[(slot, texture_guid_bytes_le, offset_of_guid_within_meta)] in file order."""
    p = 4 + 16 + 4 + 4                     # version, billboard guid, subVersion, unknown
    p = _skip_list(meta, p)                # flags
    p += 4                                 # unknown
    p = _skip_list(meta, p)                # vertex layout flags
    _blend, p = _read_sized(meta, p)
    p += 16                                # shader guid
    n = struct.unpack_from("<i", meta, p)[0]
    p += 4
    out = []
    for _ in range(n):
        slot = struct.unpack_from("<i", meta, p)[0]
        p += 4
        out.append((slot, meta[p:p + 16], p))
        p += 16
    return out


def material_summary(meta: bytes) -> dict:
    """flags / vertex-layout flags / blend / shader / shader-material flags, for reports."""
    p = 4 + 16
    sub = struct.unpack_from("<I", meta, p)[0]
    p += 8
    def lst(p):
        n = struct.unpack_from("<I", meta, p)[0]
        p += 4
        items = []
        for _ in range(n):
            s, p = _read_sized(meta, p)
            items.append(s)
        return items, p
    flags, p = lst(p)
    p += 4
    vlf, p = lst(p)
    blend, p = _read_sized(meta, p)
    shader = str(uuid.UUID(bytes_le=meta[p:p + 16]))
    p += 16
    n = struct.unpack_from("<i", meta, p)[0]
    p += 4 + n * 20 + 4
    smf, p = lst(p)
    return {"sub_version": sub, "flags": flags, "vertex_layout": vlf, "blend": blend, "shader": shader,
            "shader_material_flags": smf}


def retarget_material(data: bytes, guid_map: dict[bytes, bytes]) -> tuple[bytes, list[tuple[int, bytes]]]:
    """Copy a material tpac with fresh package/asset GUIDs and its texture GUIDs remapped through
    `guid_map`; unmapped texture GUIDs are kept and returned as [(slot, guid)]. Same length as input."""
    a = read_single_asset(data)
    if a.type_guid != MATERIAL_TYPE:
        raise ValueError(f"{a.name}: not a material tpac")
    meta = bytearray(a.meta)
    unmapped: list[tuple[int, bytes]] = []
    for slot, g, off in material_texture_slots(a.meta):
        if g in guid_map:
            meta[off:off + 16] = guid_map[g]
        else:
            unmapped.append((slot, g))
    out = write_single_asset(uuid.uuid4().bytes_le, MATERIAL_TYPE, uuid.uuid4().bytes_le, a.name,
                             bytes(meta), a.version, a.tail)
    if len(out) != len(data):
        raise ValueError(f"{a.name}: rewritten tpac changed size ({len(data)} -> {len(out)})")
    return out, unmapped


_SEGMENT_ENTRY = 8 + 8 + 8 + 16 + 16 + 8 + 4 + 1   # offset, actual, storage, guid, type guid, u64, u32, u8
_DEP_ENTRY = 16 * 3


def iter_asset_index(data: bytes):
    """Yield (type_guid, asset_guid, name) for every asset in a (possibly multi-asset, packed) tpac.
    Only the index region is walked; data segments are never read. `data` may be a prefix of the file
    as long as it covers the index (header dataOffset + 36 bytes)."""
    if data[:4] != b"TPAC" or struct.unpack_from("<I", data, 4)[0] != 2:
        raise ValueError("not a TPAC v2 file")
    count = struct.unpack_from("<I", data, 0x18)[0]
    pos = 0x24
    for _ in range(count):
        type_guid, asset_guid = data[pos:pos + 16], data[pos + 16:pos + 32]
        pos += 32 + 4                                   # + version
        nlen = struct.unpack_from("<i", data, pos)[0]
        pos += 4
        name = data[pos:pos + nlen].decode("utf-8", errors="replace")
        pos += nlen
        msize = struct.unpack_from("<Q", data, pos)[0]
        pos += 8 + msize + 8                            # metadata + checknum
        nseg = struct.unpack_from("<i", data, pos)[0]
        pos += 4 + nseg * _SEGMENT_ENTRY
        ndep = struct.unpack_from("<i", data, pos)[0]
        pos += 4 + ndep * _DEP_ENTRY
        yield type_guid, asset_guid, name


def index_size(data_head: bytes) -> int:
    """Bytes to read to cover a tpac's whole index (header + dataOffset)."""
    return 0x24 + struct.unpack_from("<I", data_head, 0x1C)[0]
