#!/usr/bin/env python3
"""Audit that every faction reference in the AWOIAF_Map (ADOD-derived) settlements.xml resolves against
DOTS + vanilla definitions, and that the generated faction layer is internally consistent.

Checks (read-only; exit 1 on any failure):
  A. every map culture="Culture.X"  -> defined in SandBoxCore/SandBox spcultures,
     dots_spcultures.xml, or dots_adod_cultures.xml
  B. every map owner="Faction.X"    -> defined in SandBox spclans.xml or characters/clans.xml
  C. every DOTS clan owner="Hero.X" -> has BOTH a <Hero id=X faction=...> row (heroes.xml)
     and an <NPCCharacter id=X> (lords.xml); hero faction == the clan
  D. every DOTS clan culture / super_faction / initial_home_settlement resolves
     (kingdoms: vanilla spkingdoms + dots_spkingdoms; settlements: map + vanilla ids)
  E. every DOTS kingdom owner hero's clan has super_faction == that kingdom
     (kingdom ruler must belong to the kingdom); kingdom culture + home resolve

Run: python tools/audit_adod_map_refs.py
"""
import os
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MD = ROOT / "Main" / "_Module" / "ModuleData"
GAME = Path(os.environ.get("BANNERLORD_GAME_DIR",
                           r"E:\Steam\steamapps\common\Mount & Blade II Bannerlord"))
MAP_XML = GAME / "Modules" / "AWOIAF_Map" / "ModuleData" / "settlements.xml"  # Id AWOIAF_Map, seeded from ADOD
SB = GAME / "Modules" / "SandBox" / "ModuleData"
SBC = GAME / "Modules" / "SandBoxCore" / "ModuleData"

errors = []


def err(msg):
    errors.append(msg)


def ids_of(path, tag, attr="id"):
    if not path.exists():
        return {}
    return {e.get(attr): e for e in ET.parse(path).getroot().iter(tag) if e.get(attr)}


XSL_NS = "{http://www.w3.org/1999/XSL/Transform}"


def parse_spclans_xslt_overrides(path):
    """clan id -> {attr: value} from spclans.xslt attribute-override templates. The audit
    can't run a real XSLT engine, but the generated transform only ever overrides whole
    attributes, so the effective post-transform value of culture/super_faction is statically
    recoverable. Without this, vanilla-clan retargets were invisible to check E — exactly
    how the Kingdom.freefolk/clan_sturgia_5 mismatch shipped (deep-review DF-1, 2026-07-14)."""
    overrides = {}
    if not path.exists():
        return overrides
    for tmpl in ET.parse(path).getroot().findall(f"{XSL_NS}template"):
        m = re.match(r"Faction\[@id='([^']+)'\]/@(\w+)", tmpl.get("match") or "")
        if not m:
            continue
        attr_el = tmpl.find(f"{XSL_NS}attribute")
        if attr_el is not None:
            overrides.setdefault(m.group(1), {})[m.group(2)] = attr_el.text or ""
    return overrides


