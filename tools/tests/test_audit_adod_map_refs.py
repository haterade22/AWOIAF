#!/usr/bin/env python3
"""Unit tests for tools/audit_adod_map_refs.py helpers (synthetic data, no game install).

Run:  python -m unittest discover -s tools/tests -p "test_*.py"
  or: python tools/tests/test_audit_adod_map_refs.py

Pins the 2026-07-14 deep-review fix DF-1: the audit must statically recover
culture/super_faction retargets from spclans.xslt so vanilla ruling clans are
kingdom-consistency-checked (the Kingdom.freefolk/clan_sturgia_5 blind spot).
"""
import os
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import audit_adod_map_refs as a  # noqa: E402

XSLT = """<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
\t<xsl:output omit-xml-declaration="no" indent="yes"/>
\t<xsl:template match="@*|node()">
\t\t<xsl:copy><xsl:apply-templates select="@*|node()"/></xsl:copy>
\t</xsl:template>
\t<xsl:template match="Faction[@id='clan_sturgia_5']/@culture">
\t\t<xsl:attribute name="culture">Culture.freefolk</xsl:attribute>
\t</xsl:template>
\t<xsl:template match="Faction[@id='clan_sturgia_5']/@super_faction">
\t\t<xsl:attribute name="super_faction">Kingdom.freefolk</xsl:attribute>
\t</xsl:template>
\t<xsl:template match="Faction[@id='clan_battania_1']/@name">
\t\t<xsl:attribute name="name">{=k}House Stark</xsl:attribute>
\t</xsl:template>
</xsl:stylesheet>
"""


class XsltOverrideParserTests(unittest.TestCase):
    def test_recovers_attribute_overrides_per_clan(self):
        with tempfile.TemporaryDirectory() as tmp:
            p = Path(tmp) / "spclans.xslt"
            p.write_text(XSLT, encoding="utf-8")
            ov = a.parse_spclans_xslt_overrides(p)
        self.assertEqual(ov["clan_sturgia_5"]["culture"], "Culture.freefolk")
        self.assertEqual(ov["clan_sturgia_5"]["super_faction"], "Kingdom.freefolk")
        self.assertEqual(ov["clan_battania_1"]["name"], "{=k}House Stark")
        self.assertNotIn("tier", ov.get("clan_battania_1", {}))

    def test_missing_file_returns_empty(self):
        self.assertEqual(a.parse_spclans_xslt_overrides(Path("does/not/exist.xslt")), {})


class IdsOfTests(unittest.TestCase):
    def test_ids_of_reads_and_missing_file_is_empty(self):
        with tempfile.TemporaryDirectory() as tmp:
            p = Path(tmp) / "clans.xml"
            p.write_text('<Factions><Faction id="c1" culture="Culture.x"/>'
                         '<Faction id="c2"/></Factions>', encoding="utf-8")
            got = a.ids_of(p, "Faction")
            self.assertEqual(set(got), {"c1", "c2"})
            self.assertEqual(got["c1"].get("culture"), "Culture.x")
        self.assertEqual(a.ids_of(Path(tmp) / "gone.xml", "Faction"), {})


if __name__ == "__main__":
    unittest.main()
