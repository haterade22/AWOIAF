#!/usr/bin/env python3
"""Unit tests for tools/generate_adod_factions.py (synthetic data, no game install).

Run:  python -m unittest discover -s tools/tests -p "test_*.py"
  or: python tools/tests/test_generate_adod_factions.py

Each test pins a deep-review finding from 2026-07-14 so it can't regress:
  - H1   main_hero / skill-less shells must never be picked as lord templates
  - DF-1 SPEC culture/kingdom overrides must win over fief-derived values
  - A.2  the overwrite guard must refuse hand-edited files structurally
  - A.5a name-pool exhaustion must fail loud (SystemExit, not IndexError)
  - A.5b duplicate ruler names must be rejected (SPEC) / re-drawn (pool)
"""
import os
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import generate_adod_factions as g  # noqa: E402


def spec_stub(**kw):
    spec = {
        "culture_to_kingdom": {"battania": "battania", "freefolk": "freefolk"},
        "kingdoms": {
            "battania": {"vanilla": True, "name": "The North", "ruling_clan": "clan_a",
                         "color": "0xFF111111", "color2": "0xFF222222"},
            "freefolk": {"name": "The Free Folk", "ruling_clan": "clan_b",
                         "color": "0xFF333333", "color2": "0xFF444444"},
        },
        "clans": {},
        "name_pools": {
            "battania": {"house_template": "House {}", "atoms": ["Moss", "Bole"],
                         "male": ["Torrhen", "Brandon"]},
            "freefolk": {"house_template": "The {} Clan", "atoms": ["Icefang"],
                         "male": ["Bael"]},
        },
        "wars": [],
    }
    spec.update(kw)
    return spec


def fiefs(culture, n=1):
    return [{"type": "town", "name": f"T{i}", "culture": culture,
             "prosperity": 1000, "id": f"town_x{i}"} for i in range(n)]


class DeriveTests(unittest.TestCase):
    def test_dominant_culture_drives_kingdom(self):
        plan = g.derive({"clan_a": fiefs("battania", 3)}, spec_stub(), {})
        self.assertEqual(plan["clan_a"]["culture"], "battania")
        self.assertEqual(plan["clan_a"]["kingdom"], "battania")

    def test_spec_culture_and_kingdom_override_win(self):
        # DF-1 (the Thenns): fiefs say battania, SPEC says freefolk — SPEC must win.
        spec = spec_stub()
        spec["clans"]["clan_a"] = {"culture": "freefolk", "kingdom": "freefolk"}
        plan = g.derive({"clan_a": fiefs("battania", 3)}, spec, {})
        self.assertEqual(plan["clan_a"]["culture"], "freefolk")
        self.assertEqual(plan["clan_a"]["kingdom"], "freefolk")

    def test_spec_tier_marks_tier_is_spec(self):
        spec = spec_stub()
        spec["clans"]["clan_a"] = {"tier": 6}
        plan = g.derive({"clan_a": fiefs("battania")}, spec, {})
        self.assertEqual(plan["clan_a"]["tier"], 6)
        self.assertTrue(plan["clan_a"]["tier_is_spec"])
        self.assertFalse(g.derive({"clan_a": fiefs("battania")}, spec_stub(), {})
                         ["clan_a"]["tier_is_spec"])

    def test_duplicate_spec_clan_name_raises(self):
        spec = spec_stub()
        spec["clans"] = {"clan_a": {"name": "House Same"}, "clan_b": {"name": "House Same"}}
        with self.assertRaises(SystemExit):
            g.derive({"clan_a": fiefs("battania"), "clan_b": fiefs("battania")}, spec, {})

    def test_duplicate_spec_ruler_name_raises(self):
        spec = spec_stub()
        spec["clans"] = {"clan_a": {"ruler_name": "Torrhen"}, "clan_b": {"ruler_name": "Torrhen"}}
        with self.assertRaises(SystemExit):
            g.derive({"clan_a": fiefs("battania"), "clan_b": fiefs("battania")}, spec, {})

    def test_pool_ruler_names_deduplicate_by_redraw(self):
        spec = spec_stub()
        spec["clans"]["clan_a"] = {"ruler_name": "Torrhen"}  # reserves pool name #1
        plan = g.derive({"clan_a": fiefs("battania"), "clan_b": fiefs("battania")}, spec, {})
        self.assertNotEqual(plan["clan_a"]["ruler_name"], plan["clan_b"]["ruler_name"])

    def test_pool_exhaustion_is_systemexit_not_indexerror(self):
        # 1 atom x (9 suffixes + base) = 10 usable house names; the 11th draw must fail loud.
        spec = spec_stub()
        owners = {f"clan_ff_{i:02d}": fiefs("freefolk") for i in range(11)}
        spec["kingdoms"]["freefolk"]["ruling_clan"] = "clan_ff_00"
        spec["name_pools"]["freefolk"]["male"] = [f"M{i}" for i in range(20)]
        with self.assertRaises(SystemExit) as cm:
            g.derive(owners, spec, {})
        self.assertIn("exhausted", str(cm.exception))


