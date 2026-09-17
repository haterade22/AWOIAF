#!/usr/bin/env python3
"""Build the modern-format settlement distance cache for the ADOD Westeros map — OFFLINE,
before ever loading into the game (no editor, no campaign required).

WHY THIS EXISTS
  The map module ships `ModuleData/settlements_distance_cache.bin` in the LEGACY pre-1.4
  format at the LEGACY path. The 1.4.x engine only probes
  `<Module>/ModuleData/DistanceCaches/settlements_distance_cache_<NavType>.bin`
  (SandBox.View SettlementPositionScript.GetSettlementsDistanceCacheFileForCapability) and its
  reader (`NavigationCache<T>.Deserialize`) has no legacy branch — the shipped bin is dead
  weight. Worse: the module scan is LAST-ACTIVE-MODULE-WINS, so with no ADOD-map bin the
  engine deserializes SandBox's (or NavalDLC's) *Calradia* cache against the *Westeros*
  settlement set -> unknown-id NRE inside a swallowed catch -> no cache registered ->
  NullReferenceException on the loading screen (DefaultMapDistanceModel.GetDistance,
  first hit by Campaign.CalculateAverageDistanceBetweenTowns). A campaign on this map
  CANNOT load until a correct modern bin exists — which is why the in-game MCM rebuild
  (Main/Features/EditorCacheRebuild, requires a live campaign) can't bootstrap it.

  Fortunately the legacy bin is internally complete and valid (verified byte-level: full
  1,562-settlement half-matrix, 1,219,141 pairs, exact id match with settlements.xml), and
  the modern reader ignores the scene-CRC header in shipping builds. So the legacy data can
  be TRANSCODED to the modern grammar entirely offline.

WHAT IT WRITES (--apply)
  <map module>/ModuleData/DistanceCaches/settlements_distance_cache_Default.bin
  - header: two zero uint32 CRCs (read-and-discarded by the shipping reader; the wEditor
    build only logs a soft assert on mismatch)
  - distances: all legacy pairs verbatim (incl. the 1e30 "unreachable" sentinels — safer
    than the 0f the engine's lazy fallback would memoize), re-grouped so every pair's outer
    element is the ordinal-smaller StringId (NavigationCacheElement.Sort canonical order —
    REQUIRED: Deserialize re-Sorts by ref and a non-canonical outer corrupts its loop var)
  - fortification neighbors: Gabriel-graph approximation over towns+castles from the
    path-distance matrix (density-matched to vanilla's ~4.3 avg degree; see
    gabriel_neighbors). Vanilla's semantic is path-walk-based and needs the navmesh; the
    proxy is sane, guarantees each reachable fortification >=1 neighbor, and unreachable
    fortifications are OMITTED (absent entries do NOT trigger
    NavigationCache.FinalizeCacheInitialization's zero-neighbor full regen — only
    present-with-empty-list entries do). The first in-game MCM rebuild replaces this
    approximation with the exact path-walk computation.
  - closest-settlement-per-navmesh-face: carried over from the legacy bin verbatim.

  Default run is a DRY-RUN: builds + validates to the scratch dir, writes nothing to the
  game install. `--apply` writes into the map module (existing file backed up to .prev).

NAVALDLC WARNING
  If NavalDLC is ENABLED in the launcher, SettlementPositionScript demands _Naval and _All
  caches too (useNavalNavigation is true for any non-Sandbox map when NavalDLC is active)
  and will pick NavalDLC's Calradia bins -> same crash. Disable NavalDLC for DOTS play
  until DOTS's naval cache support lands (#120). This tool emits the land (Default) cache only.

Decompile evidence (installed v1.4.7, via dots-src + ilspycmd on Modules/SandBox/bin):
  NavigationCache`1.Serialize/Deserialize (grammar), NavigationCacheElement`1.Sort
  (StringComparison.Ordinal, ties by IsPortUsed), FinalizeCacheInitialization (zero-neighbor
  regen trigger), SettlementPositionScript.ReadNavigationCacheForNavigationTypeOnGameLoad
  (last-module-wins scan), SandBoxNavigationCache.GetRealDistanceAndLandRatioBetweenSettlements.

Run:  python tools/build_adod_distance_cache.py            # dry-run + validation report
      python tools/build_adod_distance_cache.py --apply    # write into the map module
"""
import argparse
import io
import os
import re
import struct
import sys
from collections import defaultdict
from pathlib import Path

