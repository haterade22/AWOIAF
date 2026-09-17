#!/usr/bin/env python3
"""Unit tests for tools/awoiaf_map/create_module.py (synthetic trees, no game install).

Run:  python -m unittest tools.tests.test_create_awoiaf_map_module
  or: python -m unittest discover -s tools/tests -p "test_*.py"

Pins the load-bearing invariants of the module-creation tool:
  - the rendered SubModule.xml is the v1.5.3 vanilla-launcher form (DependedModules/DependedModule
    with Optional="true" for DOTS; NO DependedModuleMetadatas, which vanilla ModuleInfo ignores)
  - the mbproj rewrite only retargets XMLDirectory
  - source validation names every missing required file
  - the target guard refuses an existing target (even an empty dir) BEFORE anything is copied
  - the robocopy command excludes RuntimeDataCache, the nested duplicate and *.prev
  - dry-run never creates the target; --apply on a tiny tree produces the expected module
"""
import io
import os
import shutil
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
from contextlib import redirect_stdout
from pathlib import Path

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "awoiaf_map"))

import create_module as cm  # noqa: E402


ORIGINAL_MBPROJ = (
    '<?xml version="1.0" encoding="utf-8"?>\r\n'
    '<base xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" '
    'xmlns:xsd="http://www.w3.org/2001/XMLSchema" type="solution">\r\n'
    '  <outputDirectory>..\\MBModule\\MBModule\\</outputDirectory>\r\n'
    '  <XMLDirectory>..Modules\\A Dance of Dragons - Map</XMLDirectory>\r\n'
    '  <ModuleAssemblyDirectory>..\\bin\\</ModuleAssemblyDirectory>\r\n'
    '  <file id="soln_flora_kinds" name="ModuleData/flora_kinds.xml" type="flora_kind" />\r\n'
    '</base>'
)


def make_source(root: Path, *, nested_dup: bool = True) -> Path:
    """Build a minimal ADOD-shaped source tree."""
    src = root / "A Dance of Dragons - Map"
    (src / "SceneObj" / "Main_map").mkdir(parents=True)
    (src / "SceneObj" / "Main_map" / "scene.xscene").write_bytes(b'<scene name="Main_map"/>')
    (src / "ModuleData" / "DistanceCaches").mkdir(parents=True)
    (src / "ModuleData" / "settlements.xml").write_bytes(b"\xef\xbb\xbf<Settlements/>\r\n")
    (src / "ModuleData" / "settlements_distance_cache.bin").write_bytes(b"legacy")
    (src / "ModuleData" / "DistanceCaches" / "settlements_distance_cache_Default.bin").write_bytes(b"modern")
    (src / "ModuleData" / "DistanceCaches" / "settlements_distance_cache_Default.bin.prev").write_bytes(b"old")
    (src / "ModuleData" / "project.mbproj").write_bytes(ORIGINAL_MBPROJ.encode("utf-8"))
    (src / "SubModule.xml").write_bytes(b'<Module><Id value="ADODMap"/></Module>\r\n')
    (src / "Assets").mkdir()
    (src / "Assets" / "x.tpac").write_bytes(b"tpac")
    (src / "RuntimeDataCache").mkdir()
    (src / "RuntimeDataCache" / "a.rdc").write_bytes(b"rdc")
    if nested_dup:
        (src / "A Dance of Dragons - Map").mkdir()
        (src / "A Dance of Dragons - Map" / "SubModule.xml").write_bytes(b"<Module/>")
    return src


