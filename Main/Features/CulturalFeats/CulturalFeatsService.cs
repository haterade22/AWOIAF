using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace DOTS.Features.CulturalFeats;

/// <summary>
/// Concrete dispatch for <see cref="ICulturalFeatsService"/>. All methods are
/// pure (no side effects beyond mutating the by-ref <see cref="ExplainedNumber"/>),
/// no singleton lookups in the hot path, no per-call allocations beyond the
/// shared <see cref="CultureText"/>. Each method's structure mirrors the
/// original inline body of the corresponding <c>Dots*Model</c> override 1:1
/// to keep the behavior-preserving refactor reviewable line-by-line.
/// </summary>
public sealed class CulturalFeatsService : ICulturalFeatsService
{
    // Phase 9b #144 — preserved verbatim from the original models so the
    // ExplainedNumber description strings on the world-map tooltip stay
    // identical post-refactor. The `try` guard is for unit tests where the
    // TaleWorlds <c>GameTexts</c> static is uninitialised — the description
    // is a pure UI string and `Add`/`AddFactor` accept null descriptions.
    private static TextObject? _cultureText;
    private static bool _cultureTextResolved;
    private static TextObject? CultureText
    {
        get
        {
            if (_cultureTextResolved)
                return _cultureText;
            try { _cultureText = GameTexts.FindText("str_culture"); }
            catch { _cultureText = null; }
            _cultureTextResolved = true;
            return _cultureText;
        }
    }

    // ── ArmyManagement ──────────────────────────────────────────────────

    public float ApplyArmyInfluenceAward(ICultureFeatAdapter? culture, float baseAward)
    {
        if (culture == null)
            return baseAward;

        float result = baseAward;
        if (culture.HasFeat(DotsCulturalFeats.RivendellArmyInfluenceFeat))
            result += baseAward * DotsCulturalFeats.RivendellArmyInfluenceFeat.EffectBonus;
        if (culture.HasFeat(DotsCulturalFeats.GondorArmyInfluenceFeat))
            result += baseAward * DotsCulturalFeats.GondorArmyInfluenceFeat.EffectBonus;
        return result;
    }

    public int ApplyArmyInfluenceCost(ICultureFeatAdapter? culture, int baseCost)
    {
        if (culture == null)
            return baseCost;

        float multiplier = 0f;
        if (culture.HasFeat(DotsCulturalFeats.RivendellArmyInfluenceCostFeat))
            multiplier += DotsCulturalFeats.RivendellArmyInfluenceCostFeat.EffectBonus;
        if (culture.HasFeat(DotsCulturalFeats.GundabadArmyInfluenceCostFeat))
            multiplier += DotsCulturalFeats.GundabadArmyInfluenceCostFeat.EffectBonus;
        if (culture.HasFeat(DotsCulturalFeats.MistyMountainOrcsArmyInfluenceCostFeat))
            multiplier += DotsCulturalFeats.MistyMountainOrcsArmyInfluenceCostFeat.EffectBonus;
        if (culture.HasFeat(DotsCulturalFeats.DolGuldurArmyInfluenceCostFeat))
            multiplier += DotsCulturalFeats.DolGuldurArmyInfluenceCostFeat.EffectBonus;
        if (culture.HasFeat(DotsCulturalFeats.MordorArmyInfluenceCostFeat))
            multiplier += DotsCulturalFeats.MordorArmyInfluenceCostFeat.EffectBonus;
        if (culture.HasFeat(DotsCulturalFeats.MirkwoodArmyInfluenceCostFeat))
            multiplier += DotsCulturalFeats.MirkwoodArmyInfluenceCostFeat.EffectBonus;
        if (culture.HasFeat(DotsCulturalFeats.HaradArmyInfluenceCostFeat))
            multiplier += DotsCulturalFeats.HaradArmyInfluenceCostFeat.EffectBonus;

        return multiplier != 0f ? (int)(baseCost * (1f + multiplier)) : baseCost;
    }

    // ── PartySpeed ──────────────────────────────────────────────────────

