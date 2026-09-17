#!/usr/bin/env python3
"""Migrate the AWOIAF_Map settlements.xml (ADOD 1.2.12-era schema) to what Bannerlord v1.5.3 reads.

Decompile evidence (installed v1.5.3 via dots-src; see docs/features/awoiaf-map.md):
  - Settlement/Town/Village/Hideout.Deserialize still read every attribute ADOD emits (gate_posX/Y, text,
    prosperity, hearth, max_prosperity are optional-or-required exactly as ADOD provides them). No attribute
    needs stripping.
  - Hideout.Deserialize reads ONLY the three mesh attributes. The 1.2.12 `<Hideout scene_name="X">` is dead;
    the hideout scene now lives in the LocationComplex:
        <Locations complex_template="LocationComplexTemplate.hideout_complex">
          <Location id="hideout_center" scene_name="X" />
        </Locations>
    HideoutCampaignBehavior (line 666) and the campaign-map tooltip refresher (TooltipRefresherCollection
    line 925) dereference Settlement.LocationComplex -> NRE for every ADOD hideout without it.   [T1]
  - background_mesh / wait_mesh are engine GUI meshes. Six ADOD hero-city values (menuWinterfell,
    menuKingsLanding, ...) exist in no installed tpac; they are remapped to vanilla placeholders through
    MESH_REMAP. Any custom value NOT in the table is a hard error — never silently kept.          [T2]

Contract: byte-faithful (BOM + CRLF preserved; untouched lines byte-identical), idempotent, staged in
memory, every post-condition checked BEFORE the single atomic write (temp + os.replace).

Usage:
    python tools/awoiaf_map/migrate_settlements_schema.py            # DRY-RUN: report only
    python tools/awoiaf_map/migrate_settlements_schema.py --apply
    ... [--xml PATH] [--vanilla PATH]   (defaults: the AWOIAF_Map module / installed SandBox)
"""
from __future__ import annotations

import argparse
import os
import re
import xml.etree.ElementTree as ET
from pathlib import Path

GAME = Path(os.environ.get("BANNERLORD_GAME_DIR",
                           r"E:\Steam\steamapps\common\Mount & Blade II Bannerlord"))
DEFAULT_XML = GAME / "Modules" / "AWOIAF_Map" / "ModuleData" / "settlements.xml"
DEFAULT_VANILLA = GAME / "Modules" / "SandBox" / "ModuleData" / "settlements.xml"

# custom mesh value -> (vanilla background_mesh, vanilla wait_mesh). Aesthetic placeholders only —
# restore the ADOD hero-city menus once their tpacs are ported into the module.
MESH_REMAP: dict[str, tuple[str, str]] = {
    "menuStormsEnd": ("menu_empire_seaside_1", "wait_empire_town"),   # Storm's End
    "menuEyrie": ("gui_bg_town_battania", "wait_battania_town"),      # The Eyrie
    "menuWinterfell": ("gui_bg_town_sturgia", "wait_sturgia_town"),   # Winterfell
    "battania_town_3": ("gui_bg_town_battania", "wait_battania_town"),  # The Dreadfort (bg only)
    "menuKingsLanding": ("menu_empire_1", "wait_empire_town"),        # King's Landing
    "menuDragonstone": ("menu_empire_seaside_2", "wait_empire_town"),  # Dragonstone
}

MESH_ATTRS = ("background_mesh", "wait_mesh", "castle_background_mesh")
_COMPONENT_TAG = re.compile(r"<(Town|Village|Hideout)\b")
_SETTLEMENT_ID = re.compile(r'<Settlement\b[^>]*\bid="([^"]+)"')
_SCENE_ATTR = re.compile(r'\s+scene_name="([^"]*)"')


