#!/usr/bin/env python3
"""Unit tests for tools/awoiaf_map/port_geo_meshes.py (synthetic multi-asset tpacs, no game install).

Run:  python -m unittest tools.tests.test_port_geo_meshes

Pins: a geo tpac (Geometry record + Metamesh assets + data segments) is re-emitted with
  - the Geometry ResourceFile retargeted to a checksum-matching fbx in OUR icons/ (copied from the
    pristine source as <stem>_alt.fbx when our copy is a different export), ReferencedTextures lowercased
  - every sub-mesh material / second-material GUID remapped ADOD material -> our material (by name),
    unknown GUIDs untouched
  - per-asset checknums recomputed, header dataOffset and every segment offset shifted by the index
    size delta, segment bytes byte-identical, fresh package GUID
  - no matching fbx anywhere = hard error; existing output kept; dry-run writes nothing; idempotent.
"""
import io
import os
import struct
import sys
import tempfile
import unittest
import uuid
from contextlib import redirect_stdout
from pathlib import Path

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "awoiaf_map"))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import port_geo_meshes as pg  # noqa: E402
import tpac  # noqa: E402
from material_fixture import material_meta, sized  # noqa: E402


def geometry_meta(resource_file: str, fbx_checksum: int, using: list[tuple[bytes, bytes]], textures: list[str]) -> bytes:
    m = struct.pack("<i", 1) + sized(resource_file) + struct.pack("<Q", fbx_checksum)
    m += struct.pack("<i", len(using)) + b"".join(t + g for t, g in using)
    m += struct.pack("<i", len(textures)) + b"".join(sized(t) for t in textures)
    return m


def mesh_record(name: str, material: bytes, second: bytes) -> bytes:
    r = struct.pack("<?iI", True, 0, 0) + second + struct.pack("<I", 1) + uuid.uuid4().bytes_le + sized(name)
    r += struct.pack("<I", 0) + struct.pack("<I", 0)                 # UnknownUInt2, Flags (empty)
    r += material + b"\x00" * 64 + struct.pack("<5i", 1, 2, 3, 4, 0) + struct.pack("<i", 0)
    r += b"\x00" * 52 + struct.pack("<i", 0) + struct.pack("<I", 0) + struct.pack("<f", 0)
    r += sized("") + b"\x00" * 36 + struct.pack("<i", 0) + b"\x00\x00" + b"\x00" * 8 + b"\x00"
    return r


def metamesh_meta(meshes: list[bytes]) -> bytes:
    m = struct.pack("<I", 1) + uuid.uuid4().bytes_le + struct.pack("<f", 0) + sized("") + b"\x00" * 16
    m += struct.pack("<II", 0, 0) + struct.pack("<i", len(meshes)) + b"".join(meshes)
    m += b"\x00" * 16 + struct.pack("<i", 0) + b"\x00\x00"
    return m


def build_geo(assets: list[tuple[bytes, bytes, str, bytes, list[bytes]]]) -> bytes:
    """assets: (type, guid, name, meta, [segment payloads]) -> full tpac bytes (Int32 index, absolute offsets)."""
    index_parts, payloads = [], []
    # first pass to size the index
    def entry(t, g, name, meta, segs, offsets):
        nb = name.encode()
        e = t + g + struct.pack("<I", 1) + struct.pack("<i", len(nb)) + nb + struct.pack("<Q", len(meta)) + meta
        e += struct.pack("<Q", tpac.checknum(meta)) + struct.pack("<i", len(segs))
        for payload, off in zip(segs, offsets):
            e += struct.pack("<QQQ", off, len(payload), len(payload)) + uuid.uuid4().bytes_le + uuid.uuid4().bytes_le
            e += struct.pack("<QIB", 0, 0, 0)
        e += struct.pack("<i", 0)
        return e
    size = 0x24
    for t, g, name, meta, segs in assets:
        size += len(entry(t, g, name, meta, segs, [0] * len(segs)))
    off = size
    for t, g, name, meta, segs in assets:
        offs = []
        for s in segs:
            offs.append(off)
            off += len(s)
            payloads.append(s)
        index_parts.append(entry(t, g, name, meta, segs, offs))
    index = b"".join(index_parts)
    return b"TPAC" + struct.pack("<I", 2) + uuid.uuid4().bytes_le + struct.pack("<III", len(assets), len(index), 0) + index + b"".join(payloads)


