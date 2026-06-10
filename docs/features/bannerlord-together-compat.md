# BannerlordTogether Compatibility

## Overview

DOTS supports passive plug-and-play co-op via [BannerlordTogether (BT)](https://www.nexusmods.com/mountandblade2bannerlord/mods/10426) v0.2.2+. Both players host and join as normal — DOTS's factions, cultures, races, troops, and diplomacy constraints all function in co-op with no additional configuration.

## Requirements

| Requirement | Details |
|-------------|---------|
| Bannerlord version | **1.4.5** (DOTS's target; BT's stated minimum is 1.3.15 — co-op at 1.4.5 has not been re-verified since DOTS's v1.4.5 migration) |
| BannerlordTogether | v0.2.2+ |
| DOTS version | **Same version on all clients** |
| All players | Must have DOTS installed and enabled |

## Setup

Follow BT's own setup guide (`README-SETUP.txt` in the BT package). DOTS requires no special steps beyond ensuring every player has the same DOTS version in their mod list.

**Recommended mod load order:** DOTS before BannerlordTogether (alphabetical default usually handles this).

**Ports required:** 47770 and 47771 (TCP/UDP). Use Tailscale, Hamachi, or Radmin if you cannot port-forward.

**BattleLinkMPClient:** Leave this disabled in the normal singleplayer mod list per BT's own instructions. Enable it only when running a shared battle in the separate multiplayer window.

## How It Works

BT uses a **host-authoritative** model. The host runs the full campaign; clients mirror state. BT applies ~129 Harmony patches to suppress client-side logic (AI ticks, spawning, finance calculations) and sync campaign events over LiteNetLib (UDP).

DOTS runs entirely on top of vanilla systems. Because all clients have DOTS installed:
- All GameModel overrides (wages, speeds, morale, etc.) produce identical deterministic results on every machine
- All XML data (factions, cultures, lords, troops, equipment) is present on every machine
- Race assignments are embedded in the campaign save, which BT synchronizes from host to clients

## Known Limitations

| Limitation | Impact | Workaround |
|------------|--------|------------|
| Siege defense rewards (timed arrival) only fire on the host | Client players won't trigger `SiegeDefenseBehavior` rewards | Host handles siege events; client still participates in battle |
| Race data is host-save-authoritative | Joining client loads races from the host's save | None needed — this is correct behavior |
| BattleLink battles require the separate MP window | Both players need BT's BattleLinkMPClient enabled for live battles | Follow BT's battle server setup if desired |

## Conflict Analysis

DOTS and BT both patch `DeclareWarAction.ApplyInternal` and `MakePeaceAction.ApplyInternal`.

DOTS's patches are assigned `[HarmonyPriority(Priority.High)]` so they run **before** BT's sync patches. This ensures:
1. DOTS validates racial enmity / War of the Ring constraints first
2. If DOTS blocks the action (returns `false`), BT never syncs it to clients
3. If DOTS allows the action, BT syncs it normally

Without this ordering, BT could broadcast a war declaration to clients that DOTS subsequently blocks on the host, leaving clients in a desynchronized state.

## Key Files

| File | Purpose |
|------|---------|
| [DeclareWarAction_ApplyInternal_Patch.cs](../../Main/Features/Diplomacy/Hooks/DeclareWarAction_ApplyInternal_Patch.cs) | `Priority.High` ensures validation before BT sync |
| [MakePeaceAction_ApplyInternal_Patch.cs](../../Main/Features/Diplomacy/Hooks/MakePeaceAction_ApplyInternal_Patch.cs) | `Priority.High` ensures War of the Ring constraints before BT sync |
| [SiegeDefenseBehavior.cs](../../Main/Features/Siege/) | Host-only; expected not to fire on BT clients |
| [RacePersistenceBehavior.cs](../../Main/Features/HeroRace/) | Race data lives in the campaign save; syncs via BT's host-save mechanism |

## Known Incompatibility: DefaultClanFinanceModel Startup Crash

**Status: Unfixable from DOTS's side. Requires a fix in BannerlordTogether.**

When launching with both DOTS and BannerlordTogether, the game crashes on load with:

```
NullReferenceException at TaleWorlds.CampaignSystem.GameComponents.DefaultClanFinanceModel..cctor()
```

**Root cause (confirmed via decompilation):**

`DefaultClanFinanceModel` (vanilla TaleWorlds) has 16 static field initializers that call `Game.Current.GameTextManager.FindText(...)`. `Game.Current` is null during `OnSubModuleLoad`.

BannerlordTogether's `Harmony.PatchAll()` runs during `OnSubModuleLoad` and patches `DefaultClanFinanceModel` methods directly. MonoMod calls `RuntimeHelpers.PrepareMethod` on those methods, which triggers the class static constructor (`.cctor()`), which crashes on the null `Game.Current`.

**DOTS's defensive fix (applied, but not sufficient):**

All 13 DOTS GameModel override classes were changed from:
```csharp
private static readonly TextObject CultureText = GameTexts.FindText("str_culture");
```
to:
```csharp
private static TextObject? _cultureText;
private static TextObject CultureText => _cultureText ??= GameTexts.FindText("str_culture");
```
This removes DOTS's own `.cctor()` entries (good defensive practice) but does not prevent BT from triggering the crash in the vanilla `DefaultClanFinanceModel..cctor()`.

**Required fix (in BannerlordTogether):**

BT must defer `PatchAll()` — or at minimum the `DefaultClanFinanceModel` patches — to a hook where `Game.Current` is non-null (e.g., `OnGameStart` or `OnBeforeInitialModuleScreenSetAsRoot`), not `OnSubModuleLoad`.

**Workaround:** None available. Do not use DOTS + BannerlordTogether until BT ships a fix.

## Testing Checklist

- [ ] Both players load with DOTS + BT, no startup crash
- [ ] DOTS factions and culture names visible on map for client
- [ ] Client hero has correct race after joining
- [ ] Racial enmity blocks invalid war declarations (e.g. elves vs elves)
- [ ] War of the Ring forced wars trigger correctly on host; client mirrors state
- [ ] Siege defense events fire on host; client sees outcome via state sync

---

<!-- backlinks-start auto-generated; edit lint_docs.py / build_backlinks.py to change -->

## Referenced by

- [docs/INDEX.md](../INDEX.md)

<!-- backlinks-end -->
