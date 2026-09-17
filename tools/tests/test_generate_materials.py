#!/usr/bin/env python3
"""Unit tests for tools/awoiaf_map/generate_materials.py (synthetic tpacs, no game install).

Run:  python -m unittest tools.tests.test_generate_materials

Pins: ADOD texture GUIDs are resolved by NAME (lowercased) into our re-imported textures; every ADOD
material is rewritten with fresh package/asset GUIDs, remapped texture GUIDs and a recomputed checknum;
external texture GUIDs (not ADOD map textures) are kept and reported; a referenced ADOD texture missing
from our set is a hard error before any write; existing materials are kept; dry-run writes nothing;
--apply is idempotent.
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

import generate_materials as gm  # noqa: E402
import tpac  # noqa: E402
from material_fixture import material_meta  # noqa: E402


def tex_tpac(name: str, guid: bytes) -> bytes:
    # a minimal texture-shaped single asset: type TEXTURE, some metadata bytes, no segments
    return tpac.write_single_asset(uuid.uuid4().bytes_le, tpac.TEXTURE_TYPE, guid, name, b"\x00" * 24)


def make_trees(root: Path):
    adod = root / "adod" / "Assets" / "MapAssets"
    ours = root / "Modules" / "AWOIAF_Map" / "Assets" / "MapAssets" / "textures"
    (adod / "icons" / "riverrun").mkdir(parents=True)
    (adod / "textures").mkdir(parents=True)
    ours.mkdir(parents=True)
    g = {n: uuid.uuid4().bytes_le for n in ("Riverrun_Baked_d", "Riverrun_Baked_n", "Riverrun_Baked_s", "grass_d", "ext")}
    o = {n: uuid.uuid4().bytes_le for n in ("riverrun_baked_d", "riverrun_baked_n", "riverrun_baked_s", "grass_d")}
    for n in ("Riverrun_Baked_d", "Riverrun_Baked_n", "Riverrun_Baked_s"):
        (adod / "icons" / "riverrun" / f"{n}_tex.tpac").write_bytes(tex_tpac(n, g[n]))
    (adod / "textures" / "grass_d_tex.tpac").write_bytes(tex_tpac("grass_d", g["grass_d"]))
    for n, guid in o.items():
        (ours / f"{n}_tex.tpac").write_bytes(tex_tpac(n, guid))
    mat1 = tpac.write_single_asset(uuid.uuid4().bytes_le, tpac.MATERIAL_TYPE, uuid.uuid4().bytes_le, "riverrun_baked",
                                   material_meta([(0, g["Riverrun_Baked_d"]), (2, g["Riverrun_Baked_n"]), (4, g["Riverrun_Baked_s"])]))
    mat2 = tpac.write_single_asset(uuid.uuid4().bytes_le, tpac.MATERIAL_TYPE, uuid.uuid4().bytes_le, "grass",
                                   material_meta([(0, g["grass_d"]), (2, g["ext"])], flags=("two_sided", "multi_pass_alpha")))
    (adod / "icons" / "riverrun" / "riverrun_baked_mtl.tpac").write_bytes(mat1)
    (adod / "textures" / "grass_mtl.tpac").write_bytes(mat2)
    return root / "adod" / "Assets", root / "Modules" / "AWOIAF_Map", g, o


class IndexTests(unittest.TestCase):
    def test_texture_indexes(self):
        with tempfile.TemporaryDirectory() as tmp:
            adod, mod, g, o = make_trees(Path(tmp))
            by_guid = gm.index_textures_by_guid(adod)
            self.assertEqual(by_guid[g["Riverrun_Baked_d"]], "Riverrun_Baked_d")
            by_name = gm.index_textures_by_name(mod / "Assets")
            self.assertEqual(by_name["riverrun_baked_d"], o["riverrun_baked_d"])

    def test_duplicate_texture_name_in_ours_is_an_error(self):
        with tempfile.TemporaryDirectory() as tmp:
            adod, mod, g, o = make_trees(Path(tmp))
            (mod / "Assets" / "MapAssets" / "textures" / "dup_tex.tpac").write_bytes(tex_tpac("grass_d", uuid.uuid4().bytes_le))
            with self.assertRaises(SystemExit):
                gm.index_textures_by_name(mod / "Assets")


class MainTests(unittest.TestCase):
    def test_dry_run_writes_nothing_and_reports(self):
        with tempfile.TemporaryDirectory() as tmp:
            adod, mod, g, o = make_trees(Path(tmp))
            out = mod / "Assets" / "MapAssets" / "textures"
            before = sorted(p.name for p in out.iterdir())
            buf = io.StringIO()
            with redirect_stdout(buf):
                gm.main(["--adod-assets", str(adod), "--module-dir", str(mod), "--no-resolve-external"])
            self.assertEqual(sorted(p.name for p in out.iterdir()), before)
            self.assertIn("materials to write: 2", buf.getvalue())
            self.assertIn("external texture refs: 1", buf.getvalue())

    def test_apply_writes_remapped_materials_and_is_idempotent(self):
        with tempfile.TemporaryDirectory() as tmp:
            adod, mod, g, o = make_trees(Path(tmp))
            out = mod / "Assets" / "MapAssets" / "textures"
            with redirect_stdout(io.StringIO()):
                gm.main(["--adod-assets", str(adod), "--module-dir", str(mod), "--no-resolve-external", "--apply"])
            m1 = tpac.read_single_asset((out / "riverrun_baked_mtl.tpac").read_bytes())
            self.assertEqual(m1.name, "riverrun_baked")
            self.assertEqual([x for _s, x, _o in tpac.material_texture_slots(m1.meta)],
                             [o["riverrun_baked_d"], o["riverrun_baked_n"], o["riverrun_baked_s"]])
            self.assertEqual(m1.checknum, tpac.checknum(m1.meta))
            # `grass` collides with a vanilla material -> generated as adod_grass (RENAMED_MATERIALS)
            self.assertFalse((out / "grass_mtl.tpac").exists())
            m2 = tpac.read_single_asset((out / "adod_grass_mtl.tpac").read_bytes())
            self.assertEqual(m2.name, "adod_grass")
            self.assertEqual([x for _s, x, _o in tpac.material_texture_slots(m2.meta)], [o["grass_d"], g["ext"]])
            self.assertEqual(tpac.material_summary(m2.meta)["flags"], ["two_sided", "multi_pass_alpha"])
            src = tpac.read_single_asset((adod / "MapAssets" / "textures" / "grass_mtl.tpac").read_bytes())
            self.assertNotEqual(src.asset_guid, m2.asset_guid)
            snap = {p.name: p.read_bytes() for p in out.iterdir()}
            buf = io.StringIO()
            with redirect_stdout(buf):
                gm.main(["--adod-assets", str(adod), "--module-dir", str(mod), "--no-resolve-external", "--apply"])
            self.assertEqual({p.name: p.read_bytes() for p in out.iterdir()}, snap)
            self.assertIn("materials to write: 0", buf.getvalue())
            self.assertIn("already present (kept): 2", buf.getvalue())

    def test_missing_texture_in_ours_is_an_error_before_any_write(self):
        with tempfile.TemporaryDirectory() as tmp:
            adod, mod, g, o = make_trees(Path(tmp))
            out = mod / "Assets" / "MapAssets" / "textures"
            (out / "riverrun_baked_n_tex.tpac").unlink()
            with redirect_stdout(io.StringIO()):
                with self.assertRaises(SystemExit):
                    gm.main(["--adod-assets", str(adod), "--module-dir", str(mod), "--no-resolve-external", "--apply"])
            self.assertFalse((out / "riverrun_baked_mtl.tpac").exists())
            self.assertFalse((out / "grass_mtl.tpac").exists())


if __name__ == "__main__":
    unittest.main()