def make_world(root: Path, *, our_fbx_matches=True):
    adod = root / "adod" / "Assets" / "MapAssets" / "icons" / "riverrun"
    adod.mkdir(parents=True)
    src_fbx_dir = root / "adod" / "AssetSources" / "MapAssets" / "icons" / "riverrun"
    src_fbx_dir.mkdir(parents=True)
    (src_fbx_dir / "riverrun.fbx").write_bytes(b"FBX-ADOD-COMPILED")
    mod = root / "Modules" / "AWOIAF_Map"
    icons = mod / "AssetSources" / "MapAssets" / "icons"
    icons.mkdir(parents=True)
    (icons / "riverrun.fbx").write_bytes(b"FBX-ADOD-COMPILED" if our_fbx_matches else b"FBX-NEWER-EXPORT")
    tex_dir = mod / "Assets" / "MapAssets" / "textures"
    tex_dir.mkdir(parents=True)
    # ADOD material + our material with the same name
    adod_mat_guid, our_mat_guid = uuid.uuid4().bytes_le, uuid.uuid4().bytes_le
    (adod / "riverrun_material_mtl.tpac").write_bytes(
        tpac.write_single_asset(uuid.uuid4().bytes_le, tpac.MATERIAL_TYPE, adod_mat_guid, "riverrun_material", material_meta([])))
    (tex_dir / "riverrun_material_mtl.tpac").write_bytes(
        tpac.write_single_asset(uuid.uuid4().bytes_le, tpac.MATERIAL_TYPE, our_mat_guid, "riverrun_material", material_meta([])))
    unknown = uuid.uuid4().bytes_le
    mm_guid = uuid.uuid4().bytes_le
    geo = build_geo([
        (pg.GEOMETRY_TYPE, uuid.uuid4().bytes_le, "riverrun.fbx",
         geometry_meta("$BASE/Modules/A Dance of Dragons/AssetSources/MapAssets/icons/riverrun/riverrun.fbx",
                       tpac.xxh64(b"FBX-ADOD-COMPILED"), [(pg.METAMESH_TYPE, mm_guid)], ["Riverrun_D"]),
         [b"GEO-SEG"]),
        (pg.METAMESH_TYPE, mm_guid, "cube",
         metamesh_meta([mesh_record("cube.000", adod_mat_guid, b"\x00" * 16), mesh_record("cube.001", unknown, adod_mat_guid)]),
         [b"V" * 40, b"I" * 12]),
    ])
    (adod / "riverrun_geo.tpac").write_bytes(geo)
    return root / "adod", mod, adod_mat_guid, our_mat_guid, unknown


class PortTests(unittest.TestCase):
    def test_port_remaps_retargets_and_shifts(self):
        with tempfile.TemporaryDirectory() as tmp:
            adod, mod, old_mat, new_mat, unknown = make_world(Path(tmp))
            src = (adod / "Assets" / "MapAssets" / "icons" / "riverrun" / "riverrun_geo.tpac").read_bytes()
            out, report = pg.port_geo(src, {old_mat: new_mat}, "$BASE/Modules/AWOIAF_Map/AssetSources/MapAssets/icons/riverrun.fbx")
            a_in, a_out = pg.parse_package(src), pg.parse_package(out)
            self.assertEqual(len(a_out.assets), 2)
            geo = a_out.assets[0]
            rf, chk, using, texs = pg.parse_geometry(geo.meta)
            self.assertEqual(rf, "$BASE/Modules/AWOIAF_Map/AssetSources/MapAssets/icons/riverrun.fbx")
            self.assertEqual(texs, ["riverrun_d"])
            self.assertEqual(chk, tpac.xxh64(b"FBX-ADOD-COMPILED"))
            mm = a_out.assets[1]
            mats = [(m.material, m.second) for m in pg.parse_metamesh(mm.meta)]
            self.assertEqual(mats, [(new_mat, b"\x00" * 16), (unknown, new_mat)])
            for a in a_out.assets:
                self.assertEqual(a.checknum, tpac.checknum(a.meta))
            delta = a_out.index_len - a_in.index_len
            self.assertNotEqual(delta, 0)
            for si, so in zip(a_in.assets, a_out.assets):
                for x, y in zip(si.segments, so.segments):
                    self.assertEqual(y.offset, x.offset + delta)
                    self.assertEqual(out[y.offset:y.offset + y.storage], src[x.offset:x.offset + x.storage])
            self.assertEqual(struct.unpack_from("<I", out, 0x1C)[0], a_out.index_len)
            self.assertEqual(len(out), a_out.index_len + 0x24 + sum(s.storage for a in a_out.assets for s in a.segments))
            self.assertNotEqual(out[8:24], src[8:24])
            self.assertEqual(report["materials_remapped"], 2)
            self.assertEqual(report["materials_unknown"], 1)


