#!/usr/bin/env python3
"""Unit tests for tools/awoiaf_map/migrate_settlements_schema.py (synthetic XML, no game install).

Run:  python -m unittest tools.tests.test_migrate_settlements_schema

Pins the two v1.5.3 schema migrations applied to the ADOD (1.2.12-era) settlements.xml:
  T1  hideouts: <Hideout scene_name="X"> (never read on 1.5.3) becomes a
      <Locations complex_template="LocationComplexTemplate.hideout_complex"><Location id="hideout_center"
      scene_name="X"/></Locations> block, which HideoutCampaignBehavior / the map tooltip dereference
  T2  menu meshes: background_mesh / wait_mesh values absent from the installed vanilla set are remapped
      through an explicit table; any unmapped custom value is a hard error (never silently kept)
and the tool contract: byte-faithful I/O (BOM + CRLF), untouched lines stay byte-identical, idempotent,
dry-run writes nothing, --apply writes only after every post-condition passes.
"""
import io
import os
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET
from contextlib import redirect_stdout
from pathlib import Path

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "awoiaf_map"))

import migrate_settlements_schema as ms  # noqa: E402

VANILLA_MESHES = {"menu_empire_1", "menu_empire_2", "wait_empire_town", "gui_bg_town_sturgia",
                  "wait_sturgia_town", "gui_bg_castle_vlanda", "gui_bg_village_vlanda",
                  "wait_vlandia_village", "empire_twn_scene_bg", "wait_hideout_desert"}

SAMPLE = (
    '<?xml version="1.0" encoding="utf-8"?>\r\n'
    '<Settlements>\r\n'
    '  <Settlement id="town_B1" name="Winterfell" owner="Faction.clan_x" posX="1" posY="2" culture="Culture.battania">\r\n'
    '    <Components>\r\n'
    '      <Town id="town_comp_B1" is_castle="false" level="3" background_crop_position="0.0" background_mesh="menuWinterfell" wait_mesh="menuWinterfell" gate_rotation="0.5" prosperity="2600" />\r\n'
    '    </Components>\r\n'
    '    <Locations complex_template="LocationComplexTemplate.town_complex">\r\n'
    '      <Location id="center" scene_name="sturgia_town_c" />\r\n'
    '    </Locations>\r\n'
    '  </Settlement>\r\n'
    '  <Settlement id="town_A1" name="Plain" owner="Faction.clan_y" posX="3" posY="4" culture="Culture.empire">\r\n'
    '    <Components>\r\n'
    '      <Town id="town_comp_A1" is_castle="false" level="1" background_crop_position="0.0" background_mesh="menu_empire_2" wait_mesh="wait_empire_town" gate_rotation="0.1" prosperity="1000" />\r\n'
    '    </Components>\r\n'
    '  </Settlement>\r\n'
    '  <Settlement id="village_A1_1" name="Vil" posX="5" posY="6" culture="Culture.empire">\r\n'
    '    <Components>\r\n'
    '      <Village id="village_comp_A1_1" village_type="VillageType.vineyard" gate_rotation="0.5" bound="Settlement.town_A1" background_crop_position="0.0" background_mesh="gui_bg_village_vlanda" wait_mesh="wait_vlandia_village" castle_background_mesh="gui_bg_castle_vlanda" hearth="400" />\r\n'
    '    </Components>\r\n'
    '  </Settlement>\r\n'
    '  <Settlement id="hideout_desert_1" name="Hideout" type="Hideout" posX="7" posY="8" culture="Culture.desert_bandits">\r\n'
    '    <Components>\r\n'
    '      <Hideout id="hideout_desert_1" map_icon="bandit_hideout_a" scene_name="desert_hideout_002" background_crop_position="0.0" background_mesh="empire_twn_scene_bg" wait_mesh="wait_hideout_desert" gate_rotation="0.0" />\r\n'
    '    </Components>\r\n'
    '  </Settlement>\r\n'
    '  <Settlement id="hideout_desert_2" name="Hideout" type="Hideout" posX="9" posY="10" culture="Culture.desert_bandits">\r\n'
    '    <Components>\r\n'
    '      <Hideout id="hideout_desert_2" map_icon="bandit_hideout_a" scene_name="desert_hideout_003" background_crop_position="0.0" background_mesh="empire_twn_scene_bg" wait_mesh="wait_hideout_desert" gate_rotation="0.0" />\r\n'
    '    </Components>\r\n'
    '  </Settlement>\r\n'
    '</Settlements>\r\n'
)
SAMPLE_BYTES = b"\xef\xbb\xbf" + SAMPLE.encode("utf-8")


