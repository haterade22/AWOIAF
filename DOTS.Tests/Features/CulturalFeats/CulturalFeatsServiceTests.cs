using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using DOTS.Features.CulturalFeats;

namespace DOTS.Tests.Features.CulturalFeats;

/// <summary>
/// Tests the dispatch logic extracted from the 16 <c>Dots*Model</c> overrides
/// in <see cref="DOTS.Features.CulturalFeats.Models"/>. The service is tested
/// against a stubbed <see cref="ICultureFeatAdapter"/> so no live TaleWorlds
/// culture instances are required. <see cref="FeatObject"/> instances are
/// constructed with non-trivial <c>EffectBonus</c> values via reflection (the
/// vanilla <c>Initialize()</c> requires <c>Campaign.Current</c> which is not
/// available in unit tests).
/// </summary>
[TestClass]
public class CulturalFeatsServiceTests
{
    private ICulturalFeatsService _sut = null!;

    [TestInitialize]
    public void Init()
    {
        _sut = new CulturalFeatsService();
        EnsureFeatsInitialised();
    }

    // ── ArmyManagement ──────────────────────────────────────────────────

    [TestMethod]
    public void ApplyArmyInfluenceAward_NullCulture_ReturnsBaseAward()
    {
        var result = _sut.ApplyArmyInfluenceAward(null, 100f);
        Assert.AreEqual(100f, result);
    }

    [TestMethod]
    public void ApplyArmyInfluenceAward_RivendellOnly_AddsRivendellBonusToBase()
    {
        var culture = AdapterWith(DotsCulturalFeats.RivendellArmyInfluenceFeat);

        var result = _sut.ApplyArmyInfluenceAward(culture, 100f);

        Assert.AreEqual(
            100f + 100f * DotsCulturalFeats.RivendellArmyInfluenceFeat.EffectBonus,
            result);
    }

    [TestMethod]
    public void ApplyArmyInfluenceAward_GondorOnly_AddsGondorBonusToBase()
    {
        var culture = AdapterWith(DotsCulturalFeats.GondorArmyInfluenceFeat);

        var result = _sut.ApplyArmyInfluenceAward(culture, 100f);

        Assert.AreEqual(
            100f + 100f * DotsCulturalFeats.GondorArmyInfluenceFeat.EffectBonus,
            result);
    }

    [TestMethod]
    public void ApplyArmyInfluenceCost_NullCulture_ReturnsBaseCost()
    {
        var result = _sut.ApplyArmyInfluenceCost(null, 100);
        Assert.AreEqual(100, result);
    }