class MainTests(unittest.TestCase):
    def test_apply_with_matching_fbx(self):
        with tempfile.TemporaryDirectory() as tmp:
            adod, mod, *_ = make_world(Path(tmp))
            with redirect_stdout(io.StringIO()):
                pg.main(["--adod-root", str(adod), "--module-dir", str(mod), "--apply"])
            out = mod / "Assets" / "MapAssets" / "icons" / "riverrun_geo.tpac"
            self.assertTrue(out.exists())
            rf, chk, *_ = pg.parse_geometry(pg.parse_package(out.read_bytes()).assets[0].meta)
            self.assertTrue(rf.endswith("/icons/riverrun.fbx"))
            self.assertFalse((mod / "AssetSources" / "MapAssets" / "icons" / "riverrun_alt.fbx").exists())
            snap = out.read_bytes()
            buf = io.StringIO()
            with redirect_stdout(buf):
                pg.main(["--adod-root", str(adod), "--module-dir", str(mod), "--apply"])
            self.assertEqual(out.read_bytes(), snap)
            self.assertIn("already present (kept): 1", buf.getvalue())

    def test_mismatched_fbx_gets_alt_copy_from_source(self):
        with tempfile.TemporaryDirectory() as tmp:
            adod, mod, *_ = make_world(Path(tmp), our_fbx_matches=False)
            with redirect_stdout(io.StringIO()):
                pg.main(["--adod-root", str(adod), "--module-dir", str(mod), "--apply"])
            alt = mod / "AssetSources" / "MapAssets" / "icons" / "riverrun_alt.fbx"
            self.assertEqual(alt.read_bytes(), b"FBX-ADOD-COMPILED")
            self.assertEqual((mod / "AssetSources" / "MapAssets" / "icons" / "riverrun.fbx").read_bytes(), b"FBX-NEWER-EXPORT")
            out = mod / "Assets" / "MapAssets" / "icons" / "riverrun_geo.tpac"
            rf, *_ = pg.parse_geometry(pg.parse_package(out.read_bytes()).assets[0].meta)
            self.assertTrue(rf.endswith("/icons/riverrun_alt.fbx"))

    def test_dry_run_writes_nothing(self):
        with tempfile.TemporaryDirectory() as tmp:
            adod, mod, *_ = make_world(Path(tmp), our_fbx_matches=False)
            before = sorted(str(p.relative_to(mod)) for p in mod.rglob("*"))
            buf = io.StringIO()
            with redirect_stdout(buf):
                pg.main(["--adod-root", str(adod), "--module-dir", str(mod)])
            self.assertEqual(sorted(str(p.relative_to(mod)) for p in mod.rglob("*")), before)
            self.assertIn("DRY-RUN", buf.getvalue())
            self.assertIn("fbx to copy as _alt: 1", buf.getvalue())

    def test_no_matching_fbx_anywhere_is_an_error(self):
        with tempfile.TemporaryDirectory() as tmp:
            adod, mod, *_ = make_world(Path(tmp), our_fbx_matches=False)
            (adod / "AssetSources" / "MapAssets" / "icons" / "riverrun" / "riverrun.fbx").write_bytes(b"ALSO-DIFFERENT")
            with redirect_stdout(io.StringIO()):
                with self.assertRaises(SystemExit):
                    pg.main(["--adod-root", str(adod), "--module-dir", str(mod), "--apply"])
            self.assertFalse((mod / "Assets" / "MapAssets" / "icons" / "riverrun_geo.tpac").exists())


if __name__ == "__main__":
    unittest.main()