def migrate(text=SAMPLE, meshes=VANILLA_MESHES, remap=None):
    remap = {"menuWinterfell": ("gui_bg_town_sturgia", "wait_sturgia_town")} if remap is None else remap
    return ms.migrate(text, vanilla_meshes=meshes, mesh_remap=remap)


class HideoutMigrationTests(unittest.TestCase):
    def test_every_hideout_gains_a_hideout_center_location(self):
        out, report = migrate()
        root = ET.fromstring(out)
        hideouts = [s for s in root.findall("Settlement") if s.find("Components/Hideout") is not None]
        self.assertEqual(len(hideouts), 2)
        for s in hideouts:
            locs = s.findall("Locations")
            self.assertEqual(len(locs), 1, s.get("id"))
            self.assertEqual(locs[0].get("complex_template"), "LocationComplexTemplate.hideout_complex")
            loc = locs[0].findall("Location")
            self.assertEqual(len(loc), 1)
            self.assertEqual(loc[0].get("id"), "hideout_center")
            self.assertIsNone(s.find("Components/Hideout").get("scene_name"),
                              "the 1.2.12 attribute is dead on 1.5.3 and must not survive as a decoy")
        by_id = {s.get("id"): s for s in hideouts}
        self.assertEqual(by_id["hideout_desert_1"].find("Locations/Location").get("scene_name"), "desert_hideout_002")
        self.assertEqual(by_id["hideout_desert_2"].find("Locations/Location").get("scene_name"), "desert_hideout_003")
        self.assertEqual(report["hideouts_migrated"], 2)

    def test_inserted_block_matches_file_indentation_and_crlf(self):
        out, _ = migrate()
        self.assertIn(
            '    </Components>\r\n'
            '    <Locations complex_template="LocationComplexTemplate.hideout_complex">\r\n'
            '      <Location id="hideout_center" scene_name="desert_hideout_002" />\r\n'
            '    </Locations>\r\n'
            '  </Settlement>\r\n', out)

    def test_hideout_without_scene_name_is_a_hard_error(self):
        broken = SAMPLE.replace(' scene_name="desert_hideout_003"', '')
        with self.assertRaises(SystemExit):
            migrate(broken)


class MeshRemapTests(unittest.TestCase):
    def test_custom_meshes_are_remapped_only_where_they_occur(self):
        out, report = migrate()
        root = ET.fromstring(out)
        town = root.find("Settlement[@id='town_B1']/Components/Town")
        self.assertEqual(town.get("background_mesh"), "gui_bg_town_sturgia")
        self.assertEqual(town.get("wait_mesh"), "wait_sturgia_town")
        plain = root.find("Settlement[@id='town_A1']/Components/Town")
        self.assertEqual((plain.get("background_mesh"), plain.get("wait_mesh")), ("menu_empire_2", "wait_empire_town"))
        self.assertEqual(report["meshes_remapped"], {"town_B1": ("menuWinterfell", "gui_bg_town_sturgia", "wait_sturgia_town")})

    def test_unmapped_custom_mesh_is_a_hard_error(self):
        with self.assertRaises(SystemExit):
            migrate(remap={})

    def test_remap_target_must_itself_be_vanilla(self):
        with self.assertRaises(SystemExit):
            migrate(remap={"menuWinterfell": ("menuWinterfellHD", "wait_sturgia_town")})