def vanilla_mesh_set(vanilla_xml_text: str) -> set[str]:
    out: set[str] = set()
    for m in re.finditer(r"<(?:Town|Village|Hideout)\b([^>]*)>", vanilla_xml_text):
        for a in MESH_ATTRS:
            v = re.search(r'\b%s\s*=\s*"([^"]*)"' % a, m.group(1))
            if v:
                out.add(v.group(1))
    return out


def _remap_meshes(line: str, sid: str, vanilla: set[str], remap: dict[str, tuple[str, str]],
                  report: dict) -> str:
    def sub(attr: str, idx: int) -> None:
        nonlocal line
        m = re.search(r'\b%s\s*=\s*"([^"]*)"' % attr, line)
        if not m or m.group(1) in vanilla:
            return
        old = m.group(1)
        if old not in remap:
            raise SystemExit(f"{sid}: {attr}=\"{old}\" is not a vanilla mesh and has no MESH_REMAP entry")
        new = remap[old][idx]
        if new not in vanilla:
            raise SystemExit(f"{sid}: MESH_REMAP target \"{new}\" is not itself a vanilla mesh")
        line = line[:m.start(1)] + new + line[m.end(1):]
        entry = report["meshes_remapped"].setdefault(sid, [old, None, None])
        entry[1 + idx] = new

    sub("background_mesh", 0)
    sub("wait_mesh", 1)
    sub("castle_background_mesh", 0)
    if sid in report["meshes_remapped"]:
        e = report["meshes_remapped"][sid]
        # record the FINAL values so the report reads (old_bg, new_bg, new_wait) even if only one changed
        bg = re.search(r'\bbackground_mesh\s*=\s*"([^"]*)"', line)
        wt = re.search(r'\bwait_mesh\s*=\s*"([^"]*)"', line)
        report["meshes_remapped"][sid] = (e[0], bg.group(1) if bg else None, wt.group(1) if wt else None)
    return line


def migrate(text: str, *, vanilla_meshes: set[str], mesh_remap: dict[str, tuple[str, str]] = MESH_REMAP):
    """Return (migrated_text, report). Raises SystemExit on any contract violation."""
    nl = "\r\n" if "\r\n" in text else "\n"
    src_lines = text.split(nl)
    out: list[str] = []
    report = {"hideouts_migrated": 0, "hideout_ids": [], "meshes_remapped": {}}

    sid = ""
    pending_scene: str | None = None  # hideout scene awaiting insertion after </Components>
    i = 0
    while i < len(src_lines):
        line = src_lines[i]
        m = _SETTLEMENT_ID.search(line)
        if m:
            sid = m.group(1)
            pending_scene = None
        if _COMPONENT_TAG.search(line):
            line = _remap_meshes(line, sid, vanilla_meshes, mesh_remap, report)
        if "<Hideout" in line:
            sm = _SCENE_ATTR.search(line)
            # does this settlement already carry a <Locations> block?
            j = i + 1
            has_locations = False
            while j < len(src_lines) and "</Settlement>" not in src_lines[j]:
                if "<Locations" in src_lines[j]:
                    has_locations = True
                j += 1
            if sm:
                if has_locations:
                    raise SystemExit(f"{sid}: <Hideout scene_name> AND a <Locations> block — ambiguous, fix by hand")
                pending_scene = sm.group(1)
                line = line[:sm.start()] + line[sm.end():]
            elif not has_locations:
                raise SystemExit(f"{sid}: hideout has neither scene_name nor a <Locations> block")
        out.append(line)
        if pending_scene is not None and "</Components>" in line:
            indent = line[:len(line) - len(line.lstrip())]
            out.append(f'{indent}<Locations complex_template="LocationComplexTemplate.hideout_complex">')
            out.append(f'{indent}  <Location id="hideout_center" scene_name="{pending_scene}" />')
            out.append(f"{indent}</Locations>")
            report["hideouts_migrated"] += 1
            report["hideout_ids"].append(sid)
            pending_scene = None
        i += 1

    result = nl.join(out)
    _check(text, result, nl, report, vanilla_meshes)
    return result, report