class RenderSubModuleTests(unittest.TestCase):
    def setUp(self):
        self.text = cm.render_submodule_xml()
        self.root = ET.fromstring(self.text)

    def test_identity(self):
        self.assertEqual(self.root.find("Id").get("value"), "AWOIAF_Map")
        self.assertEqual(self.root.find("Name").get("value"), "A World of Ice and Fire - Map")
        self.assertEqual(self.root.find("Version").get("value"), "v1.0.0")
        self.assertEqual(self.root.find("ModuleCategory").get("value"), "Singleplayer")
        # v1.5.3 ModuleInfo reads <ModuleType> (enum default Community) and never reads <Official>
        self.assertEqual(self.root.find("ModuleType").get("value"), "Community")
        self.assertIsNone(self.root.find("Official"))

    def test_vanilla_153_dependency_form(self):
        deps = self.root.find("DependedModules").findall("DependedModule")
        ids = [d.get("Id") for d in deps]
        self.assertEqual(ids, ["Native", "SandBoxCore", "Sandbox", "CustomBattle", "StoryMode", "DOTS"])
        by_id = {d.get("Id"): d for d in deps}
        self.assertEqual(by_id["DOTS"].get("Optional"), "true")
        for hard in ("Native", "SandBoxCore", "Sandbox", "CustomBattle", "StoryMode"):
            self.assertIsNone(by_id[hard].get("Optional"), hard)
        # vanilla ModuleInfo.LoadWithFullPath (v1.5.3) never reads this element -> must not rely on it
        self.assertIsNone(self.root.find("DependedModuleMetadatas"))

    def test_settlements_xml_node_and_empty_submodules(self):
        node = self.root.find("Xmls").find("XmlNode")
        self.assertEqual(node.find("XmlName").get("id"), "Settlements")
        self.assertEqual(node.find("XmlName").get("path"), "settlements")
        types = [g.get("value") for g in node.find("IncludedGameTypes").findall("GameType")]
        self.assertEqual(types, ["Campaign", "CampaignStoryMode"])
        self.assertIsNotNone(self.root.find("SubModules"))
        self.assertEqual(len(list(self.root.find("SubModules"))), 0)

    def test_crlf_no_bom(self):
        raw = cm.render_submodule_xml().encode("utf-8")
        self.assertFalse(raw.startswith(b"\xef\xbb\xbf"))
        self.assertGreater(raw.count(b"\r\n"), 0)
        self.assertEqual(raw.count(b"\n"), raw.count(b"\r\n"))  # no bare LF


class RenderMbprojTests(unittest.TestCase):
    def test_only_xmldirectory_changes(self):
        out = cm.render_mbproj(ORIGINAL_MBPROJ)
        self.assertIn("<XMLDirectory>..\\Modules\\AWOIAF_Map\\</XMLDirectory>", out)
        self.assertNotIn("A Dance of Dragons", out)
        self.assertIn('<file id="soln_flora_kinds" name="ModuleData/flora_kinds.xml" type="flora_kind" />', out)
        self.assertIn("<outputDirectory>..\\MBModule\\MBModule\\</outputDirectory>", out)
        self.assertEqual(out.count("\r\n"), ORIGINAL_MBPROJ.count("\r\n"))

    def test_missing_xmldirectory_is_an_error(self):
        with self.assertRaises(SystemExit):
            cm.render_mbproj('<base type="solution"></base>')


class SourceValidationTests(unittest.TestCase):
    def test_reports_every_missing_required_file(self):
        with tempfile.TemporaryDirectory() as tmp:
            src = Path(tmp) / "empty"
            src.mkdir()
            missing = cm.validate_source(src)
        self.assertEqual(sorted(missing), sorted(cm.REQUIRED_SOURCE_FILES))

    def test_complete_source_passes(self):
        with tempfile.TemporaryDirectory() as tmp:
            src = make_source(Path(tmp))
            self.assertEqual(cm.validate_source(src), [])


class TargetGuardTests(unittest.TestCase):
    def test_existing_empty_dir_is_refused(self):
        with tempfile.TemporaryDirectory() as tmp:
            dst = Path(tmp) / "AWOIAF_Map"
            dst.mkdir()
            with self.assertRaises(SystemExit):
                cm.guard_target(dst)

    def test_absent_target_passes(self):
        with tempfile.TemporaryDirectory() as tmp:
            cm.guard_target(Path(tmp) / "AWOIAF_Map")  # no raise