class ContractTests(unittest.TestCase):
    def test_untouched_lines_are_byte_identical_and_count_is_bounded(self):
        out, _ = migrate()
        src_lines = SAMPLE.split("\r\n")
        out_lines = out.split("\r\n")
        # 2 hideouts x 3 inserted lines; 2 hideout tags + 1 town tag edited in place
        self.assertEqual(len(out_lines), len(src_lines) + 6)
        changed = [l for l in src_lines if l not in out_lines]
        self.assertEqual(len(changed), 3)
        self.assertEqual(out.count("\n"), out.count("\r\n"))

    def test_idempotent(self):
        once, _ = migrate()
        twice, report = migrate(once)
        self.assertEqual(once, twice)
        self.assertEqual(report["hideouts_migrated"], 0)
        self.assertEqual(report["meshes_remapped"], {})

    def test_settlement_count_and_ids_preserved(self):
        out, _ = migrate()
        a = [s.get("id") for s in ET.fromstring(SAMPLE).findall("Settlement")]
        b = [s.get("id") for s in ET.fromstring(out).findall("Settlement")]
        self.assertEqual(a, b)


class MainTests(unittest.TestCase):
    def _setup(self, tmp):
        xml = Path(tmp) / "settlements.xml"
        xml.write_bytes(SAMPLE_BYTES)
        van = Path(tmp) / "vanilla_settlements.xml"
        van.write_bytes(("<Settlements>" + "".join(
            f'<Settlement><Components><Town background_mesh="{m}" wait_mesh="{m}" castle_background_mesh="{m}"/></Components></Settlement>'
            for m in VANILLA_MESHES) + "</Settlements>").encode("utf-8"))
        return xml, van

    def test_dry_run_writes_nothing(self):
        with tempfile.TemporaryDirectory() as tmp:
            xml, van = self._setup(tmp)
            buf = io.StringIO()
            with redirect_stdout(buf):
                ms.main(["--xml", str(xml), "--vanilla", str(van)])
            self.assertEqual(xml.read_bytes(), SAMPLE_BYTES)
            self.assertIn("DRY-RUN", buf.getvalue())
            self.assertIn("hideouts_migrated=2", buf.getvalue())

    def test_apply_writes_bom_crlf_and_is_idempotent(self):
        with tempfile.TemporaryDirectory() as tmp:
            xml, van = self._setup(tmp)
            with redirect_stdout(io.StringIO()):
                ms.main(["--xml", str(xml), "--vanilla", str(van), "--apply"])
            raw = xml.read_bytes()
            self.assertTrue(raw.startswith(b"\xef\xbb\xbf"))
            self.assertIn(b'<Location id="hideout_center" scene_name="desert_hideout_002" />\r\n', raw)
            self.assertNotIn(b"menuWinterfell", raw)
            self.assertEqual(raw.count(b"\n"), raw.count(b"\r\n"))
            with redirect_stdout(io.StringIO()):
                ms.main(["--xml", str(xml), "--vanilla", str(van), "--apply"])
            self.assertEqual(xml.read_bytes(), raw)

    def test_apply_leaves_no_temp_file_on_failure(self):
        with tempfile.TemporaryDirectory() as tmp:
            xml, van = self._setup(tmp)
            xml.write_bytes(SAMPLE_BYTES.replace(b' scene_name="desert_hideout_003"', b''))
            with redirect_stdout(io.StringIO()):
                with self.assertRaises(SystemExit):
                    ms.main(["--xml", str(xml), "--vanilla", str(van), "--apply"])
            self.assertEqual(sorted(p.name for p in Path(tmp).iterdir()), ["settlements.xml", "vanilla_settlements.xml"])


if __name__ == "__main__":
    unittest.main()
