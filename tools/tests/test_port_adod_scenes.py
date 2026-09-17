#!/usr/bin/env python3
"""Unit tests for tools/awoiaf_map/port_adod_scenes.py (synthetic trees, no game install).

Run:  python -m unittest tools.tests.test_port_adod_scenes

Pins the port contract:
  - SceneObj/AssetPackages/Prefabs/NavMeshPrefabs/Atmospheres are copied; Backups, ADOD_Menu_*,
    main_menu_a, EmAssetPackages, Shaders and project.mbproj are NOT
  - existing target files are never overwritten (additive robocopy: /XC /XN /XO)
  - flora_kinds.xml is merged by <flora_kind name>: existing bytes preserved, new kinds appended
    before </flora_kinds>, duplicates by name rejected as a hard error
  - dry-run writes nothing; --apply is idempotent
"""
import io
import os
import shutil
import sys
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "awoiaf_map"))

import port_adod_scenes as ps  # noqa: E402

MAP_FLORA = ('<flora_kinds>\r\n'
             '\t<flora_kind name="beyond_the_wall_trees" view_distance="1000.000">\r\n'
             '\t\t<flags/>\r\n'
             '\t</flora_kind>\r\n'
             '</flora_kinds>\r\n')
SCENES_FLORA = ('<flora_kinds>\r\n'
                '\t<!-- Wheat -->\r\n'
                '\t<flora_kind name="adod_kl_wheat" view_distance="1000.000">\r\n'
                '\t</flora_kind>\r\n'
                '\t<flora_kind name="adod_reeds" view_distance="500.000">\r\n'
                '\t</flora_kind>\r\n'
                '</flora_kinds>\r\n')


def make_trees(root: Path):
    src = root / "A Dance of Dragons - Scenes"
    for scene in ("WesterosiGenericKeep", "HART_WINTERFELL_TOWN", "ADOD_Menu_1", "main_menu_a"):
        (src / "SceneObj" / scene).mkdir(parents=True)
        (src / "SceneObj" / scene / "scene.xscene").write_bytes(b"<scene/>")
    (src / "SceneObj" / "Backups" / "x").mkdir(parents=True)
    (src / "SceneObj" / "Backups" / "x" / "scene.xscene").write_bytes(b"<scene/>")
    (src / "AssetPackages").mkdir()
    (src / "AssetPackages" / "ADODScenesAssets.tpac").write_bytes(b"tpac")
    (src / "EmAssetPackages").mkdir()
    (src / "EmAssetPackages" / "Assets.tpac").write_bytes(b"tpac")
    (src / "Prefabs").mkdir()
    (src / "Prefabs" / "Baratheon_Stag_Statue.xml").write_bytes(b"<prefabs/>")
    (src / "NavMeshPrefabs").mkdir()
    (src / "NavMeshPrefabs" / "DS_Keep_Mesh.bin").write_bytes(b"nav")
    (src / "Atmospheres").mkdir()
    (src / "Atmospheres" / "hart_daytime_1.xml").write_bytes(b"<atm/>")
    (src / "Shaders" / "D3D11").mkdir(parents=True)
    (src / "Shaders" / "D3D11" / "cache.bin").write_bytes(b"sh")
    (src / "ModuleData").mkdir()
    (src / "ModuleData" / "flora_kinds.xml").write_bytes(SCENES_FLORA.encode())
    (src / "ModuleData" / "project.mbproj").write_bytes(b"<base/>")
    (src / "SubModule.xml").write_bytes(b"<Module/>")

    dst = root / "Modules" / "AWOIAF_Map"
    (dst / "SceneObj" / "Main_map").mkdir(parents=True)
    (dst / "SceneObj" / "Main_map" / "scene.xscene").write_bytes(b"<scene name='Main_map'/>")
    (dst / "Prefabs").mkdir()
    (dst / "Prefabs" / "5Forts.xml").write_bytes(b"<prefabs/>")
    (dst / "ModuleData").mkdir()
    (dst / "ModuleData" / "flora_kinds.xml").write_bytes(MAP_FLORA.encode())
    (dst / "ModuleData" / "project.mbproj").write_bytes(b"<base type='solution'/>")
    (dst / "SubModule.xml").write_bytes(b"<Module/>")
    return src, dst


class FloraMergeTests(unittest.TestCase):
    def test_appends_new_kinds_and_preserves_existing_bytes(self):
        out = ps.merge_flora_kinds(MAP_FLORA, SCENES_FLORA)
        self.assertTrue(out.startswith(MAP_FLORA[:-len("</flora_kinds>\r\n")]))
        self.assertTrue(out.endswith("</flora_kinds>\r\n"))
        self.assertIn('<flora_kind name="adod_kl_wheat"', out)
        self.assertIn('<flora_kind name="adod_reeds"', out)
        self.assertEqual(out.count("<flora_kinds>"), 1)
        self.assertEqual(out.count("\n"), out.count("\r\n"))

    def test_idempotent(self):
        once = ps.merge_flora_kinds(MAP_FLORA, SCENES_FLORA)
        self.assertEqual(ps.merge_flora_kinds(once, SCENES_FLORA), once)

    def test_conflicting_duplicate_name_is_an_error(self):
        clash = SCENES_FLORA.replace("adod_reeds", "beyond_the_wall_trees")
        with self.assertRaises(SystemExit):
            ps.merge_flora_kinds(MAP_FLORA, clash)