GAME = Path(os.environ.get("BANNERLORD_GAME_DIR",
                           r"E:\Steam\steamapps\common\Mount & Blade II Bannerlord"))
MAP_MODULE = GAME / "Modules" / "AWOIAF_Map"  # DOTS map module (Id AWOIAF_Map), seeded from the ADOD map
LEGACY_BIN = MAP_MODULE / "ModuleData" / "settlements_distance_cache.bin"
SETTLEMENTS_XML = MAP_MODULE / "ModuleData" / "settlements.xml"
OUT_DIR = MAP_MODULE / "ModuleData" / "DistanceCaches"
OUT_BIN = OUT_DIR / "settlements_distance_cache_Default.bin"

UNREACHABLE = 9.9e29  # legacy sentinel is 1e30 (0x7149F2CA); treat >= this as unreachable


# ---------------------------------------------------------------- .NET binary primitives

def read_7bit_string(buf, pos):
    length = 0
    shift = 0
    while True:
        b = buf[pos]
        pos += 1
        length |= (b & 0x7F) << shift
        if not (b & 0x80):
            break
        shift += 7
    s = buf[pos:pos + length].decode("utf-8")
    return s, pos + length


def write_7bit_string(out, s):
    data = s.encode("utf-8")
    n = len(data)
    while n >= 0x80:
        out.write(bytes([(n & 0x7F) | 0x80]))
        n >>= 7
    out.write(bytes([n]))
    out.write(data)


# ---------------------------------------------------------------- legacy parser

def parse_legacy(path):
    """Legacy grammar (verified round-trip to EOF):
       int32 N; for i in 0..N-1: { string outerId; for j in i+1..N-1: { string innerId; float dist } }
       then repeat { int32 faceIndex; string settlementId } until faceIndex == -1 (last 4 bytes)."""
    buf = path.read_bytes()
    pos = 0
    (n,) = struct.unpack_from("<i", buf, pos)
    pos += 4
    ids = []
    pairs = []  # (idA, idB, dist) in file order
    for i in range(n):
        outer, pos = read_7bit_string(buf, pos)
        ids.append(outer)
        for _ in range(n - 1 - i):
            inner, pos = read_7bit_string(buf, pos)
            (d,) = struct.unpack_from("<f", buf, pos)
            pos += 4
            pairs.append((outer, inner, d))
    faces = []
    while True:
        (face,) = struct.unpack_from("<i", buf, pos)
        pos += 4
        if face == -1:
            break
        sid, pos = read_7bit_string(buf, pos)
        faces.append((face, sid))
    if pos != len(buf):
        raise SystemExit(f"legacy parse error: {len(buf) - pos} leftover bytes")
    return ids, pairs, faces


# ---------------------------------------------------------------- settlements.xml

def parse_settlements(path):
    txt = path.read_text(encoding="utf-8", errors="replace")
    all_ids = re.findall(r'<Settlement id="([^"]+)"', txt)
    forts = set()
    for m in re.finditer(r'<Settlement id="([^"]+)"[^>]*>\s*<Components>\s*<Town\b', txt):
        forts.add(m.group(1))
    return set(all_ids), forts


# ---------------------------------------------------------------- neighbor approximation

def gabriel_neighbors(forts, dist):
    """Gabriel-graph approximation over fortifications: A-B is an edge iff reachable and no
    fortification C satisfies d(A,C)^2 + d(C,B)^2 < d(A,B)^2 (C inside the diametral ball,
    measured in path-distance). Chosen over the sparser relative-neighborhood graph because
    its density (~4 avg degree) matches vanilla's path-walk neighbor graph (~4.3 avg degree
    on Calradia: 486 directed rows / ~112 fortifications) far better than RNG's ~2.3.
    Guarantees every reachable fortification >=1 edge (nearest-neighbor edges are always
    Gabriel edges). O(F^3) worst case with early exit; F=~520 is fine offline."""
    fl = sorted(forts)
    def d(a, b):
        if a == b:
            return 0.0
        key = (a, b) if a < b else (b, a)
        return dist.get(key, float("inf"))
    edges = defaultdict(set)
    for i, a in enumerate(fl):
        for b in fl[i + 1:]:
            dab = d(a, b)
            if dab >= UNREACHABLE or dab == float("inf"):
                continue
            dab2 = dab * dab
            blocked = False
            for c in fl:
                if c == a or c == b:
                    continue
                dac = d(a, c)
                if dac >= dab:
                    continue
                dcb = d(c, b)
                if dac * dac + dcb * dcb < dab2:
                    blocked = True
                    break
            if not blocked:
                edges[a].add(b)
                edges[b].add(a)
    return edges