def _check(src: str, result: str, nl: str, report: dict, vanilla: set[str]) -> None:
    try:
        before = ET.fromstring(src)
        after = ET.fromstring(result)
    except ET.ParseError as e:
        raise SystemExit(f"post-check: result is not well-formed XML: {e}")
    ids_b = [s.get("id") for s in before.findall("Settlement")]
    ids_a = [s.get("id") for s in after.findall("Settlement")]
    if ids_a != ids_b:
        raise SystemExit("post-check: settlement id sequence changed")
    for s in after.findall("Settlement"):
        h = s.find("Components/Hideout")
        if h is None:
            continue
        if h.get("scene_name") is not None:
            raise SystemExit(f"post-check: {s.get('id')} still has <Hideout scene_name>")
        locs = s.findall("Locations")
        if len(locs) != 1 or locs[0].get("complex_template") != "LocationComplexTemplate.hideout_complex":
            raise SystemExit(f"post-check: {s.get('id')} lacks exactly one hideout_complex <Locations>")
        centers = [l for l in locs[0].findall("Location") if l.get("id") == "hideout_center"]
        if len(centers) != 1 or not centers[0].get("scene_name"):
            raise SystemExit(f"post-check: {s.get('id')} lacks a hideout_center scene_name")
    for comp in after.iter():
        if comp.tag in ("Town", "Village", "Hideout"):
            for a in MESH_ATTRS:
                v = comp.get(a)
                if v is not None and v not in vanilla:
                    raise SystemExit(f"post-check: non-vanilla {a}=\"{v}\" survived")
    src_lines, out_lines = src.split(nl), result.split(nl)
    if len(out_lines) != len(src_lines) + 3 * report["hideouts_migrated"]:
        raise SystemExit("post-check: line count is not source + 3 per migrated hideout")
    out_set = set(out_lines)
    removed = [l for l in src_lines if l not in out_set]
    edited_settlements = set(report["hideout_ids"]) | set(report["meshes_remapped"])
    if len(removed) != len(edited_settlements):
        raise SystemExit(f"post-check: {len(removed)} source lines changed, expected {len(edited_settlements)}")


def main(argv: list[str] | None = None) -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--xml", type=Path, default=DEFAULT_XML)
    ap.add_argument("--vanilla", type=Path, default=DEFAULT_VANILLA)
    ap.add_argument("--apply", action="store_true", help="write in place (default: dry-run, writes nothing)")
    args = ap.parse_args(argv)

    for p in (args.xml, args.vanilla):
        if not p.is_file():
            raise SystemExit(f"missing input: {p}")
    raw = args.xml.read_bytes()
    had_bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw.decode("utf-8-sig")
    vanilla = vanilla_mesh_set(args.vanilla.read_bytes().decode("utf-8-sig"))

    result, report = migrate(text, vanilla_meshes=vanilla)
    mode = "APPLY" if args.apply else "DRY-RUN"
    print(f"[{mode}] {args.xml}")
    print(f"[{mode}] vanilla mesh set: {len(vanilla)} values from {args.vanilla}")
    print(f"[{mode}] hideouts_migrated={report['hideouts_migrated']}")
    for sid, (old, bg, wt) in sorted(report["meshes_remapped"].items()):
        print(f"[{mode}] mesh remap {sid}: {old} -> background_mesh={bg} wait_mesh={wt}")
    if result == text:
        print(f"[{mode}] nothing to do (already migrated)")
        return
    if not args.apply:
        print("[DRY-RUN] nothing written. Re-run with --apply.")
        return
    tmp = args.xml.with_suffix(args.xml.suffix + ".tmp")
    tmp.write_bytes((b"\xef\xbb\xbf" if had_bom else b"") + result.encode("utf-8"))
    os.replace(tmp, args.xml)
    print(f"[APPLY] wrote {args.xml}")


if __name__ == "__main__":
    main()
