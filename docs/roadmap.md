# DOTS Roadmap — GameModel Override Opportunities

Remaining GameModel override opportunities for making DOTS more **Westeros-authentic** (Game of
Thrones / Robert's Rebellion). DOTS currently overrides **35 GameModels** (see the
[GameModel Overrides table](../CLAUDE.md#gamemodel-overrides) in CLAUDE.md).
Each override follows the established DOTS pattern (feature module + service + adapter + tests + JSON config).

> **Note (2026-06-13):** DOTS is human-only. The race/lifespan GameModels the LOTR version used
> (`AgeModel`, `PregnancyModel`) were **stripped at the GoT bootstrap** and are no longer roadmap
> items. The opportunities below are re-scoped from race-based to **culture/house-based** mechanics.

## Implemented (no longer roadmap items)

| Model | Feature | Notes |
|-------|---------|-------|
| ~~`PartySizeLimitModel`~~ | `CulturalFeats` | ✅ `DotsPartySizeModel` |
| ~~`PartySpeedModel`~~ | `CulturalFeats` | ✅ `DotsPartySpeedModel` |
| ~~`CombatSimulationModel`~~ | `BattleBalance` | ✅ `DotsCombatSimulationModel` |
| ~~`DiplomacyModel`~~ | `Diplomacy` | ✅ `DotsDiplomacyModel` |
| ~~`SettlementLoyaltyModel`~~ | `CulturalFeats` | ✅ `DotsSettlementLoyaltyModel` |
| ~~`ClanFinanceModel`~~ | `CulturalFeats` | ✅ `DotsClanFinanceModel` |
| ~~`TournamentModel`~~ | `Arena` | ✅ `DotsTournamentModel` |
| ~~`SettlementProsperityModel`~~ | `CulturalFeats` | ✅ `DotsSettlementProsperityModel` |

## Remaining Opportunities

### Tier 1 — Culture & Martial Traits (Highest Gameplay Impact)

| Model | Override Goal |
|-------|---------------|
| `AgentStatCalculateModel` | Culture/house martial-trait stat bonuses (Northern toughness, Dornish agility, Ironborn vigor) |

### Tier 2 — Army & Campaign

| Model | Override Goal |
|-------|---------------|
| `BattleMoraleModel` | House/regional bravery and morale (last-stand resolve for besieged loyalists, raider ferocity) |

### Tier 3 — Economy & Society

| Model | Override Goal |
|-------|---------------|
| `CharacterDevelopmentModel` | Culture skill caps / focus tendencies per region |

### Tier 4 — Polish

| Model | Override Goal |
|-------|---------------|
| `MapVisibilityModel` | Outrider / scout scouting range per culture |
| `DefectionModel` | House loyalty (sworn bannermen are slower to defect) |

## Recommended Implementation Order

1. `AgentStatCalculateModel` (Tier 1 — culture martial-trait stat bonuses)
2. `BattleMoraleModel` (Tier 2 — house/regional morale)
3. `CharacterDevelopmentModel` (Tier 3 — culture skill caps)

## Notes

- All new overrides follow the pattern in `.claude/rules/gamemodels.md`
- Research the `Default*` base class before implementing each override
