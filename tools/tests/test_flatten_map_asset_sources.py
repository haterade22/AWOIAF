#!/usr/bin/env python3
"""Unit tests for tools/awoiaf_map/flatten_map_asset_sources.py (synthetic trees, no game install).

Run:  python -m unittest tools.tests.test_flatten_map_asset_sources

Pins the consolidation contract for <module>/AssetSources/MapAssets:
  - end state is exactly two folders: icons/ (every .fbx, name case kept) and textures/ (every texture,
    filename LOWERCASED); no loose files, no other folders
  - byte-identical duplicates collapse to one file; differing files with the same name keep the
    in-place (or shallowest) one and suffix the others _alt/_alt2 — never silently dropped
  - desktop.ini is deleted; archives move up to AssetSources/; any other file type is a hard error
  - the plan is fully validated before the first write; dry-run writes nothing; --apply is idempotent
"""
import io
import os
import sys
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "awoiaf_map"))

import flatten_map_asset_sources as fl  # noqa: E402


def make_tree(root: Path) -> Path:
    mod = root / "AWOIAF_Map"
    ma = mod / "AssetSources" / "MapAssets"
    def w(rel, data=b"x"):
        p = ma / rel
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_bytes(data)
    w("icons/vale/TheVale.fbx", b"vale")
    w("icons/TheVale.fbx", b"vale")                        # identical duplicate, already in place
    w("icons/reach/TheReach.fbx", b"reach-small")
    w("icons/TheReach.fbx", b"reach-big")                  # DIFFERENT -> keep in place, other -> _alt
    w("icons/casterly_rock/casterly rock.fbx", b"cr")
    w("icons/casterly_rock/CasterlyRock.007_D.png", b"d")
    w("icons/casterly_rock/desktop.ini", b"[.ShellClassInfo]")
    w("textures/grassmix/Ground037_4K-PNG_Color.png", b"g")
    w("textures/Ground037_4K-PNG_Color.png", b"g")         # identical duplicate, case differs from target
    w("textures/water/Water_N.dds", b"w")
    w("textures/ADODTerrainTexturesForIcons.zip", b"zip")
    w("the_wall.fbx", b"wall")
    w("the_wall_d.png", b"walld")
    w("desktop.ini", b"[.ShellClassInfo]")
    (ma / "icons" / "empty_dir").mkdir()
    return mod


class PlanTests(unittest.TestCase):
    def test_plan_routes_and_lowercases(self):
        with tempfile.TemporaryDirectory() as tmp:
            mod = make_tree(Path(tmp))
            plan = fl.build_plan(mod / "AssetSources" / "MapAssets")
            moves = {str(s.relative_to(mod / "AssetSources" / "MapAssets")).replace("\\", "/"): d
                     for s, d in plan.moves}
            self.assertEqual(moves["icons/casterly_rock/casterly rock.fbx"], "icons/casterly rock.fbx")
            self.assertEqual(moves["icons/casterly_rock/CasterlyRock.007_D.png"], "textures/casterlyrock.007_d.png")
            self.assertEqual(moves["textures/water/Water_N.dds"], "textures/water_n.dds")
            self.assertEqual(moves["textures/Ground037_4K-PNG_Color.png"], "textures/ground037_4k-png_color.png")
            self.assertEqual(moves["the_wall.fbx"], "icons/the_wall.fbx")
            self.assertEqual(moves["the_wall_d.png"], "textures/the_wall_d.png")
            self.assertEqual(moves["icons/reach/TheReach.fbx"], "icons/TheReach_alt.fbx")
            self.assertNotIn("icons/TheReach.fbx", moves)                      # in place, untouched
            self.assertNotIn("icons/TheVale.fbx", moves)
            deleted = {str(p.relative_to(mod / "AssetSources" / "MapAssets")).replace("\\", "/") for p in plan.deletes}
            self.assertEqual(deleted, {"icons/vale/TheVale.fbx", "textures/grassmix/Ground037_4K-PNG_Color.png",
                                       "icons/casterly_rock/desktop.ini", "desktop.ini"})
            self.assertEqual([p.name for p in plan.archives_out], ["ADODTerrainTexturesForIcons.zip"])
            self.assertEqual([(a.name, b) for a, b in plan.conflicts], [("TheReach.fbx", "icons/TheReach_alt.fbx")])

    def test_unknown_file_type_is_a_hard_error(self):
        with tempfile.TemporaryDirectory() as tmp:
            mod = make_tree(Path(tmp))
            (mod / "AssetSources" / "MapAssets" / "icons" / "notes.txt").write_bytes(b"?")
            with self.assertRaises(SystemExit):
                fl.build_plan(mod / "AssetSources" / "MapAssets")

    def test_three_way_differing_conflict_gets_numbered_suffixes(self):
        with tempfile.TemporaryDirectory() as tmp:
            ma = Path(tmp) / "MapAssets"
            for rel, data in (("icons/A.fbx", b"1"), ("icons/x/A.fbx", b"2"), ("icons/y/z/A.fbx", b"3")):
                (ma / rel).parent.mkdir(parents=True, exist_ok=True)
                (ma / rel).write_bytes(data)
            plan = fl.build_plan(ma)
            targets = sorted(d for _s, d in plan.moves)
            self.assertEqual(targets, ["icons/A_alt.fbx", "icons/A_alt2.fbx"])


