# clan_heraldry

Per-clan heraldry + region-themed party rosters, consumed by the tooling
(`tools/build_clan_specs.py`, `tools/generate_clan_heraldry.py`) that generates the heraldry colours
and party-template rosters baked into `spclans.xslt`. These JSONs are **tool inputs**, not runtime
data — no `Main/**/*.cs` reads this directory.

## Status (2026-06-13 — GoT re-skin)

The 21 LOTR culture files (`gondor.json`, `mordor.json`, …) were **removed**: they referenced clans,
troops, and party templates that were zeroed at the bootstrap, and nothing consumes them.

GoT heraldry files are authored **per region/culture** when their dependencies exist — i.e. after the
Westeros clans (`clan_<region>_N` = the great houses, per
[ADR-011](../../../../docs/adrs/011-westeros-culture-model.md)) and their troop trees are authored
(conversion Phases A–C). Each file will follow the prior schema:

```json
{
  "culture": "north",
  "clans": [
    { "id": "clan_north_1", "theme": "House Stark — grey & white",
      "color": "FF...", "color2": "FF...",
      "roster": [ { "troop": "north_winterfell_levy", "min": 3, "max": 6 } ] }
  ]
}
```
