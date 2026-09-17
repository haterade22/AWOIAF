#!/usr/bin/env python3
"""Unit tests for tools/awoiaf_map/tpac.py — minimal single-asset .tpac reader/writer + material parser.

Run:  python -m unittest tools.tests.test_tpac

Layout source: szszss/TpacTool (TpacTool.Lib/Package/AssetPackage.cs, Material/Material.cs), fetched
2026-09-16. The per-asset "unknown checknum" is xxHash64(seed 0) over the 8-byte metadataSize field +
the metadata bytes — established against two real material tpacs (ADOD 1.2.12 and a fresh 1.5.3 one).
"""
import os
import struct
import sys
import unittest
import uuid

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "awoiaf_map"))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import tpac  # noqa: E402


from material_fixture import material_meta  # noqa: E402


class XxHash64Tests(unittest.TestCase):
    def test_reference_vectors(self):
        self.assertEqual(tpac.xxh64(b""), 0xEF46DB3751D8E999)
        self.assertEqual(tpac.xxh64(b"a"), 0xD24EC4F1A98C6E5B)
        self.assertEqual(tpac.xxh64(b"abc"), 0x44BC2CF5AD770999)
        self.assertEqual(tpac.xxh64(b"a" * 100), tpac.xxh64(b"a" * 100))
        self.assertNotEqual(tpac.xxh64(b"a" * 40), tpac.xxh64(b"b" * 40))


class RoundTripTests(unittest.TestCase):
    def test_write_then_read_single_asset(self):
        t0, t2 = uuid.uuid4().bytes_le, uuid.uuid4().bytes_le
        meta = material_meta([(0, t0), (2, t2)])
        pkg, asset = uuid.uuid4().bytes_le, uuid.uuid4().bytes_le
        data = tpac.write_single_asset(pkg, tpac.MATERIAL_TYPE, asset, "my_mat", meta)
        a = tpac.read_single_asset(data)
        self.assertEqual((a.package_guid, a.type_guid, a.asset_guid, a.name), (pkg, tpac.MATERIAL_TYPE, asset, "my_mat"))
        self.assertEqual(a.meta, meta)
        self.assertEqual(a.checknum, tpac.xxh64(struct.pack("<Q", len(meta)) + meta))
        self.assertEqual(struct.unpack_from("<I", data, 0x18)[0], 1)          # resource count
        self.assertEqual(struct.unpack_from("<I", data, 0x1C)[0], len(data) - 0x24)  # index size after header
        self.assertTrue(data.startswith(b"TPAC\x02\x00\x00\x00"))

    def test_material_slots_and_offsets(self):
        t0, t2 = uuid.uuid4().bytes_le, uuid.uuid4().bytes_le
        meta = material_meta([(0, t0), (2, t2)])
        slots = tpac.material_texture_slots(meta)
        self.assertEqual([(s, g) for s, g, _o in slots], [(0, t0), (2, t2)])
        for _s, g, off in slots:
            self.assertEqual(meta[off:off + 16], g)

    def test_bad_magic_and_multi_asset_are_errors(self):
        with self.assertRaises(ValueError):
            tpac.read_single_asset(b"NOPE" + b"\x00" * 60)
        data = bytearray(tpac.write_single_asset(uuid.uuid4().bytes_le, tpac.MATERIAL_TYPE, uuid.uuid4().bytes_le, "x", material_meta([])))
        struct.pack_into("<I", data, 0x18, 2)
        with self.assertRaises(ValueError):
            tpac.read_single_asset(bytes(data))


class RewriteTests(unittest.TestCase):
    def test_retarget_material_swaps_guids_and_rehashes_same_length(self):
        old0, old2, ext = uuid.uuid4().bytes_le, uuid.uuid4().bytes_le, uuid.uuid4().bytes_le
        new0, new2 = uuid.uuid4().bytes_le, uuid.uuid4().bytes_le
        src = tpac.write_single_asset(uuid.uuid4().bytes_le, tpac.MATERIAL_TYPE, uuid.uuid4().bytes_le, "m",
                                      material_meta([(0, old0), (2, old2), (4, ext)]))
        out, unmapped = tpac.retarget_material(src, {old0: new0, old2: new2})
        self.assertEqual(len(out), len(src))
        a, b = tpac.read_single_asset(src), tpac.read_single_asset(out)
        self.assertNotEqual(a.package_guid, b.package_guid)
        self.assertNotEqual(a.asset_guid, b.asset_guid)
        self.assertEqual(b.name, "m")
        self.assertEqual([g for _s, g, _o in tpac.material_texture_slots(b.meta)], [new0, new2, ext])
        self.assertEqual(unmapped, [(4, ext)])
        self.assertEqual(b.checknum, tpac.xxh64(struct.pack("<Q", len(b.meta)) + b.meta))
        self.assertNotEqual(a.checknum, b.checknum)
        # everything outside guids/checknum is preserved: flags, shader, floats
        self.assertEqual(a.meta[:0x24], b.meta[:0x24])
        self.assertEqual(a.meta[-100:], b.meta[-100:])

    def test_non_material_is_refused(self):
        src = tpac.write_single_asset(uuid.uuid4().bytes_le, tpac.TEXTURE_TYPE, uuid.uuid4().bytes_le, "t", b"\x00" * 8)
        with self.assertRaises(ValueError):
            tpac.retarget_material(src, {})


if __name__ == "__main__":
    unittest.main()