class ApplyTests(unittest.TestCase):
    def test_dry_run_writes_nothing(self):
        with tempfile.TemporaryDirectory() as tmp:
            mod = make_tree(Path(tmp))
            before = sorted(str(p.relative_to(mod)) for p in mod.rglob("*"))
            buf = io.StringIO()
            with redirect_stdout(buf):
                fl.main(["--module-dir", str(mod)])
            after = sorted(str(p.relative_to(mod)) for p in mod.rglob("*"))
            self.assertEqual(before, after)
            self.assertIn("DRY-RUN", buf.getvalue())

    def test_apply_reaches_two_folder_end_state_and_is_idempotent(self):
        with tempfile.TemporaryDirectory() as tmp:
            mod = make_tree(Path(tmp))
            ma = mod / "AssetSources" / "MapAssets"
            with redirect_stdout(io.StringIO()):
                fl.main(["--module-dir", str(mod), "--apply"])
            self.assertEqual(sorted(p.name for p in ma.iterdir()), ["icons", "textures"])
            self.assertTrue(all(p.is_dir() for p in ma.iterdir()))
            icons = sorted(p.name for p in (ma / "icons").iterdir())
            self.assertEqual(icons, ["TheReach.fbx", "TheReach_alt.fbx", "TheVale.fbx", "casterly rock.fbx", "the_wall.fbx"])
            self.assertEqual((ma / "icons" / "TheReach.fbx").read_bytes(), b"reach-big")
            self.assertEqual((ma / "icons" / "TheReach_alt.fbx").read_bytes(), b"reach-small")
            textures = sorted(p.name for p in (ma / "textures").iterdir())
            self.assertEqual(textures, ["casterlyrock.007_d.png", "ground037_4k-png_color.png", "the_wall_d.png", "water_n.dds"])
            self.assertTrue(all(n == n.lower() for n in textures))
            self.assertTrue((mod / "AssetSources" / "ADODTerrainTexturesForIcons.zip").exists())
            self.assertFalse(list(ma.rglob("desktop.ini")))
            snapshot = sorted((str(p.relative_to(mod)), p.read_bytes()) for p in mod.rglob("*") if p.is_file())
            buf = io.StringIO()
            with redirect_stdout(buf):
                fl.main(["--module-dir", str(mod), "--apply"])
            again = sorted((str(p.relative_to(mod)), p.read_bytes()) for p in mod.rglob("*") if p.is_file())
            self.assertEqual(snapshot, again)
            self.assertIn("nothing to do", buf.getvalue())

    def test_missing_mapassets_is_an_error(self):
        with tempfile.TemporaryDirectory() as tmp:
            with redirect_stdout(io.StringIO()):
                with self.assertRaises(SystemExit):
                    fl.main(["--module-dir", tmp, "--apply"])


if __name__ == "__main__":
    unittest.main()