def main():
    map_txt = MAP_XML.read_text(encoding="utf-8", errors="replace")

    cultures = set(ids_of(SBC / "spcultures.xml", "Culture")) \
        | set(ids_of(SB / "spcultures.xml", "Culture")) \
        | set(ids_of(MD / "dots_spcultures.xml", "Culture")) \
        | set(ids_of(MD / "dots_adod_cultures.xml", "Culture"))
    van_clans = ids_of(SB / "spclans.xml", "Faction")
    dots_clans = ids_of(MD / "characters" / "clans.xml", "Faction")
    clans = {**van_clans, **dots_clans}
    van_kingdoms = ids_of(SB / "spkingdoms.xml", "Kingdom")
    dots_kingdoms = ids_of(MD / "dots_spkingdoms.xml", "Kingdom")
    kingdoms = {**van_kingdoms, **dots_kingdoms}
    heroes = ids_of(MD / "characters" / "heroes.xml", "Hero")
    lords = ids_of(MD / "characters" / "lords.xml", "NPCCharacter")
    van_lords = ids_of(SB / "lords.xml", "NPCCharacter")
    van_heroes = ids_of(SB / "heroes.xml", "Hero")

    map_settlements = set(re.findall(r'<Settlement id="([^"]+)"', map_txt))
    van_settlements = set()
    van_set_file = SB / "settlements.xml"
    if van_set_file.exists():
        van_settlements = set(re.findall(r'<Settlement id="([^"]+)"',
                                         van_set_file.read_text(encoding="utf-8", errors="replace")))
    settlements = map_settlements | van_settlements

    dup = set(van_clans) & set(dots_clans)
    if dup:
        err(f"clans defined in BOTH vanilla and DOTS (duplicate ids): {sorted(dup)}")

    # A. map cultures
    for c in sorted({m.replace("Culture.", "") for m in re.findall(r'culture="(Culture\.[^"]+)"', map_txt)}):
        if c not in cultures:
            err(f"[A] map culture not defined anywhere: {c!r}")

    # B. map owners
    for o in sorted({m.replace("Faction.", "") for m in re.findall(r'owner="(Faction\.[^"]+)"', map_txt)}):
        if o not in clans:
            err(f"[B] map owner clan not defined anywhere: {o!r}")

    # C+D. DOTS clans internal consistency
    for cid, el in dots_clans.items():
        owner = (el.get("owner") or "").replace("Hero.", "")
        if not owner:
            err(f"[C] clan {cid}: no owner hero")
        else:
            if owner not in heroes:
                err(f"[C] clan {cid}: owner {owner} has no <Hero> row in heroes.xml")
            elif (heroes[owner].get("faction") or "") != f"Faction.{cid}":
                err(f"[C] clan {cid}: hero {owner} faction is {heroes[owner].get('faction')!r}, expected Faction.{cid}")
            if owner not in lords:
                err(f"[C] clan {cid}: owner {owner} has no <NPCCharacter> in lords.xml")
        cul = (el.get("culture") or "").replace("Culture.", "")
        if cul not in cultures:
            err(f"[D] clan {cid}: culture {cul!r} undefined")
        sf = (el.get("super_faction") or "").replace("Kingdom.", "")
        if sf and sf not in kingdoms:
            err(f"[D] clan {cid}: super_faction {sf!r} undefined")
        if not sf:
            err(f"[D] clan {cid}: fief-owning clan without super_faction")
        home = (el.get("initial_home_settlement") or "").replace("Settlement.", "")
        if home not in settlements:
            err(f"[D] clan {cid}: initial_home_settlement {home!r} not on the map (or vanilla)")

    # heroes.xml rows must all point at defined clans + defined lords
    for hid, el in heroes.items():
        fac = (el.get("faction") or "").replace("Faction.", "")
        if fac not in clans:
            err(f"[C] hero {hid}: faction {fac!r} undefined")
        if hid not in lords and hid not in van_lords:
            err(f"[C] hero {hid}: no NPCCharacter definition")

    # E. Kingdoms — DOTS-defined AND spkingdoms.xslt-retargeted vanilla ones. Vanilla clans'
    # post-transform culture/super_faction come from the statically-recovered spclans.xslt
    # overrides, so ruling-clan consistency is enforced for ALL kingdoms.
    clan_overrides = parse_spclans_xslt_overrides(MD / "spclans.xslt")

    def effective(clan_id, attr, prefix):
        ov = clan_overrides.get(clan_id, {}).get(attr)
        if ov is not None:
            return ov.replace(prefix, "")
        el = clans.get(clan_id)
        return (el.get(attr) or "").replace(prefix, "") if el is not None else ""

    all_heroes = {**van_heroes, **heroes}
    hero_clan = {h: (el.get("faction") or "").replace("Faction.", "")
                 for h, el in all_heroes.items()}
    kingdom_rulers = {kid: (el.get("owner") or "").replace("Hero.", "")
                      for kid, el in dots_kingdoms.items()}
    # vanilla kingdoms' post-transform owners from spkingdoms.xslt
    for tmpl in (ET.parse(MD / "spkingdoms.xslt").getroot().findall(f"{XSL_NS}template")
                 if (MD / "spkingdoms.xslt").exists() else []):
        m = re.match(r"Kingdom\[@id='([^']+)'\]/@owner", tmpl.get("match") or "")
        if m:
            attr_el = tmpl.find(f"{XSL_NS}attribute")
            if attr_el is not None:
                kingdom_rulers[m.group(1)] = (attr_el.text or "").replace("Hero.", "")

    for kid, el in dots_kingdoms.items():
        cul = (el.get("culture") or "").replace("Culture.", "")
        if cul not in cultures:
            err(f"[E] kingdom {kid}: culture {cul!r} undefined")
        home = (el.get("initial_home_settlement") or "").replace("Settlement.", "")
        if home not in settlements:
            err(f"[E] kingdom {kid}: home {home!r} not a settlement")

    for kid, owner in kingdom_rulers.items():
        if owner not in all_heroes:
            err(f"[E] kingdom {kid}: owner hero {owner!r} has no Hero row (vanilla or DOTS)")
            continue
        oc = hero_clan.get(owner)
        if not oc:
            err(f"[E] kingdom {kid}: owner hero {owner} has no faction attr")
            continue
        if oc not in clans:
            err(f"[E] kingdom {kid}: owner hero {owner} clan {oc!r} undefined")
            continue
        sf = effective(oc, "super_faction", "Kingdom.")
        if sf != kid:
            err(f"[E] kingdom {kid}: ruling clan {oc} effective super_faction is {sf!r}")

    print(f"loaded: {len(cultures)} cultures, {len(van_clans)} vanilla clans, "
          f"{len(van_heroes)} vanilla heroes, {len(van_lords)} vanilla lords, "
          f"{len(clan_overrides)} spclans.xslt clan retargets, "
          f"{len(kingdom_rulers)} kingdoms ruler-checked")
    if errors:
        print(f"FAIL — {len(errors)} problems:")
        for e in errors:
            print("  " + e)
        sys.exit(1)
    print(f"OK — map cultures/owners resolve; {len(dots_clans)} DOTS clans, "
          f"{len(dots_kingdoms)} DOTS kingdoms, {len(heroes)} heroes, {len(lords)} lords consistent.")


if __name__ == "__main__":
    main()
