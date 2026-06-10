# Codex Adversarial Review: CulturalFeats

**Date:** 2026-04-05
**Target:** working tree diff
**Verdict:** needs-attention

No-ship: the CulturalFeats GameModels embed business logic directly in engine-facing entry points, one mounted-upgrade discount branch is functionally wrong, the shipped behavior has no meaningful tests, and feat registration is brittle because the Harmony hook was not verifiable from this environment.

## Files Reviewed

- `Main/Features/CulturalFeats/DotsCulturalFeats.cs`
- `Main/Features/CulturalFeats/Hooks/Campaign_InitializeDefaultCampaignObjects_Patch.cs`
- `Main/Features/CulturalFeats/Models/DotsArmyManagementModel.cs`
- `Main/Features/CulturalFeats/Models/DotsSettlementMilitiaModel.cs`
- `Main/Features/CulturalFeats/Models/DotsCaravanModel.cs`
- `Main/Features/CulturalFeats/Models/DotsSmithingModel.cs`
- `Main/Features/CulturalFeats/Models/DotsBattleRewardModel.cs`
- `Main/Features/CulturalFeats/Models/DotsBuildingConstructionModel.cs`
- `Main/Features/CulturalFeats/Models/DotsClanFinanceModel.cs`
- `Main/Features/CulturalFeats/Models/DotsFoodConsumptionModel.cs`
- `Main/Features/CulturalFeats/Models/DotsPartyMoraleModel.cs`
- `Main/Features/CulturalFeats/Models/DotsPartySizeModel.cs`
- `Main/Features/CulturalFeats/Models/DotsPartySpeedModel.cs`
- `Main/Features/CulturalFeats/Models/DotsPartyTroopUpgradeModel.cs`
- `Main/Features/CulturalFeats/Models/DotsRaidModel.cs`
- `Main/Features/CulturalFeats/Models/DotsSettlementLoyaltyModel.cs`
- `Main/Features/CulturalFeats/Models/DotsSettlementProsperityModel.cs`
- `Main/Features/CulturalFeats/Models/DotsVillageProductionModel.cs`
- `DOTS.Tests/Features/CulturalFeats/DotsCulturalFeatsDefinitionTests.cs`

## Focus Areas

1. Adapter pattern violations (ADR-007)
2. Thin entry point violations (ADR-002)
3. GameModel override correctness — inheriting from Default* base, calling base.Method()
4. Test coverage gaps
5. Harmony patch target verification

## Findings

### [CRITICAL] GameModel entry points contain business logic — ADR-002/ADR-007 violation

**File:** `DotsArmyManagementModel.cs:7-52`

`DotsArmyManagementModel` performs culture lookup and feat math directly against sealed TaleWorlds types (`MobileParty`, `Party`, `Culture`) instead of delegating to a constructor-injected service over adapters. That is an ADR-002/ADR-007 violation, and the same pattern is repeated across the other CulturalFeats models in this review set. The practical risk is that all feature behavior is trapped in engine-bound entry points that are hard to mock, hard to test, and easy to regress when TaleWorlds API behavior changes.

**Remediation:** Extract the feat rules into one or more constructor-injected services that operate on adapters, then keep each GameModel override to boundary conversion, service delegation, and `base` fallback only.

### [HIGH] Mounted upgrade discounts applied to source troop instead of upgrade target

**File:** `DotsPartyTroopUpgradeModel.cs:19-26`

The discount branch is gated by `characterObject.IsMounted`, but `GetGoldCostForUpgrade` is pricing the transition to `upgradeTarget`. That means the advertised Rohan/Isengard mounted discount is skipped on the common infantry-to-cavalry upgrade path, even though those are exactly the upgrades that introduce horse cost. This is a player-visible economy regression, not a cosmetic issue.

**Remediation:** Base the feat check on `upgradeTarget.IsMounted` or on the actual mounted-cost portion of the upgrade calculation, and add tests covering infantry->mounted and mounted->mounted upgrades for both feat cultures.

### [HIGH] Tests do not cover any shipped CulturalFeats behavior

**File:** `DotsCulturalFeatsDefinitionTests.cs:18-162`

The test file only reflects over property names/counts and asserts that `GetAllFeats()` is empty before initialization. It never exercises any GameModel override, never verifies the feat math, and never checks null/ownership edge cases. Because the business logic lives in the models instead of services, this leaves army influence, party size, morale, loyalty, construction, smithing, raid damage, caravan cost, and speed behavior effectively untested.

**Remediation:** Add behavioral tests for the feat calculations. Preferably extract the logic into services and unit-test those; if that refactor is deferred, add targeted model tests that assert the actual `ExplainedNumber`/cost outputs for each feat path and relevant null-owner cases.

### [MEDIUM] Feat accessors can null-reference if registration hook does not run

**File:** `DotsCulturalFeats.cs:101-173`

Every public feat property blindly dereferences `_instance`, and initialization depends entirely on the Harmony postfix calling `CreateAndRegister()`. If that patch stops applying because of version drift, ordering changes, or category/patch registration issues, the first feat lookup will fail as a null-reference inside unrelated GameModels. The Harmony target (`Campaign.InitializeDefaultCampaignObjects`) could not be verified against Bannerlord 1.3.15 because the ILSpy MCP was unavailable during the review.

**Remediation:** Make feat access fail fast with an explicit guard/message instead of a raw `_instance` dereference, and verify the Harmony target signature against Bannerlord v1.3.15 before shipping.

## Recommended Next Steps

1. Refactor CulturalFeats GameModels into thin boundary classes over constructor-injected services/adapters
2. Fix `DotsPartyTroopUpgradeModel` to apply mounted discounts based on the upgrade destination
3. Replace reflection-only tests with behavioral coverage for actual feat calculations
4. Verify `Campaign.InitializeDefaultCampaignObjects` Harmony target against v1.3.15 with ILSpy
