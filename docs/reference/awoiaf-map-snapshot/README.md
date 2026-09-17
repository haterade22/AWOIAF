# AWOIAF_Map module snapshot

Reference copy of the manifest DOTS writes into the external **AWOIAF_Map** campaign-map module
(`<game>/Modules/AWOIAF_Map/`, Id `AWOIAF_Map`). **Not registered or loaded by DOTS** — storage only.

## Why this exists

The map module lives outside the repo (it is ~9 GB of scene + asset data seeded from
`E:\LOTRAOMAssets\A Dance of Dragons - Map` by `tools/awoiaf_map/create_module.py`). Its `SubModule.xml`
is the one file DOTS authors; the Jul 14 2026 edit to the ADOD copy lived only in that external folder,
so this snapshot is the safety net for the same mistake (the LOTRLOME-armory-snapshot precedent).

## Files

| File | Purpose |
|---|---|
| `SubModule.xml` | The generated manifest: v1.5.3 vanilla-launcher form (`DependedModules/DependedModule`, DOTS as `Optional="true"`), single `Settlements` XmlNode, no SubModules. |

## How to regenerate / restore

The tool renders it; do not hand-edit here. To recreate the whole module:

```powershell
python tools/awoiaf_map/create_module.py          # dry-run
python tools/awoiaf_map/create_module.py --apply
```

To restore only the manifest into an existing module:

```bash
cp docs/reference/awoiaf-map-snapshot/SubModule.xml \
   "E:/Steam/steamapps/common/Mount & Blade II Bannerlord/Modules/AWOIAF_Map/SubModule.xml"
```