# ---------------------------------------------------------------- modern writer

def write_modern(path, pair_map, neighbors, faces):
    out = io.BytesIO()
    out.write(struct.pack("<II", 0, 0))  # scene CRCs: read-and-discarded by shipping reader
    outers = sorted(pair_map)
    out.write(struct.pack("<i", len(outers)))
    for outer in outers:
        write_7bit_string(out, outer)
        out.write(b"\x00")  # IsPortUsed=false (land cache, map defines no ports)
        inner = pair_map[outer]
        out.write(struct.pack("<i", len(inner)))
        for iid in sorted(inner):
            write_7bit_string(out, iid)
            out.write(b"\x00")
            out.write(struct.pack("<f", inner[iid]))
    directed = [(k, n) for k in sorted(neighbors) for n in sorted(neighbors[k])]
    out.write(struct.pack("<i", len(directed)))
    for k, n2 in directed:
        write_7bit_string(out, k)
        write_7bit_string(out, n2)
    out.write(struct.pack("<i", len(faces)))
    for face, sid in faces:
        out.write(struct.pack("<i", face))
        write_7bit_string(out, sid)
        out.write(b"\x00")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(out.getvalue())


def parse_modern(path, expect=None, sample_every=500):
    """Independent re-parse of the modern grammar (NavigationCache`1.Deserialize, Default nav:
    no landRatio floats) — validates the written file round-trips to EOF. When `expect`
    (the canonical {(outer, inner): dist} map) is given, every `sample_every`-th pair is
    also compared BIT-EXACT against the source data, so a writer regression can't ship
    structurally-valid-but-wrong distances (deep-review B.7)."""
    buf = path.read_bytes()
    pos = 8  # skip CRC header
    (outer_count,) = struct.unpack_from("<i", buf, pos)
    pos += 4
    npairs = 0
    seen = set()
    for _ in range(outer_count):
        outer, pos = read_7bit_string(buf, pos)
        pos += 1
        (inner_count,) = struct.unpack_from("<i", buf, pos)
        pos += 4
        for _ in range(inner_count):
            inner, pos = read_7bit_string(buf, pos)
            pos += 1
            (d,) = struct.unpack_from("<f", buf, pos)
            pos += 4
            if not outer < inner:
                raise SystemExit(f"canonical-order violation: outer {outer!r} !< inner {inner!r}")
            key = (outer, inner)
            if key in seen:
                raise SystemExit(f"duplicate pair {key}")
            seen.add(key)
            if expect is not None and npairs % sample_every == 0:
                want = expect.get(key)
                if want is None or struct.pack("<f", want) != struct.pack("<f", d):
                    raise SystemExit(f"distance drift at {key}: wrote {d!r}, source {want!r}")
            npairs += 1
    (ncount,) = struct.unpack_from("<i", buf, pos)
    pos += 4
    for _ in range(ncount):
        _, pos = read_7bit_string(buf, pos)
        _, pos = read_7bit_string(buf, pos)
    (fcount,) = struct.unpack_from("<i", buf, pos)
    pos += 4
    for _ in range(fcount):
        pos += 4
        _, pos = read_7bit_string(buf, pos)
        pos += 1
    if pos != len(buf):
        raise SystemExit(f"modern re-parse: {len(buf) - pos} leftover bytes")
    return npairs, ncount, fcount


