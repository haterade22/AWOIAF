#!/usr/bin/env python3
"""Unit tests for tools/audit_scene_names.py + tools/remap_stale_scene_names.py pure functions.

Run:  python -m unittest tools.tests.test_scene_name_tools

Pins: scene refs are collected from scene_name AND scene_name_1..3; folder lookup is case-insensitive
and restricted to the enabled modules; the remap touches every scene_name variant, only exact values,
and reports counts.
"""
import os
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import audit_scene_names as audit  # noqa: E402
import remap_stale_scene_names as remap  # noqa: E402

XML = (
    '<Settlements>\r\n'
    '  <Settlement id="town_B1" name="Winterfell" posX="1" posY="2" culture="Culture.battania">\r\n'
    '    <Locations complex_template="LocationComplexTemplate.town_complex">\r\n'
    '      <Location id="center" scene_name="HART_WINTERFELL_TOWN" scene_name_1="HART_WINTERFELL_TOWN" scene_name_2="sturgia_town_c" />\r\n'
    '      <Location id="arena" scene_name="corspe_lake" />\r\n'
    '    </Locations>\r\n'
    '  </Settlement>\r\n'
    '  <Settlement id="village_1" name="V" posX="1" posY="2" culture="Culture.battania">\r\n'
    '    <Locations complex_template="LocationComplexTemplate.village_complex">\r\n'
    '      <Location id="village_center" scene_name="reach_westerlands_villagee" />\r\n'
    '    </Locations>\r\n'
    '  </Settlement>\r\n'
    '</Settlements>\r\n'
)


class AuditTests(unittest.TestCase):
    def test_collects_all_scene_name_variants_per_settlement(self):
        refs = audit.scenes_in(XML)
        self.assertEqual(sorted(refs), ["HART_WINTERFELL_TOWN", "corspe_lake", "reach_westerlands_villagee", "sturgia_town_c"])
        self.assertEqual(refs["HART_WINTERFELL_TOWN"], ["town_B1", "town_B1"])
        self.assertEqual(refs["reach_westerlands_villagee"], ["village_1"])

    def test_folder_lookup_is_case_insensitive_and_enabled_only(self):
        with tempfile.TemporaryDirectory() as tmp:
            mods = Path(tmp)
            (mods / "SandBox" / "SceneObj" / "sturgia_town_c").mkdir(parents=True)
            (mods / "AWOIAF_Map" / "SceneObj" / "hart_winterfell_town").mkdir(parents=True)
            (mods / "TAOM_Map" / "SceneObj" / "corspe_lake").mkdir(parents=True)  # NOT enabled
            folders = audit.scene_folders(mods, ["SandBox", "AWOIAF_Map"])
            self.assertEqual(folders, {"sturgia_town_c": "SandBox", "hart_winterfell_town": "AWOIAF_Map"})
            miss = audit.missing_scenes(audit.scenes_in(XML), folders)
            self.assertEqual(sorted(miss), ["corspe_lake", "reach_westerlands_villagee"])


class RemapTests(unittest.TestCase):
    def test_remaps_every_variant_and_counts(self):
        out, counts = remap.remap_text(XML, {"corspe_lake": "adod_corpse_lake",
                                             "reach_westerlands_villagee": "reach_westerlands_village",
                                             "HART_WINTERFELL_TOWN": "x"})
        self.assertIn('scene_name="adod_corpse_lake"', out)
        self.assertIn('scene_name="reach_westerlands_village"', out)
        self.assertNotIn("villagee", out)
        self.assertIn('scene_name="x" scene_name_1="x" scene_name_2="sturgia_town_c"', out)
        self.assertEqual(counts, {"corspe_lake": 1, "reach_westerlands_villagee": 1, "HART_WINTERFELL_TOWN": 2})
        self.assertEqual(out.count("\n"), out.count("\r\n"))

    def test_exact_value_only(self):
        out, counts = remap.remap_text(XML, {"reach_westerlands_village": "SHOULD_NOT_MATCH"})
        self.assertEqual(counts, {})
        self.assertEqual(out, XML)

    def test_shipped_remap_targets_are_distinct_from_sources(self):
        for old, new in remap.REMAP.items():
            self.assertNotEqual(old, new)
            self.assertNotIn(new, remap.REMAP, "chained remaps are not supported")


if __name__ == "__main__":
    unittest.main()