    [TestMethod]
    public void ApplyArmyInfluenceCost_GundabadOnly_AppliesGundabadFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.GundabadArmyInfluenceCostFeat);

        var result = _sut.ApplyArmyInfluenceCost(culture, 100);

        var expected = (int)(100 * (1f + DotsCulturalFeats.GundabadArmyInfluenceCostFeat.EffectBonus));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void ApplyArmyInfluenceCost_MultipleFeats_StackAdditively()
    {
        var culture = AdapterWith(
            DotsCulturalFeats.RivendellArmyInfluenceCostFeat,
            DotsCulturalFeats.MordorArmyInfluenceCostFeat);

        var result = _sut.ApplyArmyInfluenceCost(culture, 100);

        var multiplier = DotsCulturalFeats.RivendellArmyInfluenceCostFeat.EffectBonus
                       + DotsCulturalFeats.MordorArmyInfluenceCostFeat.EffectBonus;
        var expected = (int)(100 * (1f + multiplier));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void ApplyArmyInfluenceCost_NoMatchingFeats_LeavesCostUnchanged()
    {
        var culture = Substitute.For<ICultureFeatAdapter>();
        culture.HasFeat(Arg.Any<FeatObject>()).Returns(false);

        var result = _sut.ApplyArmyInfluenceCost(culture, 100);

        Assert.AreEqual(100, result);
    }

    // ── PartySpeed ──────────────────────────────────────────────────────

    [TestMethod]
    public void ApplyTerrainSpeedFeats_NullCulture_DoesNothing()
    {
        var en = new ExplainedNumber(1f);
        _sut.ApplyTerrainSpeedFeats(null, TerrainKind.Forest, isNight: false, ref en);
        Assert.AreEqual(1f, en.ResultNumber);
    }

    [TestMethod]
    public void ApplyTerrainSpeedFeats_NoneTerrain_DoesNothing()
    {
        var culture = AdapterWith(DotsCulturalFeats.MirkwoodForestSpeedFeat);
        var en = new ExplainedNumber(1f);

        _sut.ApplyTerrainSpeedFeats(culture, TerrainKind.None, isNight: false, ref en);

        Assert.AreEqual(1f, en.ResultNumber);
    }

    [TestMethod]
    public void ApplyTerrainSpeedFeats_WrongTerrain_DoesNothing()
    {
        // Mirkwood's forest feat must NOT apply when the party is on a plain.
        var culture = AdapterWith(DotsCulturalFeats.MirkwoodForestSpeedFeat);
        var en = new ExplainedNumber(1f);

        _sut.ApplyTerrainSpeedFeats(culture, TerrainKind.Plain, isNight: false, ref en);

        Assert.AreEqual(1f, en.ResultNumber);
    }

    [TestMethod]
    public void ApplyTerrainSpeedFeats_Forest_ElvenFeat_AppliesFlatTenPercent()
    {
        var culture = AdapterWith(DotsCulturalFeats.MirkwoodForestSpeedFeat);
        var en = new ExplainedNumber(1f);

        _sut.ApplyTerrainSpeedFeats(culture, TerrainKind.Forest, isNight: false, ref en);

        // Flat AddFactor of the feat's EffectBonus (0.10), NOT the old scaled value.
        Assert.AreEqual(1f * (1f + 0.1f), en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyTerrainSpeedFeats_Snow_DwarfFeat_AppliesTenPercent()
    {
        var culture = AdapterWith(DotsCulturalFeats.EreborSnowSpeedFeat);
        var en = new ExplainedNumber(1f);

        _sut.ApplyTerrainSpeedFeats(culture, TerrainKind.Snow, isNight: false, ref en);

        Assert.AreEqual(1f * (1f + 0.1f), en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyTerrainSpeedFeats_Steppe_KhandFeat_AppliesTenPercent()
    {
        var culture = AdapterWith(DotsCulturalFeats.KhandSteppeSpeedFeat);
        var en = new ExplainedNumber(1f);

        _sut.ApplyTerrainSpeedFeats(culture, TerrainKind.Steppe, isNight: false, ref en);

        Assert.AreEqual(1f * (1f + 0.1f), en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyTerrainSpeedFeats_Desert_HaradFeat_AppliesTenPercent()
    {
        var culture = AdapterWith(DotsCulturalFeats.HaradDesertSpeedFeat);
        var en = new ExplainedNumber(1f);

        _sut.ApplyTerrainSpeedFeats(culture, TerrainKind.Desert, isNight: false, ref en);

        Assert.AreEqual(1f * (1f + 0.1f), en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyTerrainSpeedFeats_Plain_MordorFeat_AppliesFivePercent()
    {
        // Mordor's terrain buff is deliberately smaller (5%) than the 10% others get.
        var culture = AdapterWith(DotsCulturalFeats.MordorPlainSpeedFeat);
        var en = new ExplainedNumber(1f);

        _sut.ApplyTerrainSpeedFeats(culture, TerrainKind.Plain, isNight: false, ref en);

        Assert.AreEqual(1f * (1f + 0.05f), en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyTerrainSpeedFeats_Swamp_MordorFeat_AppliesFivePercent()
    {
        var culture = AdapterWith(DotsCulturalFeats.MordorSwampSpeedFeat);
        var en = new ExplainedNumber(1f);

        _sut.ApplyTerrainSpeedFeats(culture, TerrainKind.Swamp, isNight: false, ref en);

        Assert.AreEqual(1f * (1f + 0.05f), en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyTerrainSpeedFeats_Plain_GondorFeat_AppliesTenPercent()
    {
        var culture = AdapterWith(DotsCulturalFeats.GondorPlainSpeedFeat);
        var en = new ExplainedNumber(1f);

        _sut.ApplyTerrainSpeedFeats(culture, TerrainKind.Plain, isNight: false, ref en);

        Assert.AreEqual(1f * (1f + 0.1f), en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyTerrainSpeedFeats_Night_MordorFeat_AppliesTenPercent()
    {
        var culture = AdapterWith(DotsCulturalFeats.MordorNightSpeedFeat);
        var en = new ExplainedNumber(1f);

        // Night bonus is terrain-independent — passing None still applies it.
        _sut.ApplyTerrainSpeedFeats(culture, TerrainKind.None, isNight: true, ref en);

        Assert.AreEqual(1f * (1f + 0.1f), en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyTerrainSpeedFeats_Day_MordorNightFeat_DoesNotApply()
    {
        var culture = AdapterWith(DotsCulturalFeats.MordorNightSpeedFeat);
        var en = new ExplainedNumber(1f);

        _sut.ApplyTerrainSpeedFeats(culture, TerrainKind.Plain, isNight: false, ref en);

        Assert.AreEqual(1f, en.ResultNumber);
    }

    [TestMethod]
    public void ApplyTerrainSpeedFeats_MordorPlainAtNight_StacksTerrainAndNight()
    {
        var culture = AdapterWith(
            DotsCulturalFeats.MordorPlainSpeedFeat,
            DotsCulturalFeats.MordorNightSpeedFeat);
        var en = new ExplainedNumber(1f);

        _sut.ApplyTerrainSpeedFeats(culture, TerrainKind.Plain, isNight: true, ref en);

        // 5% plain + 10% night, additive ExplainedNumber factors.
        Assert.AreEqual(1f * (1f + 0.05f + 0.1f), en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyRohanInfantryPenalty_NoRohanFeat_DoesNothing()
    {
        var culture = Substitute.For<ICultureFeatAdapter>();
        culture.HasFeat(Arg.Any<FeatObject>()).Returns(false);
        var en = new ExplainedNumber(1f);

        _sut.ApplyRohanInfantryPenalty(culture, mountedCount: 0, totalCount: 10, ref en);

        Assert.AreEqual(1f, en.ResultNumber);
    }

    [TestMethod]
    public void ApplyRohanInfantryPenalty_MajorityInfantry_AppliesPenalty()
    {
        var culture = AdapterWith(DotsCulturalFeats.RohanInfantrySpeedFeat);
        var en = new ExplainedNumber(1f);

        // 4 mounted out of 10 — 40% mounted, so > 50% infantry → penalty applies
        _sut.ApplyRohanInfantryPenalty(culture, mountedCount: 4, totalCount: 10, ref en);

        Assert.AreEqual(
            1f * (1f + DotsCulturalFeats.RohanInfantrySpeedFeat.EffectBonus),
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplyRohanInfantryPenalty_MostlyMounted_NoPenalty()
    {
        var culture = AdapterWith(DotsCulturalFeats.RohanInfantrySpeedFeat);
        var en = new ExplainedNumber(1f);

        // 6 mounted out of 10 — 60% mounted → no penalty
        _sut.ApplyRohanInfantryPenalty(culture, mountedCount: 6, totalCount: 10, ref en);

        Assert.AreEqual(1f, en.ResultNumber);
    }

    [TestMethod]
    public void ApplyRohanInfantryPenalty_ZeroTotal_NoPenalty()
    {
        var culture = AdapterWith(DotsCulturalFeats.RohanInfantrySpeedFeat);
        var en = new ExplainedNumber(1f);

        _sut.ApplyRohanInfantryPenalty(culture, mountedCount: 0, totalCount: 0, ref en);

        Assert.AreEqual(1f, en.ResultNumber);
    }

    // ── SettlementProsperity (hearth growth) ───────────────────────────

    [TestMethod]
    public void ApplyHearthGrowthFeats_NegativeResult_SkipsAllFeats()
    {
        var culture = AdapterWith(
            DotsCulturalFeats.RivendellHearthGrowthFeat,
            DotsCulturalFeats.MirkwoodHearthGrowthFeat,
            DotsCulturalFeats.GondorHearthGrowthFeat);
        var en = new ExplainedNumber(-5f);

        _sut.ApplyHearthGrowthFeats(culture, ref en);

        // Negative → all skipped per gamemodel guard
        Assert.AreEqual(-5f, en.ResultNumber);
    }

    [TestMethod]
    public void ApplyHearthGrowthFeats_PositiveResult_RivendellApplies()
    {
        var culture = AdapterWith(DotsCulturalFeats.RivendellHearthGrowthFeat);
        var en = new ExplainedNumber(10f);

        _sut.ApplyHearthGrowthFeats(culture, ref en);

        Assert.AreEqual(
            10f * (1f + DotsCulturalFeats.RivendellHearthGrowthFeat.EffectBonus),
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplyHearthGrowthFeats_NullCulture_DoesNothing()
    {
        var en = new ExplainedNumber(10f);
        _sut.ApplyHearthGrowthFeats(null, ref en);
        Assert.AreEqual(10f, en.ResultNumber);
    }

    // ── SettlementMilitia ──────────────────────────────────────────────

    [TestMethod]
    public void ApplyVeteranMilitiaFeats_MirkwoodOnly_AddsBonus()
    {
        var culture = AdapterWith(DotsCulturalFeats.MirkwoodMilitiaProductionFeat);
        var en = new ExplainedNumber(0.1f);

        _sut.ApplyVeteranMilitiaFeats(culture, ref en);

        Assert.AreEqual(
            0.1f + DotsCulturalFeats.MirkwoodMilitiaProductionFeat.EffectBonus,
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplyVeteranMilitiaFeats_NullCulture_DoesNothing()
    {
        var en = new ExplainedNumber(0.1f);
        _sut.ApplyVeteranMilitiaFeats(null, ref en);
        Assert.AreEqual(0.1f, en.ResultNumber);
    }

    // ── BuildingConstruction ───────────────────────────────────────────

    [TestMethod]
    public void ApplyConstructionSpeedFeats_EreborOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.EreborConstructionSpeedFeat);
        var en = new ExplainedNumber(20f);

        _sut.ApplyConstructionSpeedFeats(culture, ref en);

        Assert.AreEqual(
            20f * (1f + DotsCulturalFeats.EreborConstructionSpeedFeat.EffectBonus),
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplyConstructionSpeedFeats_NullCulture_DoesNothing()
    {
        var en = new ExplainedNumber(20f);
        _sut.ApplyConstructionSpeedFeats(null, ref en);
        Assert.AreEqual(20f, en.ResultNumber);
    }

    // ── VillageProduction ──────────────────────────────────────────────

    [TestMethod]
    public void ApplyVillageProductionFeats_EreborOnly_GeneralProductionApplies()
    {
        var culture = AdapterWith(DotsCulturalFeats.EreborProductionFeat);
        var en = new ExplainedNumber(50f);

        _sut.ApplyVillageProductionFeats(culture, isGrain: false, ref en);

        Assert.AreEqual(
            50f * (1f + DotsCulturalFeats.EreborProductionFeat.EffectBonus),
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplyVillageProductionFeats_GundabadGrain_OnlyAppliesWhenGrain()
    {
        var culture = AdapterWith(DotsCulturalFeats.GundabadGrainProductionFeat);
        var en = new ExplainedNumber(50f);

        _sut.ApplyVillageProductionFeats(culture, isGrain: false, ref en);

        Assert.AreEqual(50f, en.ResultNumber);
    }

    [TestMethod]
    public void ApplyVillageProductionFeats_GundabadGrain_AppliesWhenGrain()
    {
        var culture = AdapterWith(DotsCulturalFeats.GundabadGrainProductionFeat);
        var en = new ExplainedNumber(50f);

        _sut.ApplyVillageProductionFeats(culture, isGrain: true, ref en);

        Assert.AreEqual(
            50f * (1f + DotsCulturalFeats.GundabadGrainProductionFeat.EffectBonus),
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplyVillageProductionFeats_NullCulture_DoesNothing()
    {
        var en = new ExplainedNumber(50f);
        _sut.ApplyVillageProductionFeats(null, isGrain: true, ref en);
        Assert.AreEqual(50f, en.ResultNumber);
    }

    // ── Caravan ────────────────────────────────────────────────────────

    [TestMethod]
    public void ApplyCaravanCost_NullCulture_ReturnsBaseCost()
    {
        Assert.AreEqual(1000, _sut.ApplyCaravanCost(null, 1000));
    }

    [TestMethod]
    public void ApplyCaravanCost_NoFeat_ReturnsBaseCost()
    {
        var culture = Substitute.For<ICultureFeatAdapter>();
        culture.HasFeat(Arg.Any<FeatObject>()).Returns(false);
        Assert.AreEqual(1000, _sut.ApplyCaravanCost(culture, 1000));
    }

    [TestMethod]
    public void ApplyCaravanCost_UmbarFeat_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.UmbarCheaperCaravansFeat);

        var result = _sut.ApplyCaravanCost(culture, 1000);

        var expected = (int)System.Math.Round(
            1000 * (1f + DotsCulturalFeats.UmbarCheaperCaravansFeat.EffectBonus),
            System.MidpointRounding.AwayFromZero);
        Assert.AreEqual(expected, result);
    }

    // ── BattleReward ───────────────────────────────────────────────────

    [TestMethod]
    public void ApplyRenownFeats_UmbarOnly_AddsFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.UmbarRenownFeat);
        var en = new ExplainedNumber(50f);

        _sut.ApplyRenownFeats(culture, ref en);

        Assert.AreEqual(
            50f * (1f + DotsCulturalFeats.UmbarRenownFeat.EffectBonus),
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplyRenownFeats_NullCulture_DoesNothing()
    {
        var en = new ExplainedNumber(50f);
        _sut.ApplyRenownFeats(null, ref en);
        Assert.AreEqual(50f, en.ResultNumber);
    }

    // ── PartyTroopUpgrade ──────────────────────────────────────────────

    [TestMethod]
    public void ApplyTroopUpgradeFeats_NotMounted_DoesNothing()
    {
        var culture = AdapterWith(
            DotsCulturalFeats.IsengardCheaperRecruitsFeat,
            DotsCulturalFeats.RohanMountedCostFeat);
        var en = new ExplainedNumber(100f);

        _sut.ApplyTroopUpgradeFeats(culture, isMounted: false, ref en);

        Assert.AreEqual(100f, en.ResultNumber);
    }

    [TestMethod]
    public void ApplyTroopUpgradeFeats_MountedIsengard_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.IsengardCheaperRecruitsFeat);
        var en = new ExplainedNumber(100f);

        _sut.ApplyTroopUpgradeFeats(culture, isMounted: true, ref en);

        Assert.AreEqual(
            100f * (1f + DotsCulturalFeats.IsengardCheaperRecruitsFeat.EffectBonus),
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplyTroopUpgradeFeats_NullCulture_DoesNothing()
    {
        var en = new ExplainedNumber(100f);
        _sut.ApplyTroopUpgradeFeats(null, isMounted: true, ref en);
        Assert.AreEqual(100f, en.ResultNumber);
    }

    // ── PartySize ──────────────────────────────────────────────────────

    [TestMethod]
    public void ApplyPartySizeFeats_MordorOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.MordorPartySizeFeat);
        var en = new ExplainedNumber(100f);

        _sut.ApplyPartySizeFeats(culture, ref en);

        Assert.AreEqual(
            100f * (1f + DotsCulturalFeats.MordorPartySizeFeat.EffectBonus),
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplyPartySizeFeats_NullCulture_DoesNothing()
    {
        var en = new ExplainedNumber(100f);
        _sut.ApplyPartySizeFeats(null, ref en);
        Assert.AreEqual(100f, en.ResultNumber);
    }

    [TestMethod]
    public void ApplyPartySizeFeats_DunlandOnly_AppliesFivePercent()
    {
        var culture = AdapterWith(DotsCulturalFeats.DunlandPartySizeFeat);
        var en = new ExplainedNumber(100f);
        _sut.ApplyPartySizeFeats(culture, ref en);
        Assert.AreEqual(100f * (1f + 0.05f), en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyPartySizeFeats_RhunOnly_AppliesFivePercent()
    {
        var culture = AdapterWith(DotsCulturalFeats.RhunPartySizeFeat);
        var en = new ExplainedNumber(100f);
        _sut.ApplyPartySizeFeats(culture, ref en);
        Assert.AreEqual(100f * (1f + 0.05f), en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyPartySizeFeats_HaradOnly_AppliesFivePercent()
    {
        var culture = AdapterWith(DotsCulturalFeats.HaradPartySizeFeat);
        var en = new ExplainedNumber(100f);
        _sut.ApplyPartySizeFeats(culture, ref en);
        Assert.AreEqual(100f * (1f + 0.05f), en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyPartySizeFeats_GondorOnly_AppliesRetunedTwoPointFivePercent()
    {
        // Verifies Gondor retune from 0.10 → 0.025. EnsureFeatsInitialised stores the new
        // reflected value, and the test reads it dynamically — so this guards both the
        // reflection-table update and the production InitializeAll value.
        var culture = AdapterWith(DotsCulturalFeats.GondorPartySizeFeat);
        var en = new ExplainedNumber(100f);
        _sut.ApplyPartySizeFeats(culture, ref en);
        Assert.AreEqual(100f * (1f + 0.025f), en.ResultNumber, 0.0001f);
        Assert.AreEqual(0.025f, DotsCulturalFeats.GondorPartySizeFeat.EffectBonus, 0.0001f);
    }

    [TestMethod]
    public void ApplyPartySizeFeats_RetunedValues_MatchTargets()
    {
        // Defensive: confirm the 4 retuned EffectBonus values.
        Assert.AreEqual(0.10f, DotsCulturalFeats.MordorPartySizeFeat.EffectBonus, 0.0001f);
        Assert.AreEqual(0.20f, DotsCulturalFeats.GundabadPartySizeFeat.EffectBonus, 0.0001f);
        Assert.AreEqual(0.20f, DotsCulturalFeats.DolGuldurPartySizeFeat.EffectBonus, 0.0001f);
        Assert.AreEqual(0.025f, DotsCulturalFeats.GondorPartySizeFeat.EffectBonus, 0.0001f);
    }

    // ── VolunteerRespawn ──────────────────────────────────────────────

    [TestMethod]
    public void ApplyVolunteerRespawnFeats_NullCulture_DoesNothing()
    {
        var en = new ExplainedNumber(0.7f);
        _sut.ApplyVolunteerRespawnFeats(null, ref en);
        Assert.AreEqual(0.7f, en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyVolunteerRespawnFeats_NonMatchingCulture_DoesNothing()
    {
        var culture = Substitute.For<ICultureFeatAdapter>();
        culture.HasFeat(Arg.Any<FeatObject>()).Returns(false);
        var en = new ExplainedNumber(0.7f);
        _sut.ApplyVolunteerRespawnFeats(culture, ref en);
        Assert.AreEqual(0.7f, en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyVolunteerRespawnFeats_DunlandCulture_AddsTenPercent()
    {
        var culture = AdapterWith(DotsCulturalFeats.DunlandVolunteerRateFeat);
        var en = new ExplainedNumber(0.7f);
        _sut.ApplyVolunteerRespawnFeats(culture, ref en);
        Assert.AreEqual(0.7f * (1f + 0.1f), en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyVolunteerRespawnFeats_MordorCulture_AddsTwentyPercent()
    {
        var culture = AdapterWith(DotsCulturalFeats.MordorVolunteerRateFeat);
        var en = new ExplainedNumber(0.7f);
        _sut.ApplyVolunteerRespawnFeats(culture, ref en);
        Assert.AreEqual(0.7f * (1f + 0.2f), en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyVolunteerRespawnFeats_GundabadCulture_AddsTwentyPercent()
    {
        var culture = AdapterWith(DotsCulturalFeats.GundabadVolunteerRateFeat);
        var en = new ExplainedNumber(0.7f);
        _sut.ApplyVolunteerRespawnFeats(culture, ref en);
        Assert.AreEqual(0.7f * (1f + 0.2f), en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyVolunteerRespawnFeats_DolGuldurCulture_AddsTwentyPercent()
    {
        var culture = AdapterWith(DotsCulturalFeats.DolGuldurVolunteerRateFeat);
        var en = new ExplainedNumber(0.7f);
        _sut.ApplyVolunteerRespawnFeats(culture, ref en);
        Assert.AreEqual(0.7f * (1f + 0.2f), en.ResultNumber, 0.0001f);
    }

    // ── NotableSpawn ──────────────────────────────────────────────────

    [TestMethod]
    public void ApplyNotableCountFeat_NullCulture_ReturnsBaseCount()
    {
        Assert.AreEqual(2, _sut.ApplyNotableCountFeat(null, NotableOccupationKind.GangLeader, baseCount: 2));
    }

    [TestMethod]
    public void ApplyNotableCountFeat_ZeroBase_ReturnsZero()
    {
        // Vanilla returns 0 for unsupported (settlement, occupation) pairs (e.g. Preacher in towns).
        // We must NOT inflate 0 → 1 even with a positive feat.
        var culture = AdapterWith(DotsCulturalFeats.IsengardNotableCountTownGangLeaderFeat);
        Assert.AreEqual(0, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.GangLeader, baseCount: 0));
    }

    [TestMethod]
    public void ApplyNotableCountFeat_NonMatchingCulture_ReturnsBaseCount()
    {
        var culture = Substitute.For<ICultureFeatAdapter>();
        culture.HasFeat(Arg.Any<FeatObject>()).Returns(false);
        Assert.AreEqual(2, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.GangLeader, baseCount: 2));
        Assert.AreEqual(2, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.RuralNotable, baseCount: 2));
    }

    [TestMethod]
    public void ApplyNotableCountFeat_OtherOccupation_ReturnsBaseCount()
    {
        // Preacher / Soldier / Lord and anything else outside the spawn pool map to Other → no-op.
        var culture = AdapterWith(DotsCulturalFeats.IsengardNotableCountTownGangLeaderFeat);
        Assert.AreEqual(3, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.Other, baseCount: 3));
    }

    // ── Per-occupation town Add semantics ──

    [TestMethod]
    public void ApplyNotableCountFeat_IsengardMerchant_AddsTwo()
    {
        // Vanilla town Merchant = 2; Isengard merchant feat adds +2 = 4.
        var culture = AdapterWith(DotsCulturalFeats.IsengardNotableCountTownMerchantFeat);
        Assert.AreEqual(4, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.Merchant, baseCount: 2));
    }

    [TestMethod]
    public void ApplyNotableCountFeat_IsengardArtisan_AddsOne()
    {
        // Vanilla town Artisan = 1; Isengard artisan feat adds +1 = 2.
        var culture = AdapterWith(DotsCulturalFeats.IsengardNotableCountTownArtisanFeat);
        Assert.AreEqual(2, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.Artisan, baseCount: 1));
    }

    [TestMethod]
    public void ApplyNotableCountFeat_IsengardGangLeader_AddsTwelve()
    {
        // Vanilla town Gang Leader = 2; Isengard adds +12 → 14 (key target for Isengard's single town).
        var culture = AdapterWith(DotsCulturalFeats.IsengardNotableCountTownGangLeaderFeat);
        Assert.AreEqual(14, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.GangLeader, baseCount: 2));
    }

    [TestMethod]
    public void ApplyNotableCountFeat_DolGuldurMerchant_AddsOne()
    {
        var culture = AdapterWith(DotsCulturalFeats.DolGuldurNotableCountTownMerchantFeat);
        Assert.AreEqual(3, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.Merchant, baseCount: 2));
    }

    [TestMethod]
    public void ApplyNotableCountFeat_DolGuldurGangLeader_AddsThirteen()
    {
        // Vanilla 2 + 13 = 15 (Dol Guldur's shadow command center scale).
        var culture = AdapterWith(DotsCulturalFeats.DolGuldurNotableCountTownGangLeaderFeat);
        Assert.AreEqual(15, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.GangLeader, baseCount: 2));
    }

    [TestMethod]
    public void ApplyNotableCountFeat_DolGuldurArtisan_AddsOne()
    {
        var culture = AdapterWith(DotsCulturalFeats.DolGuldurNotableCountTownArtisanFeat);
        Assert.AreEqual(2, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.Artisan, baseCount: 1));
    }

    [TestMethod]
    public void ApplyNotableCountFeat_MordorGangLeader_AddsTwo()
    {
        var culture = AdapterWith(DotsCulturalFeats.MordorNotableCountTownGangLeaderFeat);
        Assert.AreEqual(4, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.GangLeader, baseCount: 2));
    }

    [TestMethod]
    public void ApplyNotableCountFeat_MordorOnlyGangLeader_DoesNotAffectMerchantSlot()
    {
        // Mordor only registers a Gang Leader feat — Merchant slot must stay vanilla.
        var culture = AdapterWith(DotsCulturalFeats.MordorNotableCountTownGangLeaderFeat);
        Assert.AreEqual(2, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.Merchant, baseCount: 2));
        Assert.AreEqual(1, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.Artisan, baseCount: 1));
    }

    [TestMethod]
    public void ApplyNotableCountFeat_GundabadGangLeader_AddsThree()
    {
        var culture = AdapterWith(DotsCulturalFeats.GundabadNotableCountTownGangLeaderFeat);
        Assert.AreEqual(5, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.GangLeader, baseCount: 2));
    }

    [TestMethod]
    public void ApplyNotableCountFeat_GundabadArtisan_AddsOne()
    {
        var culture = AdapterWith(DotsCulturalFeats.GundabadNotableCountTownArtisanFeat);
        Assert.AreEqual(2, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.Artisan, baseCount: 1));
    }

    // ── Village (legacy AddFactor + ceiling) ──

    [TestMethod]
    public void ApplyNotableCountFeat_IsengardVillage_RuralNotable_CeilingsTwoToThree()
    {
        // Vanilla village RuralNotable = 2; +10% → ceil(2.2) = 3.
        var culture = AdapterWith(DotsCulturalFeats.IsengardNotableCountVillageFeat);
        Assert.AreEqual(3, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.RuralNotable, baseCount: 2));
    }

    [TestMethod]
    public void ApplyNotableCountFeat_IsengardVillage_Headman_CeilingsOneToTwo()
    {
        // Vanilla village Headman = 1; +10% → ceil(1.1) = 2.
        var culture = AdapterWith(DotsCulturalFeats.IsengardNotableCountVillageFeat);
        Assert.AreEqual(2, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.Headman, baseCount: 1));
    }

    [TestMethod]
    public void ApplyNotableCountFeat_VillageFeat_DoesNotFireOnTownOccupation()
    {
        // Village-only feat must NOT fire on a town occupation even if the culture has it.
        var culture = AdapterWith(DotsCulturalFeats.IsengardNotableCountVillageFeat);
        Assert.AreEqual(2, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.GangLeader, baseCount: 2));
    }

    [TestMethod]
    public void ApplyNotableCountFeat_TownFeat_DoesNotFireOnVillageOccupation()
    {
        // Town gang-leader feat must NOT fire on RuralNotable / Headman slots.
        var culture = AdapterWith(DotsCulturalFeats.IsengardNotableCountTownGangLeaderFeat);
        Assert.AreEqual(2, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.RuralNotable, baseCount: 2));
        Assert.AreEqual(1, _sut.ApplyNotableCountFeat(culture, NotableOccupationKind.Headman, baseCount: 1));
    }

    // ── FoodConsumption ────────────────────────────────────────────────

    [TestMethod]
    public void ApplyFoodConsumptionFeats_LothlorienOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.LothlorienFoodConsumptionFeat);
        var en = new ExplainedNumber(10f);

        _sut.ApplyFoodConsumptionFeats(culture, ref en);

        Assert.AreEqual(
            10f * (1f + DotsCulturalFeats.LothlorienFoodConsumptionFeat.EffectBonus),
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplyFoodConsumptionFeats_DolGuldurOnly_IncreasesCost()
    {
        var culture = AdapterWith(DotsCulturalFeats.DolGuldurFoodConsumptionFeat);
        var en = new ExplainedNumber(10f);

        _sut.ApplyFoodConsumptionFeats(culture, ref en);

        Assert.AreEqual(
            10f * (1f + DotsCulturalFeats.DolGuldurFoodConsumptionFeat.EffectBonus),
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplyFoodConsumptionFeats_NullCulture_DoesNothing()
    {
        var en = new ExplainedNumber(10f);
        _sut.ApplyFoodConsumptionFeats(null, ref en);
        Assert.AreEqual(10f, en.ResultNumber);
    }

    // ── SettlementLoyalty ──────────────────────────────────────────────

    [TestMethod]
    public void ApplyLoyaltyFeats_GondorOnly_AddsBonus()
    {
        var culture = AdapterWith(DotsCulturalFeats.GondorLoyaltyFeat);
        var en = new ExplainedNumber(2f);

        _sut.ApplyLoyaltyFeats(culture, ref en);

        Assert.AreEqual(
            2f + DotsCulturalFeats.GondorLoyaltyFeat.EffectBonus,
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplyLoyaltyFeats_AllFiveStack_AddsSum()
    {
        var culture = AdapterWith(
            DotsCulturalFeats.GondorLoyaltyFeat,
            DotsCulturalFeats.EreborLoyaltyFeat,
            DotsCulturalFeats.LothlorienLoyaltyFeat,
            DotsCulturalFeats.RivendellLoyaltyFeat,
            DotsCulturalFeats.RohanLoyaltyFeat);
        var en = new ExplainedNumber(0f);

        _sut.ApplyLoyaltyFeats(culture, ref en);

        var expected =
            DotsCulturalFeats.GondorLoyaltyFeat.EffectBonus +
            DotsCulturalFeats.EreborLoyaltyFeat.EffectBonus +
            DotsCulturalFeats.LothlorienLoyaltyFeat.EffectBonus +
            DotsCulturalFeats.RivendellLoyaltyFeat.EffectBonus +
            DotsCulturalFeats.RohanLoyaltyFeat.EffectBonus;
        Assert.AreEqual(expected, en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyLoyaltyFeats_NullCulture_DoesNothing()
    {
        var en = new ExplainedNumber(2f);
        _sut.ApplyLoyaltyFeats(null, ref en);
        Assert.AreEqual(2f, en.ResultNumber);
    }

    // ── PartyMorale ────────────────────────────────────────────────────

    [TestMethod]
    public void ApplyMoraleFeats_GondorOnly_AddsBonus()
    {
        var culture = AdapterWith(DotsCulturalFeats.GondorMoraleFeat);
        var en = new ExplainedNumber(50f);

        _sut.ApplyMoraleFeats(culture, ref en);

        Assert.AreEqual(
            50f + DotsCulturalFeats.GondorMoraleFeat.EffectBonus,
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplyMoraleFeats_NullCulture_DoesNothing()
    {
        var en = new ExplainedNumber(50f);
        _sut.ApplyMoraleFeats(null, ref en);
        Assert.AreEqual(50f, en.ResultNumber);
    }

    // ── Smithing ───────────────────────────────────────────────────────

    [TestMethod]
    public void ApplySmithingFeats_EreborOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.EreborSmithingFeat);
        var en = new ExplainedNumber(100f);

        _sut.ApplySmithingFeats(culture, ref en);

        Assert.AreEqual(
            100f * (1f + DotsCulturalFeats.EreborSmithingFeat.EffectBonus),
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplySmithingFeats_NullCulture_DoesNothing()
    {
        var en = new ExplainedNumber(100f);
        _sut.ApplySmithingFeats(null, ref en);
        Assert.AreEqual(100f, en.ResultNumber);
    }

    // ── ClanFinance (tariffs) ──────────────────────────────────────────

    [TestMethod]
    public void ApplyTariffIncomeFeats_UmbarOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.UmbarTariffIncomeFeat);
        var en = new ExplainedNumber(500f);

        _sut.ApplyTariffIncomeFeats(culture, ref en);

        Assert.AreEqual(
            500f * (1f + DotsCulturalFeats.UmbarTariffIncomeFeat.EffectBonus),
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplyTariffIncomeFeats_NullCulture_DoesNothing()
    {
        var en = new ExplainedNumber(500f);
        _sut.ApplyTariffIncomeFeats(null, ref en);
        Assert.AreEqual(500f, en.ResultNumber);
    }

    // ── Raid ───────────────────────────────────────────────────────────

    [TestMethod]
    public void ApplyRaidDamageFeats_MordorOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.MordorRaidDamageFeat);
        var en = new ExplainedNumber(100f);

        _sut.ApplyRaidDamageFeats(culture, ref en);

        Assert.AreEqual(
            100f * (1f + DotsCulturalFeats.MordorRaidDamageFeat.EffectBonus),
            en.ResultNumber,
            0.0001f);
    }

    [TestMethod]
    public void ApplyRaidDamageFeats_NullCulture_DoesNothing()
    {
        var en = new ExplainedNumber(100f);
        _sut.ApplyRaidDamageFeats(null, ref en);
        Assert.AreEqual(100f, en.ResultNumber);
    }

    // ── Wave 1: Smithing (Mordor / Goblin / Misty Mountain Orcs) ──────

    [TestMethod]
    public void ApplySmithingFeats_MordorOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.MordorSmithingFeat);
        var en = new ExplainedNumber(100f);
        _sut.ApplySmithingFeats(culture, ref en);
        Assert.AreEqual(
            100f * (1f + DotsCulturalFeats.MordorSmithingFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplySmithingFeats_GoblinOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.GoblinSmithingFeat);
        var en = new ExplainedNumber(100f);
        _sut.ApplySmithingFeats(culture, ref en);
        Assert.AreEqual(
            100f * (1f + DotsCulturalFeats.GoblinSmithingFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplySmithingFeats_MistyMountainOrcsOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.MistyMountainOrcsSmithingFeat);
        var en = new ExplainedNumber(100f);
        _sut.ApplySmithingFeats(culture, ref en);
        Assert.AreEqual(
            100f * (1f + DotsCulturalFeats.MistyMountainOrcsSmithingFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    // ── Wave 1: Tariff income (Erebor / Dale / Khand) ─────────────────

    [TestMethod]
    public void ApplyTariffIncomeFeats_EreborOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.EreborTariffIncomeFeat);
        var en = new ExplainedNumber(500f);
        _sut.ApplyTariffIncomeFeats(culture, ref en);
        Assert.AreEqual(
            500f * (1f + DotsCulturalFeats.EreborTariffIncomeFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyTariffIncomeFeats_DaleOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.DaleTariffIncomeFeat);
        var en = new ExplainedNumber(500f);
        _sut.ApplyTariffIncomeFeats(culture, ref en);
        Assert.AreEqual(
            500f * (1f + DotsCulturalFeats.DaleTariffIncomeFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyTariffIncomeFeats_KhandOnly_ReducesIncome()
    {
        var culture = AdapterWith(DotsCulturalFeats.KhandTariffIncomeFeat);
        var en = new ExplainedNumber(500f);
        _sut.ApplyTariffIncomeFeats(culture, ref en);
        Assert.AreEqual(
            500f * (1f + DotsCulturalFeats.KhandTariffIncomeFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    // ── Wave 1: Raid damage (Umbar / Goblin / MMO / Harad / Rhun) ─────

    [TestMethod]
    public void ApplyRaidDamageFeats_UmbarOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.UmbarRaidDamageFeat);
        var en = new ExplainedNumber(100f);
        _sut.ApplyRaidDamageFeats(culture, ref en);
        Assert.AreEqual(
            100f * (1f + DotsCulturalFeats.UmbarRaidDamageFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyRaidDamageFeats_GoblinOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.GoblinRaidDamageFeat);
        var en = new ExplainedNumber(100f);
        _sut.ApplyRaidDamageFeats(culture, ref en);
        Assert.AreEqual(
            100f * (1f + DotsCulturalFeats.GoblinRaidDamageFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyRaidDamageFeats_MistyMountainOrcsOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.MistyMountainOrcsRaidDamageFeat);
        var en = new ExplainedNumber(100f);
        _sut.ApplyRaidDamageFeats(culture, ref en);
        Assert.AreEqual(
            100f * (1f + DotsCulturalFeats.MistyMountainOrcsRaidDamageFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyRaidDamageFeats_HaradOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.HaradRaidDamageFeat);
        var en = new ExplainedNumber(100f);
        _sut.ApplyRaidDamageFeats(culture, ref en);
        Assert.AreEqual(
            100f * (1f + DotsCulturalFeats.HaradRaidDamageFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyRaidDamageFeats_RhunOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.RhunRaidDamageFeat);
        var en = new ExplainedNumber(100f);
        _sut.ApplyRaidDamageFeats(culture, ref en);
        Assert.AreEqual(
            100f * (1f + DotsCulturalFeats.RhunRaidDamageFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    // ── Wave 1: Food consumption (Umbar / Khand / Harad) ──────────────

    [TestMethod]
    public void ApplyFoodConsumptionFeats_UmbarOnly_ReducesCost()
    {
        var culture = AdapterWith(DotsCulturalFeats.UmbarFoodConsumptionFeat);
        var en = new ExplainedNumber(10f);
        _sut.ApplyFoodConsumptionFeats(culture, ref en);
        Assert.AreEqual(
            10f * (1f + DotsCulturalFeats.UmbarFoodConsumptionFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyFoodConsumptionFeats_KhandOnly_ReducesCost()
    {
        var culture = AdapterWith(DotsCulturalFeats.KhandFoodConsumptionFeat);
        var en = new ExplainedNumber(10f);
        _sut.ApplyFoodConsumptionFeats(culture, ref en);
        Assert.AreEqual(
            10f * (1f + DotsCulturalFeats.KhandFoodConsumptionFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyFoodConsumptionFeats_HaradOnly_ReducesCost()
    {
        var culture = AdapterWith(DotsCulturalFeats.HaradFoodConsumptionFeat);
        var en = new ExplainedNumber(10f);
        _sut.ApplyFoodConsumptionFeats(culture, ref en);
        Assert.AreEqual(
            10f * (1f + DotsCulturalFeats.HaradFoodConsumptionFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    // ── Wave 1: Volunteer respawn (Lothlorien) ────────────────────────

    [TestMethod]
    public void ApplyVolunteerRespawnFeats_LothlorienCulture_ReducesRate()
    {
        var culture = AdapterWith(DotsCulturalFeats.LothlorienVolunteerRateFeat);
        var en = new ExplainedNumber(0.7f);
        _sut.ApplyVolunteerRespawnFeats(culture, ref en);
        Assert.AreEqual(
            0.7f * (1f + DotsCulturalFeats.LothlorienVolunteerRateFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    // ── Wave 1: Army influence cost (Mirkwood / Harad) ────────────────

    [TestMethod]
    public void ApplyArmyInfluenceCost_MirkwoodOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.MirkwoodArmyInfluenceCostFeat);
        var result = _sut.ApplyArmyInfluenceCost(culture, 100);
        var expected = (int)(100 * (1f + DotsCulturalFeats.MirkwoodArmyInfluenceCostFeat.EffectBonus));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void ApplyArmyInfluenceCost_HaradOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.HaradArmyInfluenceCostFeat);
        var result = _sut.ApplyArmyInfluenceCost(culture, 100);
        var expected = (int)(100 * (1f + DotsCulturalFeats.HaradArmyInfluenceCostFeat.EffectBonus));
        Assert.AreEqual(expected, result);
    }

    // ── Wave 1: Construction speed (Misty Mountain Orcs) ──────────────

    [TestMethod]
    public void ApplyConstructionSpeedFeats_MistyMountainOrcsOnly_ReducesSpeed()
    {
        var culture = AdapterWith(DotsCulturalFeats.MistyMountainOrcsConstructionSpeedFeat);
        var en = new ExplainedNumber(20f);
        _sut.ApplyConstructionSpeedFeats(culture, ref en);
        Assert.AreEqual(
            20f * (1f + DotsCulturalFeats.MistyMountainOrcsConstructionSpeedFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    // ── Wave 1: Battle renown (Dale / Khand) ──────────────────────────

    [TestMethod]
    public void ApplyRenownFeats_DaleOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.DaleRenownFeat);
        var en = new ExplainedNumber(50f);
        _sut.ApplyRenownFeats(culture, ref en);
        Assert.AreEqual(
            50f * (1f + DotsCulturalFeats.DaleRenownFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyRenownFeats_KhandOnly_AppliesFactor()
    {
        var culture = AdapterWith(DotsCulturalFeats.KhandRenownFeat);
        var en = new ExplainedNumber(50f);
        _sut.ApplyRenownFeats(culture, ref en);
        Assert.AreEqual(
            50f * (1f + DotsCulturalFeats.KhandRenownFeat.EffectBonus),
            en.ResultNumber, 0.0001f);
    }

    // ── Wave 1: Settlement loyalty (Dale / Rhun) ──────────────────────

    [TestMethod]
    public void ApplyLoyaltyFeats_DaleOnly_AddsBonus()
    {
        var culture = AdapterWith(DotsCulturalFeats.DaleLoyaltyFeat);
        var en = new ExplainedNumber(2f);
        _sut.ApplyLoyaltyFeats(culture, ref en);
        Assert.AreEqual(
            2f + DotsCulturalFeats.DaleLoyaltyFeat.EffectBonus,
            en.ResultNumber, 0.0001f);
    }

    [TestMethod]
    public void ApplyLoyaltyFeats_RhunOnly_AddsBonus()
    {
        var culture = AdapterWith(DotsCulturalFeats.RhunLoyaltyFeat);
        var en = new ExplainedNumber(2f);
        _sut.ApplyLoyaltyFeats(culture, ref en);
        Assert.AreEqual(
            2f + DotsCulturalFeats.RhunLoyaltyFeat.EffectBonus,
            en.ResultNumber, 0.0001f);
    }

    // ── Wave 1: Party morale (Harad) ──────────────────────────────────

    [TestMethod]
    public void ApplyMoraleFeats_HaradOnly_AddsBonus()
    {
        var culture = AdapterWith(DotsCulturalFeats.HaradMoraleFeat);
        var en = new ExplainedNumber(50f);
        _sut.ApplyMoraleFeats(culture, ref en);
        Assert.AreEqual(
            50f + DotsCulturalFeats.HaradMoraleFeat.EffectBonus,
            en.ResultNumber, 0.0001f);
    }

    // ── Wave 1: Party size (Khand) ────────────────────────────────────

    [TestMethod]
    public void ApplyPartySizeFeats_KhandOnly_AppliesFivePercent()
    {
        var culture = AdapterWith(DotsCulturalFeats.KhandPartySizeFeat);
        var en = new ExplainedNumber(100f);
        _sut.ApplyPartySizeFeats(culture, ref en);
        Assert.AreEqual(100f * (1f + 0.05f), en.ResultNumber, 0.0001f);
    }

    // ── Adapter helpers ────────────────────────────────────────────────

    /// <summary>
    /// Returns a substitute adapter that reports HasFeat==true for every
    /// FeatObject in <paramref name="present"/>, false for any other instance.
    /// </summary>
    private static ICultureFeatAdapter AdapterWith(params FeatObject[] present)
    {
        var adapter = Substitute.For<ICultureFeatAdapter>();
        adapter.HasFeat(Arg.Any<FeatObject>()).Returns(false);
        foreach (var feat in present)
            adapter.HasFeat(feat).Returns(true);
        return adapter;
    }

    /// <summary>
    /// We can't call <c>DotsCulturalFeats.CreateAndRegister()</c> in unit tests
    /// (no <c>Game.Current</c>), but we still need usable <see cref="FeatObject"/>
    /// references with a non-zero <c>EffectBonus</c> for assertions. This helper
    /// constructs a real FeatObject for every static getter on
    /// <see cref="DotsCulturalFeats"/>, mirrors the production EffectBonus values
    /// from <c>InitializeAll()</c>, and stuffs each into its matching backing
    /// field on the singleton instance.
    /// </summary>
    private static bool _featsInitialised;
    private static readonly object _featInitLock = new();

    private static void EnsureFeatsInitialised()
    {
        lock (_featInitLock)
        {
            if (_featsInitialised)
                return;

            var t = typeof(DotsCulturalFeats);
            var instanceField = t.GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            var instance = instanceField!.GetValue(null);
            if (instance == null)
            {
                // DotsCulturalFeats has no declared ctor — use the implicit public parameterless
                // ctor. Activator.CreateInstance reaches it irrespective of access modifier.
                instance = System.Activator.CreateInstance(t, nonPublic: true);
                instanceField.SetValue(null, instance);
            }

            // Map of (backing-field-name, stringId, effectBonus) matching InitializeAll().
            var entries = new (string field, string stringId, float bonus)[]
            {
                ("_ereborGarrisonWage", "dots_erebor_garrison_wage", -0.25f),
                ("_ereborProduction", "dots_erebor_production", 0.1f),
                ("_ereborConstructionSpeed", "dots_erebor_construction_speed", -0.15f),
                ("_ereborLoyalty", "dots_erebor_loyalty", 1f),
                ("_ereborMorale", "dots_erebor_morale", 5f),
                ("_ereborSmithing", "dots_erebor_smithing", -0.3f),

                ("_rivendellArmyInfluence", "dots_rivendell_army_influence", 0.35f),
                ("_rivendellHearthGrowth", "dots_rivendell_hearth_growth", 0.2f),
                ("_rivendellArmyInfluenceCost", "dots_rivendell_army_influence_cost", 0.25f),
                ("_rivendellFoodConsumption", "dots_rivendell_food_consumption", -0.15f),
                ("_rivendellLoyalty", "dots_rivendell_loyalty", 0.5f),

                ("_mirkwoodForestSpeed", "dots_mirkwood_forest_speed", 0.1f),
                ("_mirkwoodMilitiaProduction", "dots_mirkwood_militia_production", 0.25f),
                ("_mirkwoodHearthGrowth", "dots_mirkwood_hearth_growth", -0.2f),
                ("_mirkwoodFoodConsumption", "dots_mirkwood_food_consumption", -0.15f),
                ("_mirkwoodMorale", "dots_mirkwood_morale", 3f),

                ("_lothlorienForestSpeed", "dots_lothlorien_forest_speed", 0.1f),
                ("_lothlorienGarrisonWage", "dots_lothlorien_garrison_wage", -0.2f),
                ("_lothlorienConstructionSpeed", "dots_lothlorien_construction_speed", -0.1f),
                ("_lothlorienFoodConsumption", "dots_lothlorien_food_consumption", -0.15f),
                ("_lothlorienLoyalty", "dots_lothlorien_loyalty", 0.5f),
                ("_lothlorienMorale", "dots_lothlorien_morale", 3f),

                ("_isengardCheaperRecruits", "dots_isengard_cheaper_recruits", -0.15f),
                ("_isengardGarrisonWage", "dots_isengard_garrison_wage", -0.2f),
                ("_isengardDecisionPenalty", "dots_isengard_decision_penalty", 0.25f),
                ("_isengardPartySize", "dots_isengard_party_size", 0.2f),
                ("_isengardConstructionSpeed", "dots_isengard_construction_speed", 0.15f),
                ("_isengardSmithing", "dots_isengard_smithing", -0.2f),
                ("_isengardRaidDamage", "dots_isengard_raid_damage", 0.2f),

                ("_gundabadArmyInfluenceCost", "dots_gundabad_army_influence_cost", -0.4f),
                ("_gundabadGrainProduction", "dots_gundabad_grain_production", 0.15f),
                ("_gundabadWage", "dots_gundabad_wage", 0.1f),
                ("_gundabadPartySize", "dots_gundabad_party_size", 0.2f),
                ("_gundabadRaidDamage", "dots_gundabad_raid_damage", 0.25f),

                ("_umbarCheaperCaravans", "dots_umbar_cheaper_caravans", -0.25f),
                ("_umbarRenown", "dots_umbar_renown", 0.08f),
                ("_umbarWage", "dots_umbar_wage", 0.08f),
                ("_umbarTariffIncome", "dots_umbar_tariff_income", 0.15f),

                ("_dolguldurArmyInfluenceCost", "dots_dolguldur_army_influence_cost", -0.5f),
                ("_dolguldurMilitiaProduction", "dots_dolguldur_militia_production", 0.2f),
                ("_dolguldurConstructionSpeed", "dots_dolguldur_construction_speed", -0.2f),
                ("_dolguldurPartySize", "dots_dolguldur_party_size", 0.2f),
                ("_dolguldurFoodConsumption", "dots_dolguldur_food_consumption", 0.1f),

                ("_gondorGarrisonWage", "dots_gondor_garrison_wage", -0.2f),
                ("_gondorArmyInfluence", "dots_gondor_army_influence", 0.3f),
                ("_gondorHearthGrowth", "dots_gondor_hearth_growth", -0.15f),
                ("_gondorPartySize", "dots_gondor_party_size", 0.025f),
                ("_gondorLoyalty", "dots_gondor_loyalty", 1f),
                ("_gondorMorale", "dots_gondor_morale", 5f),

                ("_mordorArmyInfluenceCost", "dots_mordor_army_influence_cost", -0.6f),
                ("_mordorGrainProduction", "dots_mordor_grain_production", 0.2f),
                ("_mordorWage", "dots_mordor_wage", 0.2f),
                ("_mordorPartySize", "dots_mordor_party_size", 0.1f),
                ("_mordorRaidDamage", "dots_mordor_raid_damage", 0.25f),

                ("_rohanMountedCost", "dots_rohan_mounted_cost", -0.15f),
                ("_rohanMountedWage", "dots_rohan_mounted_wage", -0.15f),
                ("_rohanInfantrySpeed", "dots_rohan_infantry_speed", -0.1f),
                ("_rohanLoyalty", "dots_rohan_loyalty", 0.5f),
                ("_rohanMorale", "dots_rohan_morale", 5f),

                // Terrain movement-speed feats (issue: cultural terrain bonuses)
                ("_ereborSnowSpeed", "dots_erebor_snow_speed", 0.1f),
                ("_rivendellForestSpeed", "dots_rivendell_forest_speed", 0.1f),
                ("_isengardPlainSpeed", "dots_isengard_plain_speed", 0.1f),
                ("_isengardSwampSpeed", "dots_isengard_swamp_speed", 0.1f),
                ("_gundabadSnowSpeed", "dots_gundabad_snow_speed", 0.1f),
                ("_umbarDesertSpeed", "dots_umbar_desert_speed", 0.1f),
                ("_gondorPlainSpeed", "dots_gondor_plain_speed", 0.1f),
                ("_mordorPlainSpeed", "dots_mordor_plain_speed", 0.05f),
                ("_mordorSwampSpeed", "dots_mordor_swamp_speed", 0.05f),
                ("_mordorNightSpeed", "dots_mordor_night_speed", 0.1f),
                ("_rohanPlainSpeed", "dots_rohan_plain_speed", 0.1f),
                ("_dalePlainSpeed", "dots_dale_plain_speed", 0.1f),
                ("_khandSteppeSpeed", "dots_khand_steppe_speed", 0.1f),
                ("_rhunSteppeSpeed", "dots_rhun_steppe_speed", 0.1f),
                ("_haradDesertSpeed", "dots_harad_desert_speed", 0.1f),
                ("_dunlandPlainSpeed", "dots_dunland_plain_speed", 0.1f),
                ("_shaghanaDesertSpeed", "dots_shaghana_desert_speed", 0.1f),
                ("_abanissaDesertSpeed", "dots_abanissa_desert_speed", 0.1f),

                // New party-size feats (3) — Dunland/Rhun/Harad +5%
                ("_dunlandPartySize", "dots_dunland_party_size", 0.05f),
                ("_rhunPartySize", "dots_rhun_party_size", 0.05f),
                ("_haradPartySize", "dots_harad_party_size", 0.05f),

                // Volunteer respawn rate (4)
                ("_dunlandVolunteerRate", "dots_dunland_volunteer_rate", 0.1f),
                ("_gundabadVolunteerRate", "dots_gundabad_volunteer_rate", 0.2f),
                ("_dolguldurVolunteerRate", "dots_dolguldur_volunteer_rate", 0.2f),
                ("_mordorVolunteerRate", "dots_mordor_volunteer_rate", 0.2f),

                // Notable count: per-occupation town (Add semantics) + per-(culture, village)
                // (AddFactor semantics, unchanged). 9 town + 4 village = 13 feats.
                ("_isengardNotableCountTownMerchant", "dots_isengard_notable_count_town_merchant", 2f),
                ("_isengardNotableCountTownArtisan", "dots_isengard_notable_count_town_artisan", 1f),
                ("_isengardNotableCountTownGangLeader", "dots_isengard_notable_count_town_gang_leader", 12f),
                ("_isengardNotableCountVillage", "dots_isengard_notable_count_village", 0.1f),
                ("_dolguldurNotableCountTownMerchant", "dots_dolguldur_notable_count_town_merchant", 1f),
                ("_dolguldurNotableCountTownArtisan", "dots_dolguldur_notable_count_town_artisan", 1f),
                ("_dolguldurNotableCountTownGangLeader", "dots_dolguldur_notable_count_town_gang_leader", 13f),
                ("_dolguldurNotableCountVillage", "dots_dolguldur_notable_count_village", 0.1f),
                ("_mordorNotableCountTownGangLeader", "dots_mordor_notable_count_town_gang_leader", 2f),
                ("_mordorNotableCountVillage", "dots_mordor_notable_count_village", 0.05f),
                ("_gundabadNotableCountTownArtisan", "dots_gundabad_notable_count_town_artisan", 1f),
                ("_gundabadNotableCountTownGangLeader", "dots_gundabad_notable_count_town_gang_leader", 3f),
                ("_gundabadNotableCountVillage", "dots_gundabad_notable_count_village", 0.1f),

                // Wave 1 economy/military feats (24)
                ("_mordorSmithing", "dots_mordor_smithing", -0.15f),
                ("_ereborTariffIncome", "dots_erebor_tariff_income", 0.05f),
                ("_umbarRaidDamage", "dots_umbar_raid_damage", 0.2f),
                ("_umbarFoodConsumption", "dots_umbar_food_consumption", -0.1f),
                ("_lothlorienVolunteerRate", "dots_lothlorien_volunteer_rate", -0.15f),
                ("_mirkwoodArmyInfluenceCost", "dots_mirkwood_army_influence_cost", 0.15f),
                ("_goblinSmithing", "dots_goblin_smithing", -0.1f),
                ("_goblinRaidDamage", "dots_goblin_raid_damage", 0.1f),
                ("_mistyMountainOrcsSmithing", "dots_mistymountainorcs_smithing", -0.15f),
                ("_mistyMountainOrcsRaidDamage", "dots_mistymountainorcs_raid_damage", 0.15f),
                ("_mistyMountainOrcsConstructionSpeed", "dots_mistymountainorcs_construction_speed", -0.1f),
                ("_daleTariffIncome", "dots_dale_tariff_income", 0.1f),
                ("_daleRenown", "dots_dale_renown", 0.1f),
                ("_daleLoyalty", "dots_dale_loyalty", -0.5f),
                ("_khandRenown", "dots_khand_renown", 0.08f),
                ("_khandTariffIncome", "dots_khand_tariff_income", -0.1f),
                ("_khandFoodConsumption", "dots_khand_food_consumption", -0.1f),
                ("_khandPartySize", "dots_khand_party_size", 0.05f),
                ("_haradMorale", "dots_harad_morale", 5f),
                ("_haradFoodConsumption", "dots_harad_food_consumption", -0.15f),
                ("_haradRaidDamage", "dots_harad_raid_damage", 0.15f),
                ("_haradArmyInfluenceCost", "dots_harad_army_influence_cost", 0.15f),
                ("_rhunLoyalty", "dots_rhun_loyalty", -0.5f),
                ("_rhunRaidDamage", "dots_rhun_raid_damage", 0.15f),
            };

            var effectBonusProp = typeof(FeatObject).GetProperty(
                nameof(FeatObject.EffectBonus),
                BindingFlags.Public | BindingFlags.Instance);
            var effectBonusSetter = effectBonusProp!.GetSetMethod(nonPublic: true);

            foreach (var (fieldName, stringId, bonus) in entries)
            {
                var feat = new FeatObject(stringId);
                effectBonusSetter!.Invoke(feat, new object[] { bonus });
                var f = t.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                f!.SetValue(instance, feat);
            }

            _featsInitialised = true;
        }
    }
}