    public void ApplyTerrainSpeedFeats(
        ICultureFeatAdapter? culture, TerrainKind terrain, bool isNight, ref ExplainedNumber result)
    {
        if (culture == null)
            return;

        switch (terrain)
        {
            case TerrainKind.Forest:
                ApplyIfHas(culture, DotsCulturalFeats.MirkwoodForestSpeedFeat, ref result);
                ApplyIfHas(culture, DotsCulturalFeats.LothlorienForestSpeedFeat, ref result);
                ApplyIfHas(culture, DotsCulturalFeats.RivendellForestSpeedFeat, ref result);
                break;
            case TerrainKind.Snow:
                ApplyIfHas(culture, DotsCulturalFeats.EreborSnowSpeedFeat, ref result);
                ApplyIfHas(culture, DotsCulturalFeats.GundabadSnowSpeedFeat, ref result);
                ApplyIfHas(culture, DotsCulturalFeats.GoblinSnowSpeedFeat, ref result);
                ApplyIfHas(culture, DotsCulturalFeats.MistyMountainOrcsSnowSpeedFeat, ref result);
                break;
            case TerrainKind.Steppe:
                ApplyIfHas(culture, DotsCulturalFeats.KhandSteppeSpeedFeat, ref result);
                ApplyIfHas(culture, DotsCulturalFeats.RhunSteppeSpeedFeat, ref result);
                break;
            case TerrainKind.Desert:
                ApplyIfHas(culture, DotsCulturalFeats.UmbarDesertSpeedFeat, ref result);
                ApplyIfHas(culture, DotsCulturalFeats.HaradDesertSpeedFeat, ref result);
                ApplyIfHas(culture, DotsCulturalFeats.ShaghanaDesertSpeedFeat, ref result);
                ApplyIfHas(culture, DotsCulturalFeats.AbanissaDesertSpeedFeat, ref result);
                break;
            case TerrainKind.Plain:
                ApplyIfHas(culture, DotsCulturalFeats.MordorPlainSpeedFeat, ref result);
                ApplyIfHas(culture, DotsCulturalFeats.GondorPlainSpeedFeat, ref result);
                ApplyIfHas(culture, DotsCulturalFeats.RohanPlainSpeedFeat, ref result);
                ApplyIfHas(culture, DotsCulturalFeats.DalePlainSpeedFeat, ref result);
                ApplyIfHas(culture, DotsCulturalFeats.DunlandPlainSpeedFeat, ref result);
                ApplyIfHas(culture, DotsCulturalFeats.IsengardPlainSpeedFeat, ref result);
                break;
            case TerrainKind.Swamp:
                ApplyIfHas(culture, DotsCulturalFeats.MordorSwampSpeedFeat, ref result);
                ApplyIfHas(culture, DotsCulturalFeats.IsengardSwampSpeedFeat, ref result);
                break;
        }

        if (isNight)
            ApplyIfHas(culture, DotsCulturalFeats.MordorNightSpeedFeat, ref result);
    }

    private static void ApplyIfHas(ICultureFeatAdapter culture, FeatObject feat, ref ExplainedNumber result)
    {
        if (culture.HasFeat(feat))
            result.AddFactor(feat.EffectBonus, CultureText);
    }

    public void ApplyRohanInfantryPenalty(
        ICultureFeatAdapter? culture, int mountedCount, int totalCount, ref ExplainedNumber result)
    {
        if (culture == null || totalCount <= 0)
            return;
        if (!culture.HasFeat(DotsCulturalFeats.RohanInfantrySpeedFeat))
            return;
        if (mountedCount * 2 < totalCount)
            result.AddFactor(DotsCulturalFeats.RohanInfantrySpeedFeat.EffectBonus, CultureText);
    }

    // ── SettlementProsperity ───────────────────────────────────────────

