#!/usr/bin/env python3
"""Unit tests for tools/awoiaf_map/rename_tpac_assets.py (synthetic tpacs, no game install).

Run:  python -m unittest tools.tests.test_rename_tpac_assets

Pins: renaming an asset inside a single- or multi-asset tpac changes only the name field — asset GUIDs
(so GUID references from meshes/materials keep working), metadata, segment bytes are preserved;
checknums are unchanged (the name is outside the hashed region) but re-emitted; segment offsets shift by
the name-length delta; a name not found is a hard error; dry-run writes nothing; idempotent.
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

import rename_tpac_assets as rn  # noqa: E402
import port_geo_meshes as pg  # noqa: E402
import tpac  # noqa: E402
from material_fixture import material_meta  # noqa: E402
from test_port_geo_meshes import build_geo, geometry_meta, metamesh_meta, mesh_record  # noqa: E402


class RenameTests(unittest.TestCase):
    def test_single_asset_material_rename(self):
        g = uuid.uuid4().bytes_le
        src = tpac.write_single_asset(uuid.uuid4().bytes_le, tpac.MATERIAL_TYPE, g, "grass", material_meta([]))
        out = rn.rename_assets(src, {"grass": "adod_grass"})
        a = tpac.read_single_asset(out)
        self.assertEqual((a.name, a.asset_guid, a.meta), ("adod_grass", g, tpac.read_single_asset(src).meta))
        self.assertEqual(a.checknum, tpac.checknum(a.meta))
        self.assertEqual(len(out), len(src) + len("adod_"))

    def test_multi_asset_geo_rename_shifts_segments(self):
        mm = uuid.uuid4().bytes_le
        src = build_geo([
            (pg.GEOMETRY_TYPE, uuid.uuid4().bytes_le, "riverrun.fbx", geometry_meta("$BASE/x/riverrun.fbx", 1, [(pg.METAMESH_TYPE, mm)], []), [b"G" * 10]),
            (pg.METAMESH_TYPE, mm, "cube", metamesh_meta([mesh_record("cube.002", b"\x00" * 16, uuid.uuid4().bytes_le)]), [b"V" * 30]),
        ])
        out = rn.rename_assets(src, {"cube": "adod_cube"})
        a, b = pg.parse_package(src), pg.parse_package(out)
        self.assertEqual([x.name for x in b.assets], ["riverrun.fbx", "adod_cube"])
        self.assertEqual([x.asset_guid for x in b.assets], [x.asset_guid for x in a.assets])
        for x, y in zip(a.assets, b.assets):
            self.assertEqual(x.meta, y.meta)
            for sx, sy in zip(x.segments, y.segments):
                self.assertEqual(sy.offset, sx.offset + len("adod_"))
                self.assertEqual(out[sy.offset:sy.offset + sy.storage], src[sx.offset:sx.offset + sx.storage])
        self.assertEqual(struct.unpack_from("<I", out, 0x1C)[0], b.index_len)

    def test_unknown_name_is_an_error(self):
        src = tpac.write_single_asset(uuid.uuid4().bytes_le, tpac.MATERIAL_TYPE, uuid.uuid4().bytes_le, "grass", material_meta([]))
        with self.assertRaises(SystemExit):
            rn.rename_assets(src, {"nope": "x"})


class MainTests(unittest.TestCase):
    def test_main_renames_files_and_is_idempotent(self):
        with tempfile.TemporaryDirectory() as tmp:
            assets = Path(tmp) / "Assets" / "MapAssets" / "textures"
            assets.mkdir(parents=True)
            p = assets / "grass_mtl.tpac"
            p.write_bytes(tpac.write_single_asset(uuid.uuid4().bytes_le, tpac.MATERIAL_TYPE, uuid.uuid4().bytes_le, "grass", material_meta([])))
            buf = io.StringIO()
            with redirect_stdout(buf):
                rn.main(["--assets", str(assets.parent.parent), "--rename", "grass=adod_grass", "--rename", "cube=adod_cube"])
            self.assertTrue(p.exists())
            self.assertIn("DRY-RUN", buf.getvalue())
            with redirect_stdout(io.StringIO()):
                rn.main(["--assets", str(assets.parent.parent), "--rename", "grass=adod_grass", "--apply"])
            q = assets / "adod_grass_mtl.tpac"
            self.assertTrue(q.exists() and not p.exists())
            self.assertEqual(tpac.read_single_asset(q.read_bytes()).name, "adod_grass")
            buf = io.StringIO()
            with redirect_stdout(buf):
                rn.main(["--assets", str(assets.parent.parent), "--rename", "grass=adod_grass", "--apply"])
            self.assertIn("nothing to do", buf.getvalue())


if __name__ == "__main__":
    unittest.main()