class RobocopyCommandTests(unittest.TestCase):
    def test_excludes_and_flags(self):
        cmd = cm.robocopy_cmd(Path(r"C:\src"), Path(r"C:\dst"))
        self.assertEqual(cmd[0].lower(), "robocopy")
        self.assertEqual(cmd[1:3], [r"C:\src", r"C:\dst"])
        self.assertIn("/E", cmd)
        xd = cmd[cmd.index("/XD") + 1: cmd.index("/XF")]
        self.assertEqual(xd, [str(Path(r"C:\src") / "RuntimeDataCache"),
                              str(Path(r"C:\src") / "A Dance of Dragons - Map")])
        self.assertIn("*.prev", cmd[cmd.index("/XF") + 1:])
        self.assertNotIn("/MOV", cmd)
        self.assertNotIn("/MIR", cmd)  # never delete anything at the destination


class MainTests(unittest.TestCase):
    def test_dry_run_creates_nothing(self):
        with tempfile.TemporaryDirectory() as tmp:
            src = make_source(Path(tmp))
            dst = Path(tmp) / "Modules" / "AWOIAF_Map"
            buf = io.StringIO()
            with redirect_stdout(buf):
                cm.main(["--source", str(src), "--target", str(dst)])
            self.assertFalse(dst.exists())
            self.assertFalse((Path(tmp) / "Modules").exists())
            self.assertIn("DRY-RUN", buf.getvalue())
            self.assertIn('Id value="AWOIAF_Map"', buf.getvalue())

    @unittest.skipUnless(shutil.which("robocopy"), "robocopy is Windows-only")
    def test_apply_produces_expected_module(self):
        with tempfile.TemporaryDirectory() as tmp:
            src = make_source(Path(tmp))
            dst = Path(tmp) / "Modules" / "AWOIAF_Map"
            with redirect_stdout(io.StringIO()):
                cm.main(["--source", str(src), "--target", str(dst), "--apply"])
            self.assertTrue((dst / "SceneObj" / "Main_map" / "scene.xscene").exists())
            self.assertTrue((dst / "ModuleData" / "settlements.xml").exists())
            self.assertTrue((dst / "ModuleData" / "settlements_distance_cache.bin").exists(),
                            "legacy bin is the transcoder's INPUT; it must be kept")
            self.assertTrue((dst / "ModuleData" / "DistanceCaches" / "settlements_distance_cache_Default.bin").exists())
            self.assertFalse((dst / "ModuleData" / "DistanceCaches" / "settlements_distance_cache_Default.bin.prev").exists())
            self.assertFalse((dst / "RuntimeDataCache").exists())
            self.assertFalse((dst / "A Dance of Dragons - Map").exists())
            self.assertTrue((dst / "Assets" / "x.tpac").exists())
            root = ET.parse(dst / "SubModule.xml").getroot()
            self.assertEqual(root.find("Id").get("value"), "AWOIAF_Map")
            mb = (dst / "ModuleData" / "project.mbproj").read_bytes().decode("utf-8")
            self.assertIn("<XMLDirectory>..\\Modules\\AWOIAF_Map\\</XMLDirectory>", mb)
            # the source is untouched
            self.assertEqual((src / "SubModule.xml").read_bytes(), b'<Module><Id value="ADODMap"/></Module>\r\n')

    def test_apply_refuses_when_target_exists_and_copies_nothing(self):
        with tempfile.TemporaryDirectory() as tmp:
            src = make_source(Path(tmp))
            dst = Path(tmp) / "Modules" / "AWOIAF_Map"
            dst.mkdir(parents=True)
            with redirect_stdout(io.StringIO()):
                with self.assertRaises(SystemExit):
                    cm.main(["--source", str(src), "--target", str(dst), "--apply"])
            self.assertEqual(list(dst.iterdir()), [])


if __name__ == "__main__":
    unittest.main()