class CopyPlanTests(unittest.TestCase):
    def test_sceneobj_excludes_and_additive_flags(self):
        cmds = ps.robocopy_cmds(Path(r"C:\src"), Path(r"C:\dst"))
        by_folder = {Path(c[1]).name: c for c in cmds}
        self.assertEqual(set(by_folder), {"SceneObj", "AssetPackages", "Prefabs", "NavMeshPrefabs", "Atmospheres"})
        so = by_folder["SceneObj"]
        xd = so[so.index("/XD") + 1:]
        self.assertEqual(xd, ["Backups", "ADOD_Menu_*", "main_menu_a"])
        for c in cmds:
            for flag in ("/E", "/XC", "/XN", "/XO"):
                self.assertIn(flag, c)
            self.assertNotIn("/MIR", c)


class MainTests(unittest.TestCase):
    def test_dry_run_writes_nothing(self):
        with tempfile.TemporaryDirectory() as tmp:
            src, dst = make_trees(Path(tmp))
            before = sorted(str(p.relative_to(dst)) for p in dst.rglob("*"))
            buf = io.StringIO()
            with redirect_stdout(buf):
                ps.main(["--source", str(src), "--target", str(dst)])
            after = sorted(str(p.relative_to(dst)) for p in dst.rglob("*"))
            self.assertEqual(before, after)
            self.assertEqual((dst / "ModuleData" / "flora_kinds.xml").read_bytes(), MAP_FLORA.encode())
            self.assertIn("DRY-RUN", buf.getvalue())

    @unittest.skipUnless(shutil.which("robocopy"), "robocopy is Windows-only")
    def test_apply_ports_scenes_and_is_idempotent(self):
        with tempfile.TemporaryDirectory() as tmp:
            src, dst = make_trees(Path(tmp))
            with redirect_stdout(io.StringIO()):
                ps.main(["--source", str(src), "--target", str(dst), "--apply"])
            self.assertTrue((dst / "SceneObj" / "WesterosiGenericKeep" / "scene.xscene").exists())
            self.assertTrue((dst / "SceneObj" / "HART_WINTERFELL_TOWN" / "scene.xscene").exists())
            self.assertTrue((dst / "SceneObj" / "Main_map" / "scene.xscene").exists())
            for excluded in ("SceneObj/ADOD_Menu_1", "SceneObj/main_menu_a", "SceneObj/Backups",
                             "EmAssetPackages", "Shaders"):
                self.assertFalse((dst / excluded).exists(), excluded)
            self.assertTrue((dst / "AssetPackages" / "ADODScenesAssets.tpac").exists())
            self.assertTrue((dst / "Prefabs" / "Baratheon_Stag_Statue.xml").exists())
            self.assertTrue((dst / "Prefabs" / "5Forts.xml").exists())
            self.assertTrue((dst / "NavMeshPrefabs" / "DS_Keep_Mesh.bin").exists())
            self.assertTrue((dst / "Atmospheres" / "hart_daytime_1.xml").exists())
            self.assertEqual((dst / "ModuleData" / "project.mbproj").read_bytes(), b"<base type='solution'/>")
            flora = (dst / "ModuleData" / "flora_kinds.xml").read_bytes()
            self.assertIn(b'name="adod_kl_wheat"', flora)
            self.assertIn(b'name="beyond_the_wall_trees"', flora)
            snapshot = sorted((str(p.relative_to(dst)), p.read_bytes()) for p in dst.rglob("*") if p.is_file())
            with redirect_stdout(io.StringIO()):
                ps.main(["--source", str(src), "--target", str(dst), "--apply"])
            again = sorted((str(p.relative_to(dst)), p.read_bytes()) for p in dst.rglob("*") if p.is_file())
            self.assertEqual(snapshot, again)

    @unittest.skipUnless(shutil.which("robocopy"), "robocopy is Windows-only")
    def test_existing_target_file_is_never_overwritten(self):
        with tempfile.TemporaryDirectory() as tmp:
            src, dst = make_trees(Path(tmp))
            (dst / "Prefabs" / "Baratheon_Stag_Statue.xml").write_bytes(b"<mine/>")
            with redirect_stdout(io.StringIO()):
                ps.main(["--source", str(src), "--target", str(dst), "--apply"])
            self.assertEqual((dst / "Prefabs" / "Baratheon_Stag_Statue.xml").read_bytes(), b"<mine/>")

    def test_missing_target_module_is_an_error(self):
        with tempfile.TemporaryDirectory() as tmp:
            src, dst = make_trees(Path(tmp))
            with redirect_stdout(io.StringIO()):
                with self.assertRaises(SystemExit):
                    ps.main(["--source", str(src), "--target", str(Path(tmp) / "nope"), "--apply"])


if __name__ == "__main__":
    unittest.main()