    public void ApplyHearthGrowthFeats(ICultureFeatAdapter? culture, ref ExplainedNumber result)
    {
        if (culture == null)
            return;

        // Match the original model's guard: only buff/nerf growth when result is still
        // non-negative. Negative results are skipped wholesale per pre-refactor behavior.
        if (culture.HasFeat(DotsCulturalFeats.RivendellHearthGrowthFeat) && result.ResultNumber >= 0f)
            result.AddFactor(DotsCulturalFeats.RivendellHearthGrowthFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.MirkwoodHearthGrowthFeat) && result.ResultNumber >= 0f)
            result.AddFactor(DotsCulturalFeats.MirkwoodHearthGrowthFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.GondorHearthGrowthFeat) && result.ResultNumber >= 0f)
            result.AddFactor(DotsCulturalFeats.GondorHearthGrowthFeat.EffectBonus, CultureText);
    }

    // ── SettlementMilitia ──────────────────────────────────────────────

    public void ApplyVeteranMilitiaFeats(ICultureFeatAdapter? culture, ref ExplainedNumber result)
    {
        if (culture == null)
            return;
        if (culture.HasFeat(DotsCulturalFeats.MirkwoodMilitiaProductionFeat))
            result.Add(DotsCulturalFeats.MirkwoodMilitiaProductionFeat.EffectBonus);
        if (culture.HasFeat(DotsCulturalFeats.DolGuldurMilitiaProductionFeat))
            result.Add(DotsCulturalFeats.DolGuldurMilitiaProductionFeat.EffectBonus);
    }

    // ── BuildingConstruction ───────────────────────────────────────────

    public void ApplyConstructionSpeedFeats(ICultureFeatAdapter? culture, ref ExplainedNumber result)
    {
        if (culture == null)
            return;
        if (culture.HasFeat(DotsCulturalFeats.EreborConstructionSpeedFeat))
            result.AddFactor(DotsCulturalFeats.EreborConstructionSpeedFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.LothlorienConstructionSpeedFeat))
            result.AddFactor(DotsCulturalFeats.LothlorienConstructionSpeedFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.DolGuldurConstructionSpeedFeat))
            result.AddFactor(DotsCulturalFeats.DolGuldurConstructionSpeedFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.IsengardConstructionSpeedFeat))
            result.AddFactor(DotsCulturalFeats.IsengardConstructionSpeedFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.MistyMountainOrcsConstructionSpeedFeat))
            result.AddFactor(DotsCulturalFeats.MistyMountainOrcsConstructionSpeedFeat.EffectBonus, CultureText);
    }

    // ── VillageProduction ──────────────────────────────────────────────

    public void ApplyVillageProductionFeats(ICultureFeatAdapter? culture, bool isGrain, ref ExplainedNumber result)
    {
        if (culture == null)
            return;
        if (culture.HasFeat(DotsCulturalFeats.EreborProductionFeat))
            result.AddFactor(DotsCulturalFeats.EreborProductionFeat.EffectBonus, CultureText);
        if (isGrain && culture.HasFeat(DotsCulturalFeats.GundabadGrainProductionFeat))
            result.AddFactor(DotsCulturalFeats.GundabadGrainProductionFeat.EffectBonus, CultureText);
        if (isGrain && culture.HasFeat(DotsCulturalFeats.MordorGrainProductionFeat))
            result.AddFactor(DotsCulturalFeats.MordorGrainProductionFeat.EffectBonus, CultureText);
    }

    // ── Caravan ────────────────────────────────────────────────────────

    public int ApplyCaravanCost(ICultureFeatAdapter? culture, int baseCost)
    {
        if (culture == null)
            return baseCost;
        if (culture.HasFeat(DotsCulturalFeats.UmbarCheaperCaravansFeat))
            return MathF.Round(baseCost * (1f + DotsCulturalFeats.UmbarCheaperCaravansFeat.EffectBonus));
        return baseCost;
    }

    // ── BattleReward ───────────────────────────────────────────────────

    public void ApplyRenownFeats(ICultureFeatAdapter? culture, ref ExplainedNumber result)
    {
        if (culture == null)
            return;
        if (culture.HasFeat(DotsCulturalFeats.UmbarRenownFeat))
            result.AddFactor(DotsCulturalFeats.UmbarRenownFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.DaleRenownFeat))
            result.AddFactor(DotsCulturalFeats.DaleRenownFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.KhandRenownFeat))
            result.AddFactor(DotsCulturalFeats.KhandRenownFeat.EffectBonus, CultureText);
    }

    // ── PartyTroopUpgrade ──────────────────────────────────────────────

    public void ApplyTroopUpgradeFeats(ICultureFeatAdapter? culture, bool isMounted, ref ExplainedNumber result)
    {
        if (culture == null || !isMounted)
            return;
        if (culture.HasFeat(DotsCulturalFeats.IsengardCheaperRecruitsFeat))
            result.AddFactor(DotsCulturalFeats.IsengardCheaperRecruitsFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.RohanMountedCostFeat))
            result.AddFactor(DotsCulturalFeats.RohanMountedCostFeat.EffectBonus, CultureText);
    }

    // ── PartySize ──────────────────────────────────────────────────────

    public void ApplyPartySizeFeats(ICultureFeatAdapter? culture, ref ExplainedNumber result)
    {
        if (culture == null)
            return;
        if (culture.HasFeat(DotsCulturalFeats.MordorPartySizeFeat))
            result.AddFactor(DotsCulturalFeats.MordorPartySizeFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.GundabadPartySizeFeat))
            result.AddFactor(DotsCulturalFeats.GundabadPartySizeFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.GoblinPartySizeFeat))
            result.AddFactor(DotsCulturalFeats.GoblinPartySizeFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.MistyMountainOrcsPartySizeFeat))
            result.AddFactor(DotsCulturalFeats.MistyMountainOrcsPartySizeFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.DolGuldurPartySizeFeat))
            result.AddFactor(DotsCulturalFeats.DolGuldurPartySizeFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.IsengardPartySizeFeat))
            result.AddFactor(DotsCulturalFeats.IsengardPartySizeFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.GondorPartySizeFeat))
            result.AddFactor(DotsCulturalFeats.GondorPartySizeFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.DunlandPartySizeFeat))
            result.AddFactor(DotsCulturalFeats.DunlandPartySizeFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.RhunPartySizeFeat))
            result.AddFactor(DotsCulturalFeats.RhunPartySizeFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.HaradPartySizeFeat))
            result.AddFactor(DotsCulturalFeats.HaradPartySizeFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.KhandPartySizeFeat))
            result.AddFactor(DotsCulturalFeats.KhandPartySizeFeat.EffectBonus, CultureText);
    }

    // ── VolunteerRespawn ──────────────────────────────────────────────

    public void ApplyVolunteerRespawnFeats(ICultureFeatAdapter? culture, ref ExplainedNumber result)
    {
        if (culture == null)
            return;
        if (culture.HasFeat(DotsCulturalFeats.DunlandVolunteerRateFeat))
            result.AddFactor(DotsCulturalFeats.DunlandVolunteerRateFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.GundabadVolunteerRateFeat))
            result.AddFactor(DotsCulturalFeats.GundabadVolunteerRateFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.GoblinVolunteerRateFeat))
            result.AddFactor(DotsCulturalFeats.GoblinVolunteerRateFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.DolGuldurVolunteerRateFeat))
            result.AddFactor(DotsCulturalFeats.DolGuldurVolunteerRateFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.MordorVolunteerRateFeat))
            result.AddFactor(DotsCulturalFeats.MordorVolunteerRateFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.LothlorienVolunteerRateFeat))
            result.AddFactor(DotsCulturalFeats.LothlorienVolunteerRateFeat.EffectBonus, CultureText);
    }

    // ── NotableSpawn ──────────────────────────────────────────────────

    public int ApplyNotableCountFeat(ICultureFeatAdapter? culture, NotableOccupationKind occupation, int baseCount)
    {
        if (culture == null || baseCount <= 0)
            return baseCount;

        // Town occupations use per-(culture, occupation) AdditionType.Add feats — supports the
        // asymmetric Isengard/Dol Guldur gang-leader-heavy distributions a uniform multiplier
        // couldn't express ("a few Merchants, many Gang Leaders").
        switch (occupation)
        {
            case NotableOccupationKind.Merchant:
            {
                int add = 0;
                if (culture.HasFeat(DotsCulturalFeats.IsengardNotableCountTownMerchantFeat))
                    add += (int)DotsCulturalFeats.IsengardNotableCountTownMerchantFeat.EffectBonus;
                if (culture.HasFeat(DotsCulturalFeats.DolGuldurNotableCountTownMerchantFeat))
                    add += (int)DotsCulturalFeats.DolGuldurNotableCountTownMerchantFeat.EffectBonus;
                return baseCount + add;
            }
            case NotableOccupationKind.Artisan:
            {
                int add = 0;
                if (culture.HasFeat(DotsCulturalFeats.IsengardNotableCountTownArtisanFeat))
                    add += (int)DotsCulturalFeats.IsengardNotableCountTownArtisanFeat.EffectBonus;
                if (culture.HasFeat(DotsCulturalFeats.DolGuldurNotableCountTownArtisanFeat))
                    add += (int)DotsCulturalFeats.DolGuldurNotableCountTownArtisanFeat.EffectBonus;
                if (culture.HasFeat(DotsCulturalFeats.GundabadNotableCountTownArtisanFeat))
                    add += (int)DotsCulturalFeats.GundabadNotableCountTownArtisanFeat.EffectBonus;
                return baseCount + add;
            }
            case NotableOccupationKind.GangLeader:
            {
                int add = 0;
                if (culture.HasFeat(DotsCulturalFeats.IsengardNotableCountTownGangLeaderFeat))
                    add += (int)DotsCulturalFeats.IsengardNotableCountTownGangLeaderFeat.EffectBonus;
                if (culture.HasFeat(DotsCulturalFeats.DolGuldurNotableCountTownGangLeaderFeat))
                    add += (int)DotsCulturalFeats.DolGuldurNotableCountTownGangLeaderFeat.EffectBonus;
                if (culture.HasFeat(DotsCulturalFeats.MordorNotableCountTownGangLeaderFeat))
                    add += (int)DotsCulturalFeats.MordorNotableCountTownGangLeaderFeat.EffectBonus;
                if (culture.HasFeat(DotsCulturalFeats.GundabadNotableCountTownGangLeaderFeat))
                    add += (int)DotsCulturalFeats.GundabadNotableCountTownGangLeaderFeat.EffectBonus;
                return baseCount + add;
            }
            case NotableOccupationKind.RuralNotable:
            case NotableOccupationKind.Headman:
            {
                // Village: legacy uniform per-(culture, village) AddFactor + ceiling. The 4 village
                // feats deliver the same +1/+1 distribution for all 4 cultures (the user's spec didn't
                // call for asymmetric village counts), so keeping the AddFactor shape is cleaner than
                // splitting them into per-occupation Add feats with identical values.
                float multiplier = 0f;
                if (culture.HasFeat(DotsCulturalFeats.IsengardNotableCountVillageFeat))
                    multiplier += DotsCulturalFeats.IsengardNotableCountVillageFeat.EffectBonus;
                if (culture.HasFeat(DotsCulturalFeats.DolGuldurNotableCountVillageFeat))
                    multiplier += DotsCulturalFeats.DolGuldurNotableCountVillageFeat.EffectBonus;
                if (culture.HasFeat(DotsCulturalFeats.MordorNotableCountVillageFeat))
                    multiplier += DotsCulturalFeats.MordorNotableCountVillageFeat.EffectBonus;
                if (culture.HasFeat(DotsCulturalFeats.GundabadNotableCountVillageFeat))
                    multiplier += DotsCulturalFeats.GundabadNotableCountVillageFeat.EffectBonus;
                if (multiplier <= 0f)
                    return baseCount;
                return (int)Math.Ceiling((double)baseCount * (1.0 + multiplier));
            }
            default:
                return baseCount;
        }
    }

    // ── FoodConsumption ────────────────────────────────────────────────

    public void ApplyFoodConsumptionFeats(ICultureFeatAdapter? culture, ref ExplainedNumber result)
    {
        if (culture == null)
            return;
        if (culture.HasFeat(DotsCulturalFeats.RivendellFoodConsumptionFeat))
            result.AddFactor(DotsCulturalFeats.RivendellFoodConsumptionFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.MirkwoodFoodConsumptionFeat))
            result.AddFactor(DotsCulturalFeats.MirkwoodFoodConsumptionFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.LothlorienFoodConsumptionFeat))
            result.AddFactor(DotsCulturalFeats.LothlorienFoodConsumptionFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.DolGuldurFoodConsumptionFeat))
            result.AddFactor(DotsCulturalFeats.DolGuldurFoodConsumptionFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.GoblinFoodConsumptionFeat))
            result.AddFactor(DotsCulturalFeats.GoblinFoodConsumptionFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.MistyMountainOrcsFoodConsumptionFeat))
            result.AddFactor(DotsCulturalFeats.MistyMountainOrcsFoodConsumptionFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.UmbarFoodConsumptionFeat))
            result.AddFactor(DotsCulturalFeats.UmbarFoodConsumptionFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.KhandFoodConsumptionFeat))
            result.AddFactor(DotsCulturalFeats.KhandFoodConsumptionFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.HaradFoodConsumptionFeat))
            result.AddFactor(DotsCulturalFeats.HaradFoodConsumptionFeat.EffectBonus, CultureText);
    }

    // ── SettlementLoyalty ──────────────────────────────────────────────

    public void ApplyLoyaltyFeats(ICultureFeatAdapter? culture, ref ExplainedNumber result)
    {
        if (culture == null)
            return;
        if (culture.HasFeat(DotsCulturalFeats.GondorLoyaltyFeat))
            result.Add(DotsCulturalFeats.GondorLoyaltyFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.EreborLoyaltyFeat))
            result.Add(DotsCulturalFeats.EreborLoyaltyFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.LothlorienLoyaltyFeat))
            result.Add(DotsCulturalFeats.LothlorienLoyaltyFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.RivendellLoyaltyFeat))
            result.Add(DotsCulturalFeats.RivendellLoyaltyFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.RohanLoyaltyFeat))
            result.Add(DotsCulturalFeats.RohanLoyaltyFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.DaleLoyaltyFeat))
            result.Add(DotsCulturalFeats.DaleLoyaltyFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.RhunLoyaltyFeat))
            result.Add(DotsCulturalFeats.RhunLoyaltyFeat.EffectBonus, CultureText);
    }

    // ── PartyMorale ────────────────────────────────────────────────────

    public void ApplyMoraleFeats(ICultureFeatAdapter? culture, ref ExplainedNumber result)
    {
        if (culture == null)
            return;
        if (culture.HasFeat(DotsCulturalFeats.GondorMoraleFeat))
            result.Add(DotsCulturalFeats.GondorMoraleFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.RohanMoraleFeat))
            result.Add(DotsCulturalFeats.RohanMoraleFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.EreborMoraleFeat))
            result.Add(DotsCulturalFeats.EreborMoraleFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.MirkwoodMoraleFeat))
            result.Add(DotsCulturalFeats.MirkwoodMoraleFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.LothlorienMoraleFeat))
            result.Add(DotsCulturalFeats.LothlorienMoraleFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.HaradMoraleFeat))
            result.Add(DotsCulturalFeats.HaradMoraleFeat.EffectBonus, CultureText);
    }

    // ── Smithing ───────────────────────────────────────────────────────

    public void ApplySmithingFeats(ICultureFeatAdapter? culture, ref ExplainedNumber result)
    {
        if (culture == null)
            return;
        if (culture.HasFeat(DotsCulturalFeats.EreborSmithingFeat))
            result.AddFactor(DotsCulturalFeats.EreborSmithingFeat.EffectBonus);
        if (culture.HasFeat(DotsCulturalFeats.IsengardSmithingFeat))
            result.AddFactor(DotsCulturalFeats.IsengardSmithingFeat.EffectBonus);
        if (culture.HasFeat(DotsCulturalFeats.MordorSmithingFeat))
            result.AddFactor(DotsCulturalFeats.MordorSmithingFeat.EffectBonus);
        if (culture.HasFeat(DotsCulturalFeats.GoblinSmithingFeat))
            result.AddFactor(DotsCulturalFeats.GoblinSmithingFeat.EffectBonus);
        if (culture.HasFeat(DotsCulturalFeats.MistyMountainOrcsSmithingFeat))
            result.AddFactor(DotsCulturalFeats.MistyMountainOrcsSmithingFeat.EffectBonus);
    }

    // ── ClanFinance (tariffs) ──────────────────────────────────────────

    public void ApplyTariffIncomeFeats(ICultureFeatAdapter? culture, ref ExplainedNumber result)
    {
        if (culture == null)
            return;
        if (culture.HasFeat(DotsCulturalFeats.UmbarTariffIncomeFeat))
            result.AddFactor(DotsCulturalFeats.UmbarTariffIncomeFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.EreborTariffIncomeFeat))
            result.AddFactor(DotsCulturalFeats.EreborTariffIncomeFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.DaleTariffIncomeFeat))
            result.AddFactor(DotsCulturalFeats.DaleTariffIncomeFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.KhandTariffIncomeFeat))
            result.AddFactor(DotsCulturalFeats.KhandTariffIncomeFeat.EffectBonus, CultureText);
    }

    // ── Raid ───────────────────────────────────────────────────────────

    public void ApplyRaidDamageFeats(ICultureFeatAdapter? culture, ref ExplainedNumber result)
    {
        if (culture == null)
            return;
        if (culture.HasFeat(DotsCulturalFeats.MordorRaidDamageFeat))
            result.AddFactor(DotsCulturalFeats.MordorRaidDamageFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.GundabadRaidDamageFeat))
            result.AddFactor(DotsCulturalFeats.GundabadRaidDamageFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.IsengardRaidDamageFeat))
            result.AddFactor(DotsCulturalFeats.IsengardRaidDamageFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.UmbarRaidDamageFeat))
            result.AddFactor(DotsCulturalFeats.UmbarRaidDamageFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.GoblinRaidDamageFeat))
            result.AddFactor(DotsCulturalFeats.GoblinRaidDamageFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.MistyMountainOrcsRaidDamageFeat))
            result.AddFactor(DotsCulturalFeats.MistyMountainOrcsRaidDamageFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.HaradRaidDamageFeat))
            result.AddFactor(DotsCulturalFeats.HaradRaidDamageFeat.EffectBonus, CultureText);
        if (culture.HasFeat(DotsCulturalFeats.RhunRaidDamageFeat))
            result.AddFactor(DotsCulturalFeats.RhunRaidDamageFeat.EffectBonus, CultureText);
    }
}