# ---------------------------------------------------------------- main

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true",
                    help="write into the map module (default: dry-run to scratch + validate)")
    args = ap.parse_args()

    for p in (LEGACY_BIN, SETTLEMENTS_XML):
        if not p.exists():
            raise SystemExit(f"missing input: {p}")

    print(f"parsing legacy bin ({LEGACY_BIN.stat().st_size:,} bytes)...")
    ids, pairs, faces = parse_legacy(LEGACY_BIN)
    xml_ids, forts = parse_settlements(SETTLEMENTS_XML)

    # --- input validation
    if set(ids) != xml_ids:
        missing = sorted(xml_ids - set(ids))[:10]
        extra = sorted(set(ids) - xml_ids)[:10]
        raise SystemExit(f"id mismatch vs settlements.xml: missing={missing} extra={extra}")
    n = len(ids)
    expected_pairs = n * (n - 1) // 2
    if len(pairs) != expected_pairs:
        raise SystemExit(f"pair count {len(pairs):,} != n(n-1)/2 = {expected_pairs:,}")
    non_ascii = [s for s in ids if any(ord(c) > 127 for c in s)]
    if non_ascii:
        raise SystemExit(f"non-ASCII settlement ids break ordinal-order assumptions: {non_ascii[:5]}")
    bad_faces = [sid for _, sid in faces if sid not in xml_ids]
    if bad_faces:
        raise SystemExit(f"face section references unknown ids: {bad_faces[:5]}")

    # --- canonical regroup (outer = ordinal-smaller id, required by Deserialize's ref-Sort)
    pair_map = defaultdict(dict)   # outer -> {inner: dist}
    dist = {}
    for a, b, d in pairs:
        lo, hi = (a, b) if a < b else (b, a)
        if hi in pair_map[lo]:
            raise SystemExit(f"duplicate pair after canonicalization: {lo}/{hi}")
        pair_map[lo][hi] = d
        dist[(lo, hi)] = d

    unreachable = {s: 0 for s in ids}
    for (lo, hi), d in dist.items():
        if d >= UNREACHABLE:
            unreachable[lo] += 1
            unreachable[hi] += 1
    isolated = sorted(s for s, c in unreachable.items() if c == n - 1)

    print(f"settlements: {n:,} (fortifications: {len(forts)})  pairs: {len(pairs):,}  "
          f"faces: {len(faces):,}")
    if isolated:
        print(f"WARNING: {len(isolated)} settlements unreachable from everywhere "
              f"(off-navmesh placements in the ADOD scene): {isolated}")

    print("computing fortification-neighbor approximation (Gabriel graph on path distances)...")
    edges = gabriel_neighbors(forts, dist)
    fort_no_edges = sorted(f for f in forts if f not in edges)
    if fort_no_edges:
        print(f"  {len(fort_no_edges)} fortifications omitted from neighbor section "
              f"(unreachable): {fort_no_edges}")
    directed_count = sum(len(v) for v in edges.values())
    print(f"  neighbor edges: {directed_count // 2} undirected ({directed_count} directed rows)")

    # Write to a temp path, validate the TEMP file (structure + sampled bit-exact distances),
    # and only then swap it live — a crash or failed validation never leaves the map module
    # without a working cache (deep-review B.1: the old rename-then-write ordering had a
    # window with no _Default.bin at the live path, and left an UNVALIDATED file live when
    # validation failed).
    scratch = Path(__file__).resolve().parent / "_adod_cache_dryrun.bin"
    target = OUT_BIN.with_suffix(".bin.tmp") if args.apply else scratch

    print(f"writing modern-format cache -> {target}")
    write_modern(target, pair_map, edges, faces)

    npairs, ncount, fcount = parse_modern(target, expect=dist)
    ok = (npairs == expected_pairs and ncount == directed_count and fcount == len(faces))
    print(f"validation re-parse: pairs={npairs:,} neighbors={ncount} faces={fcount:,} "
          f"size={target.stat().st_size:,} bytes, sampled distances bit-exact -> "
          f"{'OK' if ok else 'MISMATCH'}")
    if not ok:
        target.unlink(missing_ok=True)
        raise SystemExit(1)

    if args.apply:
        if OUT_BIN.exists():
            prev = OUT_BIN.with_suffix(".bin.prev")
            if prev.exists():
                prev.unlink()
            OUT_BIN.rename(prev)
            print(f"backed up existing cache to {prev.name}")
        target.rename(OUT_BIN)

    if not args.apply:
        scratch.unlink()
        print("\nDRY-RUN ok (scratch output validated + deleted). "
              "Run with --apply to write into the map module.")
    else:
        print(f"\nDONE: {OUT_BIN}")
        print("The campaign map will now load with correct distances (no in-game compute).")
        print("NOTE: the fortification-neighbor section is an offline approximation — run the")
        print("in-game rebuild (MCM > DOTS > Map Tools > Rebuild Now) once from a loaded")
        print("campaign whenever you want the exact path-walk neighbor data.")
        print("NOTE: if NavalDLC is ENABLED in the launcher, disable it for DOTS play — the")
        print("engine would demand _Naval/_All caches and crash on NavalDLC's Calradia bins (#120).")


if __name__ == "__main__":
    main()
