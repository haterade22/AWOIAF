#!/usr/bin/env python3
"""Unit tests for tools/awoiaf_map/fix_terrain_texture_case.py."""
import os
import sys
import unittest

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "awoiaf_map"))

import fix_terrain_texture_case as ft  # noqa: E402

SCENE = (
    '<scene name="Main_map">\r\n'
    '  <game_entity name="Grass003_4K-PNG_Color_entity"/>\r\n'
    '  <terrain enabled="true">\r\n'
    '    <layer name="grass"><summer name="grass"><textures>\r\n'
    '      <texture type="diffusemap" name="Grass003_4K-PNG_Color"/>\r\n'
    '      <texture type="areamap" name="none"/>\r\n'
    '      <texture type="normalmap" name="ground_grass_soil_a_n"/>\r\n'
    '      <texture type="specularmap" name=""/>\r\n'
    '      <texture type="heightmap" name="NotOurs_H"/>\r\n'
    '    </textures></summer></layer>\r\n'
    '  </terrain>\r\n'
    '</scene>\r\n'
)
TEXTURES = {"grass003_4k-png_color", "ground_grass_soil_a_n"}


class FixTests(unittest.TestCase):
    def test_only_terrain_refs_with_a_lowercase_twin_change(self):
        out, changes = ft.fix_scene_text(SCENE, TEXTURES)
        self.assertEqual(changes, [("Grass003_4K-PNG_Color", "grass003_4k-png_color")])
        self.assertIn('name="grass003_4k-png_color"', out)
        self.assertIn('name="Grass003_4K-PNG_Color_entity"', out)   # outside <terrain>: untouched
        self.assertIn('name="NotOurs_H"', out)                       # no lowercase twin: untouched
        self.assertIn('name="ground_grass_soil_a_n"', out)
        self.assertEqual(len(out), len(SCENE))
        self.assertEqual(out.count("\r\n"), SCENE.count("\r\n"))

    def test_idempotent(self):
        once, _ = ft.fix_scene_text(SCENE, TEXTURES)
        twice, changes = ft.fix_scene_text(once, TEXTURES)
        self.assertEqual(once, twice)
        self.assertEqual(changes, [])

    def test_no_terrain_block_is_an_error(self):
        with self.assertRaises(SystemExit):
            ft.fix_scene_text("<scene/>", TEXTURES)


if __name__ == "__main__":
    unittest.main()
