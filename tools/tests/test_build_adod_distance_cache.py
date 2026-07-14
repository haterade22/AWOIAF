#!/usr/bin/env python3
"""Unit tests for tools/build_adod_distance_cache.py (synthetic data, no game install).

Run:  python -m unittest discover -s tools/tests -p "test_*.py"
  or: python tools/tests/test_build_adod_distance_cache.py

Pins the load-bearing transcoder invariants from the 2026-07-14 deep review:
  - the .NET 7-bit varint string codec (incl. multi-byte lengths >= 128)
  - the 1e30 unreachable sentinel really exceeds the UNREACHABLE threshold
  - write_modern/parse_modern round-trip, canonical-order + duplicate rejection,
    and the bit-exact distance sampling added for finding B.7
  - Gabriel-graph neighbor semantics (blocking, unreachable exclusion,
    nearest-neighbor edge guarantee)
"""
import io
import os
import struct
import sys
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import build_adod_distance_cache as b  # noqa: E402


class VarintTests(unittest.TestCase):
    def test_round_trip_lengths_across_continuation_boundaries(self):
        for n in (0, 1, 127, 128, 129, 300, 16383, 16384):
            s = "x" * n
            out = io.BytesIO()
            b.write_7bit_string(out, s)
            got, pos = b.read_7bit_string(out.getvalue(), 0)
            self.assertEqual(got, s, f"length {n}")
            self.assertEqual(pos, len(out.getvalue()), f"length {n}: leftover bytes")


class SentinelTests(unittest.TestCase):
    def test_legacy_1e30_bit_pattern_exceeds_threshold(self):
        (v,) = struct.unpack("<f", bytes.fromhex("caf24971"))
        self.assertGreaterEqual(v, b.UNREACHABLE)
        self.assertLess(4163.69, b.UNREACHABLE)  # sane real distances stay below it


class RoundTripTests(unittest.TestCase):
    def _pairs(self):
        # canonical: outer is the ordinal-smaller id
        return {"a_town": {"b_town": 10.5, "c_town": 20.25}, "b_town": {"c_town": 5.125}}

    def test_write_then_parse_counts_and_bitexact_sampling(self):
        with tempfile.TemporaryDirectory() as tmp:
            p = Path(tmp) / "cache.bin"
            edges = {"a_town": {"b_town"}, "b_town": {"a_town"}}
            faces = [(0, "a_town"), (7, "c_town")]
            b.write_modern(p, self._pairs(), edges, faces)
            flat = {(o, i): d for o, inner in self._pairs().items() for i, d in inner.items()}
            npairs, ncount, fcount = b.parse_modern(p, expect=flat, sample_every=1)
            self.assertEqual((npairs, ncount, fcount), (3, 2, 2))

    def test_parse_rejects_noncanonical_order(self):
        with tempfile.TemporaryDirectory() as tmp:
            p = Path(tmp) / "cache.bin"
            b.write_modern(p, {"z_town": {"a_town": 1.0}}, {}, [])
            with self.assertRaises(SystemExit):
                b.parse_modern(p)

    def test_parse_detects_distance_drift(self):
        with tempfile.TemporaryDirectory() as tmp:
            p = Path(tmp) / "cache.bin"
            b.write_modern(p, {"a_town": {"b_town": 10.5}}, {}, [])
            with self.assertRaises(SystemExit):
                b.parse_modern(p, expect={("a_town", "b_town"): 99.0}, sample_every=1)

    def test_header_is_two_zero_uint32(self):
        with tempfile.TemporaryDirectory() as tmp:
            p = Path(tmp) / "cache.bin"
            b.write_modern(p, {}, {}, [])
            self.assertEqual(p.read_bytes()[:8], b"\x00" * 8)


class GabrielTests(unittest.TestCase):
    def test_blocking_and_nearest_neighbor_guarantee(self):
        # Collinear forts A - B - C: A-C is blocked by B (d(A,B)^2 + d(B,C)^2 < d(A,C)^2).
        dist = {("fa", "fb"): 10.0, ("fb", "fc"): 10.0, ("fa", "fc"): 20.0}
        edges = b.gabriel_neighbors({"fa", "fb", "fc"}, dist)
        self.assertIn("fb", edges["fa"])
        self.assertIn("fc", edges["fb"])
        self.assertNotIn("fc", edges["fa"])
        # every reachable fort has >=1 edge (nearest-neighbor edges are Gabriel edges)
        for f in ("fa", "fb", "fc"):
            self.assertGreaterEqual(len(edges[f]), 1)

    def test_unreachable_fort_omitted_entirely(self):
        dist = {("fa", "fb"): 10.0, ("fa", "fx"): 1e30, ("fb", "fx"): 1e30}
        edges = b.gabriel_neighbors({"fa", "fb", "fx"}, dist)
        self.assertNotIn("fx", edges)  # absent, never present-with-empty-list
        self.assertIn("fb", edges["fa"])


if __name__ == "__main__":
    unittest.main()
