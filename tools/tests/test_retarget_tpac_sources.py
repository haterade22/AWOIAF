#!/usr/bin/env python3
"""Unit tests for tools/awoiaf_map/retarget_tpac_sources.py (synthetic tpac bytes, no game install).

Run:  python -m unittest tools.tests.test_retarget_tpac_sources

Pins the in-place patch contract for the Int32-length-prefixed "$BASE/Modules/<module>/AssetSources/..."
source-path strings inside editor-form *_tex.tpac files:
  - the replacement is EXACTLY the same byte length (module segment padded with "./" no-op components),
    so the length prefix and every byte outside the string are untouched
  - a module segment that cannot be padded to equal length (too short, or odd difference) is a hard error
  - a rewritten path that does not resolve to an existing source file is a hard error (dry-run and apply)
  - files without a "$BASE/Modules/" string are left alone; idempotent; --apply writes only after checks
"""
import io
import os
import struct
import sys
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path

sys.path.insert(0, os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "awoiaf_map"))

import retarget_tpac_sources as rt  # noqa: E402


def tpac_bytes(path_str: str, *, pre=b"\x00" * 20 + b"the_twins\x00", post=b"\x1b!X%\x18\xff-\xd0" + b"\x00" * 8) -> bytes:
    s = path_str.encode("utf-8")
    return pre + struct.pack("<i", len(s)) + s + post


class ReplacementTests(unittest.TestCase):
    def test_pads_with_dot_slash_to_equal_length(self):
        self.assertEqual(rt.equal_length_segment("A Dance of Dragons", "AWOIAF_Map"), "AWOIAF_Map/./././.")
        self.assertEqual(rt.equal_length_segment("ADOD_IAF Map", "AWOIAF_Map"), "AWOIAF_Map/.")
        self.assertEqual(rt.equal_length_segment("AWOIAF_Map", "AWOIAF_Map"), "AWOIAF_Map")

    def test_unpaddable_segments_are_errors(self):
        with self.assertRaises(SystemExit):
            rt.equal_length_segment("HART_scenes", "AWOIAF_Map")   # 11 vs 10: odd difference
        with self.assertRaises(SystemExit):
            rt.equal_length_segment("ADOD", "AWOIAF_Map")          # shorter than the target


class PatchTests(unittest.TestCase):
    def test_rewrites_in_place_same_length(self):
        old = "$BASE/Modules/A Dance of Dragons/AssetSources/MapAssets/icons/riverrun/x.png"
        data = tpac_bytes(old)
        out, paths = rt.patch_bytes(data, "AWOIAF_Map")
        self.assertEqual(len(out), len(data))
        self.assertEqual(paths, [(old, "$BASE/Modules/AWOIAF_Map/././././AssetSources/MapAssets/icons/riverrun/x.png")])
        i = data.index(b"$BASE")
        self.assertEqual(out[:i - 4], data[:i - 4])                       # bytes before the length prefix
        self.assertEqual(out[i - 4:i], data[i - 4:i])                     # length prefix unchanged
        self.assertEqual(out[i + len(old):], data[i + len(old):])         # trailer (hash etc.) unchanged
        self.assertEqual(struct.unpack("<i", out[i - 4:i])[0], len(old))

    def test_length_prefix_mismatch_is_an_error(self):
        old = "$BASE/Modules/A Dance of Dragons/AssetSources/a.png"
        data = tpac_bytes(old)
        i = data.index(b"$BASE")
        bad = data[:i - 4] + struct.pack("<i", len(old) + 3) + data[i:]
        with self.assertRaises(SystemExit):
            rt.patch_bytes(bad, "AWOIAF_Map")

    def test_no_marker_is_a_noop_and_idempotent(self):
        data = b"\x00" * 40
        self.assertEqual(rt.patch_bytes(data, "AWOIAF_Map"), (data, []))
        once, _ = rt.patch_bytes(tpac_bytes("$BASE/Modules/ADOD_IAF Map/AssetSources/a.png"), "AWOIAF_Map")
        twice, paths = rt.patch_bytes(once, "AWOIAF_Map")
        self.assertEqual(once, twice)
        self.assertEqual(paths, [])


class MainTests(unittest.TestCase):
    def _module(self, tmp: Path, with_png=True):
        mod = tmp / "Modules" / "AWOIAF_Map"
        (mod / "Assets" / "MapAssets" / "icons").mkdir(parents=True)
        if with_png:
            (mod / "AssetSources" / "MapAssets" / "icons").mkdir(parents=True)
            (mod / "AssetSources" / "MapAssets" / "icons" / "x.png").write_bytes(b"png")
        t = mod / "Assets" / "MapAssets" / "icons" / "x_tex.tpac"
        t.write_bytes(tpac_bytes("$BASE/Modules/A Dance of Dragons/AssetSources/MapAssets/icons/x.png"))
        (mod / "Assets" / "MapAssets" / "plain.tpac").write_bytes(b"\x00" * 16)
        return mod, t

    def test_dry_run_verifies_targets_and_writes_nothing(self):
        with tempfile.TemporaryDirectory() as tmp:
            mod, t = self._module(Path(tmp))
            before = t.read_bytes()
            buf = io.StringIO()
            with redirect_stdout(buf):
                rt.main(["--module-dir", str(mod), "--game-dir", tmp])
            self.assertEqual(t.read_bytes(), before)
            self.assertIn("DRY-RUN", buf.getvalue())
            self.assertIn("tpacs to patch: 1", buf.getvalue())

    def test_missing_source_file_is_an_error_before_any_write(self):
        with tempfile.TemporaryDirectory() as tmp:
            mod, t = self._module(Path(tmp), with_png=False)
            before = t.read_bytes()
            with redirect_stdout(io.StringIO()):
                with self.assertRaises(SystemExit):
                    rt.main(["--module-dir", str(mod), "--game-dir", tmp, "--apply"])
            self.assertEqual(t.read_bytes(), before)

    def test_apply_patches_and_is_idempotent(self):
        with tempfile.TemporaryDirectory() as tmp:
            mod, t = self._module(Path(tmp))
            size = t.stat().st_size
            with redirect_stdout(io.StringIO()):
                rt.main(["--module-dir", str(mod), "--game-dir", tmp, "--apply"])
            raw = t.read_bytes()
            self.assertEqual(len(raw), size)
            self.assertIn(b"$BASE/Modules/AWOIAF_Map/././././AssetSources/MapAssets/icons/x.png", raw)
            self.assertNotIn(b"A Dance of Dragons", raw)
            buf = io.StringIO()
            with redirect_stdout(buf):
                rt.main(["--module-dir", str(mod), "--game-dir", tmp, "--apply"])
            self.assertEqual(t.read_bytes(), raw)
            self.assertIn("tpacs to patch: 0", buf.getvalue())


if __name__ == "__main__":
    unittest.main()
