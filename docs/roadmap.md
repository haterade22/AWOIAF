# DOTS Roadmap — GameModel Override Opportunities

Remaining GameModel override opportunities for making DOTS more LOTR-authentic.
DOTS currently has **31 of 121** available GameModels overridden.
Each override follows the established DOTS pattern (feature module + service + adapter + tests + JSON config).

## Implemented (no longer roadmap items)

| Model | Feature | Notes |
|-------|---------|-------|
| ~~`AgeModel`~~ | `RaceAge` | ✅ `DotsAgeModel` |
| ~~`PregnancyModel`~~ | `RaceAge` | ✅ `DotsPregnancyModel` |
| ~~`PartySizeLimitModel`~~ | `CulturalFeats` | ✅ `DotsPartySizeModel` |
| ~~`PartySpeedModel`~~ | `CulturalFeats` | ✅ `DotsPartySpeedModel` |
| ~~`CombatSimulationModel`~~ | `BattleBalance` | ✅ `DotsCombatSimulationModel` |
| ~~`DiplomacyModel`~~ | `Diplomacy` | ✅ `DotsDiplomacyModel` |
| ~~`SettlementLoyaltyModel`~~ | `CulturalFeats` | ✅ `DotsSettlementLoyaltyModel` |
| ~~`ClanFinanceModel`~~ | `CulturalFeats` | ✅ `DotsClanFinanceModel` |
| ~~`TournamentModel`~~ | `Arena` | ✅ `DotsTournamentModel` |
| ~~`SettlementProsperityModel`~~ | `CulturalFeats` | ✅ `DotsSettlementProsperityModel` |

## Remaining Opportunities

### Tier 1 — Race & Lifespan (Highest Visual Impact)

| Model | Override Goal |
|-------|---------------|
| `AgentStatCalculateModel` | Race-based stat bonuses (Uruk strength, Elf agility) |

### Tier 2 — Army & Campaign

| Model | Override Goal |
|-------|---------------|
| `BattleMoraleModel` | Racial fearlessness (Undead), cultural bravery |

### Tier 3 — Economy & Society

| Model | Override Goal |
|-------|---------------|
| `CharacterDevelopmentModel` | Race-locked skill caps |

### Tier 4 — Polish

| Model | Override Goal |
|-------|---------------|
| `MapVisibilityModel` | Ranger scouting range, Orc night vision |
| `DefectionModel` | Racial loyalty (Dwarves don't defect) |

## Recommended Implementation Order

1. `AgentStatCalculateModel` (Tier 1 — race stat bonuses)
2. `BattleMoraleModel` (Tier 2 — racial morale)
3. `CharacterDevelopmentModel` (Tier 3 — race skill caps)

## Notes

- All new overrides follow the pattern in `.claude/rules/gamemodels.md`
- Research the `Default*` base class before implementing each override