class LordTemplateTests(unittest.TestCase):
    def _write(self, tmp, body):
        p = Path(tmp) / "lords.xml"
        p.write_text(f"<NPCCharacters>{body}</NPCCharacters>", encoding="utf-8")
        return p

    def test_main_hero_and_skillless_shells_are_never_picked(self):
        # H1: main_hero is occupation=Lord + is_hero but has no skill_template — must be skipped.
        body = (
            '<NPCCharacter id="main_hero" occupation="Lord" is_hero="true" culture="Culture.battania"/>'
            '<NPCCharacter id="shell_1" occupation="Lord" is_hero="true" culture="Culture.battania"/>'
            '<NPCCharacter id="lord_ok" occupation="Lord" is_hero="true" culture="Culture.battania"'
            ' skill_template="SkillSet.spc_x"/>'
        )
        with tempfile.TemporaryDirectory() as tmp:
            tmpl = g.parse_lord_templates(self._write(tmp, body))
        self.assertEqual(tmpl["battania"]["m"].get("id"), "lord_ok")

    def test_non_ruler_preferred_over_ruler(self):
        body = (
            '<NPCCharacter id="king" occupation="Lord" is_hero="true" culture="Culture.battania"'
            ' skill_template="SkillSet.spc_politician_skills_ruler"/>'
            '<NPCCharacter id="vassal" occupation="Lord" is_hero="true" culture="Culture.battania"'
            ' skill_template="SkillSet.spc_knight_skills"/>'
        )
        with tempfile.TemporaryDirectory() as tmp:
            tmpl = g.parse_lord_templates(self._write(tmp, body))
        self.assertEqual(tmpl["battania"]["m"].get("id"), "vassal")


class GuardOverwriteTests(unittest.TestCase):
    def test_marker_file_and_empty_placeholder_and_identity_stub_pass(self):
        with tempfile.TemporaryDirectory() as tmp:
            marked = Path(tmp) / "a.xml"
            marked.write_text(f"<Factions><!--{g.MARKER}--><Faction id='x'/></Factions>",
                              encoding="utf-8")
            g.guard_overwrite(marked)  # no raise
            empty = Path(tmp) / "b.xml"
            empty.write_text("<Kingdoms/>", encoding="utf-8")
            g.guard_overwrite(empty)  # no raise
            stub = Path(tmp) / "c.xslt"
            stub.write_text(
                '<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">'
                '<xsl:template match="@*|node()"><xsl:copy/></xsl:template></xsl:stylesheet>',
                encoding="utf-8")
            g.guard_overwrite(stub)  # no raise
            missing = Path(tmp) / "d.xml"
            g.guard_overwrite(missing)  # no raise

    def test_small_hand_edited_xml_refused(self):
        # A.2: the old heuristic silently clobbered hand-edited files under 400 bytes.
        with tempfile.TemporaryDirectory() as tmp:
            p = Path(tmp) / "clans.xml"
            p.write_text('<Factions><Faction id="hand_made"/></Factions>', encoding="utf-8")
            with self.assertRaises(SystemExit):
                g.guard_overwrite(p)

    def test_hand_edited_xslt_with_overrides_refused(self):
        with tempfile.TemporaryDirectory() as tmp:
            p = Path(tmp) / "spclans.xslt"
            p.write_text(
                '<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">'
                '<xsl:template match="@*|node()"><xsl:copy/></xsl:template>'
                "<xsl:template match=\"Faction[@id='x']/@name\"><xsl:attribute name='name'>Y'"
                "</xsl:attribute></xsl:template></xsl:stylesheet>", encoding="utf-8")
            with self.assertRaises(SystemExit):
                g.guard_overwrite(p)


class HelperTests(unittest.TestCase):
    def test_slug_ascii_folding_and_specials(self):
        self.assertEqual(g.slug("Basilisk Corsair"), "basilisk_corsair")
        self.assertEqual(g.slug("N'ghai"), "n_ghai")
        self.assertEqual(g.slug("Yi-Tish"), "yi_tish")

    def test_det_is_deterministic(self):
        self.assertEqual(g.det("clan_x", 25), g.det("clan_x", 25))
        self.assertTrue(0 <= g.det("clan_x", 25) < 25)

    def test_strip0x(self):
        self.assertEqual(g.strip0x("0xFF1c2A3b"), "FF1C2A3B")
        self.assertEqual(g.strip0x("FF1C2A3B"), "FF1C2A3B")


if __name__ == "__main__":
    unittest.main()
