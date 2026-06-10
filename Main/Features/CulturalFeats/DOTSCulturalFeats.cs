using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace DOTS.Features.CulturalFeats;

public class DotsCulturalFeats
{
    private static DotsCulturalFeats _instance;

    private static DotsCulturalFeats Instance => _instance
        ?? throw new InvalidOperationException(
            "DotsCulturalFeats not initialized. Ensure Patch18_CulturalFeats is registered.");

    // Erebor
    private FeatObject _ereborGarrisonWage;
    private FeatObject _ereborProduction;
    private FeatObject _ereborConstructionSpeed;
    private FeatObject _ereborLoyalty;
    private FeatObject _ereborMorale;
    private FeatObject _ereborSmithing;
    private FeatObject _ereborSnowSpeed;
    private FeatObject _ereborTariffIncome;

    // Rivendell
    private FeatObject _rivendellArmyInfluence;
    private FeatObject _rivendellHearthGrowth;
    private FeatObject _rivendellArmyInfluenceCost;
    private FeatObject _rivendellFoodConsumption;
    private FeatObject _rivendellLoyalty;
    private FeatObject _rivendellForestSpeed;

    // Mirkwood
    private FeatObject _mirkwoodForestSpeed;
    private FeatObject _mirkwoodMilitiaProduction;
    private FeatObject _mirkwoodHearthGrowth;
    private FeatObject _mirkwoodFoodConsumption;
    private FeatObject _mirkwoodMorale;
    private FeatObject _mirkwoodArmyInfluenceCost;

    // Lothlorien
    private FeatObject _lothlorienForestSpeed;
    private FeatObject _lothlorienGarrisonWage;
    private FeatObject _lothlorienConstructionSpeed;
    private FeatObject _lothlorienFoodConsumption;
    private FeatObject _lothlorienLoyalty;
    private FeatObject _lothlorienMorale;
    private FeatObject _lothlorienVolunteerRate;

    // Isengard
    private FeatObject _isengardCheaperRecruits;
    private FeatObject _isengardGarrisonWage;
    private FeatObject _isengardDecisionPenalty;
    private FeatObject _isengardPartySize;
    private FeatObject _isengardConstructionSpeed;
    private FeatObject _isengardSmithing;
    private FeatObject _isengardRaidDamage;
    private FeatObject _isengardPlainSpeed;
    private FeatObject _isengardSwampSpeed;
    // Per-occupation town notable feats (AdditionType.Add); enables asymmetric distributions
    // that a uniform per-(culture, town) multiplier can't express ("3 Merchants, 14 Gang Leaders").
    private FeatObject _isengardNotableCountTownMerchant;
    private FeatObject _isengardNotableCountTownArtisan;
    private FeatObject _isengardNotableCountTownGangLeader;
    private FeatObject _isengardNotableCountVillage;

    // Gundabad
    private FeatObject _gundabadArmyInfluenceCost;
    private FeatObject _gundabadGrainProduction;
    private FeatObject _gundabadWage;
    private FeatObject _gundabadPartySize;
    private FeatObject _gundabadRaidDamage;
    private FeatObject _gundabadSnowSpeed;
    private FeatObject _gundabadVolunteerRate;
    // Per-occupation town notable feats (AdditionType.Add); only Artisan and GangLeader for Gundabad.
    private FeatObject _gundabadNotableCountTownArtisan;
    private FeatObject _gundabadNotableCountTownGangLeader;
    private FeatObject _gundabadNotableCountVillage;
    // Goblins (Goblin Town) — vast weak swarms, snow-bred, ravenous (food penalty)
    private FeatObject _goblinPartySize;
    private FeatObject _goblinVolunteerRate;
    private FeatObject _goblinSnowSpeed;
    private FeatObject _goblinFoodConsumption;
    private FeatObject _goblinSmithing;
    private FeatObject _goblinRaidDamage;
    // Misty Mountain Orcs — large weak war-host, snow-bred, hungry (food penalty)
    private FeatObject _mistyMountainOrcsArmyInfluenceCost;
    private FeatObject _mistyMountainOrcsPartySize;
    private FeatObject _mistyMountainOrcsSnowSpeed;
    private FeatObject _mistyMountainOrcsFoodConsumption;
    private FeatObject _mistyMountainOrcsSmithing;
    private FeatObject _mistyMountainOrcsRaidDamage;
    private FeatObject _mistyMountainOrcsConstructionSpeed;

    // Umbar
    private FeatObject _umbarCheaperCaravans;
    private FeatObject _umbarRenown;
    private FeatObject _umbarWage;
    private FeatObject _umbarTariffIncome;
    private FeatObject _umbarDesertSpeed;
    private FeatObject _umbarRaidDamage;
    private FeatObject _umbarFoodConsumption;

    // Dol Guldur
    private FeatObject _dolguldurArmyInfluenceCost;
    private FeatObject _dolguldurMilitiaProduction;
    private FeatObject _dolguldurConstructionSpeed;
    private FeatObject _dolguldurPartySize;
    private FeatObject _dolguldurFoodConsumption;
    private FeatObject _dolguldurVolunteerRate;
    // Per-occupation town notable feats (AdditionType.Add); Dol Guldur targets ~20 total town
    // notables, Gang-Leader-heavy to match Isengard's recruitment competitiveness vs Rohan.
    private FeatObject _dolguldurNotableCountTownMerchant;
    private FeatObject _dolguldurNotableCountTownArtisan;
    private FeatObject _dolguldurNotableCountTownGangLeader;
    private FeatObject _dolguldurNotableCountVillage;

    // Gondor
    private FeatObject _gondorGarrisonWage;
    private FeatObject _gondorArmyInfluence;
    private FeatObject _gondorHearthGrowth;
    private FeatObject _gondorPartySize;
    private FeatObject _gondorLoyalty;
    private FeatObject _gondorMorale;
    private FeatObject _gondorPlainSpeed;

    // Mordor
    private FeatObject _mordorArmyInfluenceCost;
    private FeatObject _mordorGrainProduction;
    private FeatObject _mordorWage;
    private FeatObject _mordorPartySize;
    private FeatObject _mordorRaidDamage;
    private FeatObject _mordorPlainSpeed;
    private FeatObject _mordorSwampSpeed;
    private FeatObject _mordorNightSpeed;
    private FeatObject _mordorVolunteerRate;
    private FeatObject _mordorSmithing;
    // Per-occupation town notable feats (AdditionType.Add); Mordor only boosts Gang Leaders.
    private FeatObject _mordorNotableCountTownGangLeader;
    private FeatObject _mordorNotableCountVillage;

    // Rohan (XSLT culture — custom C# feats)
    private FeatObject _rohanMountedCost;
    private FeatObject _rohanMountedWage;
    private FeatObject _rohanInfantrySpeed;
    private FeatObject _rohanLoyalty;
    private FeatObject _rohanMorale;
    private FeatObject _rohanPlainSpeed;

    // Dale (XSLT culture — custom C# feats)
    private FeatObject _dalePlainSpeed;
    private FeatObject _daleTariffIncome;
    private FeatObject _daleRenown;
    private FeatObject _daleLoyalty;

    // Khand (XSLT culture — custom C# feats)
    private FeatObject _khandSteppeSpeed;
    private FeatObject _khandRenown;
    private FeatObject _khandTariffIncome;
    private FeatObject _khandFoodConsumption;
    private FeatObject _khandPartySize;

    // Rhun (XSLT culture — custom C# feats)
    private FeatObject _rhunSteppeSpeed;
    private FeatObject _rhunPartySize;
    private FeatObject _rhunLoyalty;
    private FeatObject _rhunRaidDamage;

    // Harad (XSLT culture — custom C# feats)
    private FeatObject _haradDesertSpeed;
    private FeatObject _haradPartySize;
    private FeatObject _haradMorale;
    private FeatObject _haradFoodConsumption;
    private FeatObject _haradRaidDamage;
    private FeatObject _haradArmyInfluenceCost;

    // Dunland (XSLT culture — custom C# feats)
    private FeatObject _dunlandPlainSpeed;
    private FeatObject _dunlandPartySize;
    private FeatObject _dunlandVolunteerRate;

    // Shaghana
    private FeatObject _shaghanaDesertSpeed;

    // Abanissa
    private FeatObject _abanissaDesertSpeed;

    // Erebor
    public static FeatObject EreborGarrisonWageFeat => Instance._ereborGarrisonWage;
    public static FeatObject EreborProductionFeat => Instance._ereborProduction;
    public static FeatObject EreborConstructionSpeedFeat => Instance._ereborConstructionSpeed;
    public static FeatObject EreborLoyaltyFeat => Instance._ereborLoyalty;
    public static FeatObject EreborMoraleFeat => Instance._ereborMorale;
    public static FeatObject EreborSmithingFeat => Instance._ereborSmithing;
    public static FeatObject EreborSnowSpeedFeat => Instance._ereborSnowSpeed;
    public static FeatObject EreborTariffIncomeFeat => Instance._ereborTariffIncome;

    // Rivendell
    public static FeatObject RivendellArmyInfluenceFeat => Instance._rivendellArmyInfluence;
    public static FeatObject RivendellHearthGrowthFeat => Instance._rivendellHearthGrowth;
    public static FeatObject RivendellArmyInfluenceCostFeat => Instance._rivendellArmyInfluenceCost;
    public static FeatObject RivendellFoodConsumptionFeat => Instance._rivendellFoodConsumption;
    public static FeatObject RivendellLoyaltyFeat => Instance._rivendellLoyalty;
    public static FeatObject RivendellForestSpeedFeat => Instance._rivendellForestSpeed;

    // Mirkwood
    public static FeatObject MirkwoodForestSpeedFeat => Instance._mirkwoodForestSpeed;
    public static FeatObject MirkwoodMilitiaProductionFeat => Instance._mirkwoodMilitiaProduction;
    public static FeatObject MirkwoodHearthGrowthFeat => Instance._mirkwoodHearthGrowth;
    public static FeatObject MirkwoodFoodConsumptionFeat => Instance._mirkwoodFoodConsumption;
    public static FeatObject MirkwoodMoraleFeat => Instance._mirkwoodMorale;
    public static FeatObject MirkwoodArmyInfluenceCostFeat => Instance._mirkwoodArmyInfluenceCost;

    // Lothlorien
    public static FeatObject LothlorienForestSpeedFeat => Instance._lothlorienForestSpeed;
    public static FeatObject LothlorienGarrisonWageFeat => Instance._lothlorienGarrisonWage;
    public static FeatObject LothlorienConstructionSpeedFeat => Instance._lothlorienConstructionSpeed;
    public static FeatObject LothlorienFoodConsumptionFeat => Instance._lothlorienFoodConsumption;
    public static FeatObject LothlorienLoyaltyFeat => Instance._lothlorienLoyalty;
    public static FeatObject LothlorienMoraleFeat => Instance._lothlorienMorale;
    public static FeatObject LothlorienVolunteerRateFeat => Instance._lothlorienVolunteerRate;

    // Isengard
    public static FeatObject IsengardCheaperRecruitsFeat => Instance._isengardCheaperRecruits;
    public static FeatObject IsengardGarrisonWageFeat => Instance._isengardGarrisonWage;
    public static FeatObject IsengardDecisionPenaltyFeat => Instance._isengardDecisionPenalty;
    public static FeatObject IsengardPartySizeFeat => Instance._isengardPartySize;
    public static FeatObject IsengardConstructionSpeedFeat => Instance._isengardConstructionSpeed;
    public static FeatObject IsengardSmithingFeat => Instance._isengardSmithing;
    public static FeatObject IsengardRaidDamageFeat => Instance._isengardRaidDamage;
    public static FeatObject IsengardPlainSpeedFeat => Instance._isengardPlainSpeed;
    public static FeatObject IsengardSwampSpeedFeat => Instance._isengardSwampSpeed;
    public static FeatObject IsengardNotableCountTownMerchantFeat => Instance._isengardNotableCountTownMerchant;
    public static FeatObject IsengardNotableCountTownArtisanFeat => Instance._isengardNotableCountTownArtisan;
    public static FeatObject IsengardNotableCountTownGangLeaderFeat => Instance._isengardNotableCountTownGangLeader;
    public static FeatObject IsengardNotableCountVillageFeat => Instance._isengardNotableCountVillage;

    // Gundabad
    public static FeatObject GundabadArmyInfluenceCostFeat => Instance._gundabadArmyInfluenceCost;
    public static FeatObject GundabadGrainProductionFeat => Instance._gundabadGrainProduction;
    public static FeatObject GundabadWageFeat => Instance._gundabadWage;
    public static FeatObject GundabadPartySizeFeat => Instance._gundabadPartySize;
    public static FeatObject GundabadRaidDamageFeat => Instance._gundabadRaidDamage;
    public static FeatObject GundabadSnowSpeedFeat => Instance._gundabadSnowSpeed;
    public static FeatObject GundabadVolunteerRateFeat => Instance._gundabadVolunteerRate;
    public static FeatObject GundabadNotableCountTownArtisanFeat => Instance._gundabadNotableCountTownArtisan;
    public static FeatObject GundabadNotableCountTownGangLeaderFeat => Instance._gundabadNotableCountTownGangLeader;
    public static FeatObject GundabadNotableCountVillageFeat => Instance._gundabadNotableCountVillage;
    // Goblins
    public static FeatObject GoblinPartySizeFeat => Instance._goblinPartySize;
    public static FeatObject GoblinVolunteerRateFeat => Instance._goblinVolunteerRate;
    public static FeatObject GoblinSnowSpeedFeat => Instance._goblinSnowSpeed;
    public static FeatObject GoblinFoodConsumptionFeat => Instance._goblinFoodConsumption;
    public static FeatObject GoblinSmithingFeat => Instance._goblinSmithing;
    public static FeatObject GoblinRaidDamageFeat => Instance._goblinRaidDamage;
    // Misty Mountain Orcs
    public static FeatObject MistyMountainOrcsArmyInfluenceCostFeat => Instance._mistyMountainOrcsArmyInfluenceCost;
    public static FeatObject MistyMountainOrcsPartySizeFeat => Instance._mistyMountainOrcsPartySize;
    public static FeatObject MistyMountainOrcsSnowSpeedFeat => Instance._mistyMountainOrcsSnowSpeed;
    public static FeatObject MistyMountainOrcsFoodConsumptionFeat => Instance._mistyMountainOrcsFoodConsumption;
    public static FeatObject MistyMountainOrcsSmithingFeat => Instance._mistyMountainOrcsSmithing;
    public static FeatObject MistyMountainOrcsRaidDamageFeat => Instance._mistyMountainOrcsRaidDamage;
    public static FeatObject MistyMountainOrcsConstructionSpeedFeat => Instance._mistyMountainOrcsConstructionSpeed;

    // Umbar
    public static FeatObject UmbarCheaperCaravansFeat => Instance._umbarCheaperCaravans;
    public static FeatObject UmbarRenownFeat => Instance._umbarRenown;
    public static FeatObject UmbarWageFeat => Instance._umbarWage;
    public static FeatObject UmbarTariffIncomeFeat => Instance._umbarTariffIncome;
    public static FeatObject UmbarDesertSpeedFeat => Instance._umbarDesertSpeed;
    public static FeatObject UmbarRaidDamageFeat => Instance._umbarRaidDamage;
    public static FeatObject UmbarFoodConsumptionFeat => Instance._umbarFoodConsumption;

    // Dol Guldur
    public static FeatObject DolGuldurArmyInfluenceCostFeat => Instance._dolguldurArmyInfluenceCost;
    public static FeatObject DolGuldurMilitiaProductionFeat => Instance._dolguldurMilitiaProduction;
    public static FeatObject DolGuldurConstructionSpeedFeat => Instance._dolguldurConstructionSpeed;
    public static FeatObject DolGuldurPartySizeFeat => Instance._dolguldurPartySize;
    public static FeatObject DolGuldurFoodConsumptionFeat => Instance._dolguldurFoodConsumption;
    public static FeatObject DolGuldurVolunteerRateFeat => Instance._dolguldurVolunteerRate;
    public static FeatObject DolGuldurNotableCountTownMerchantFeat => Instance._dolguldurNotableCountTownMerchant;
    public static FeatObject DolGuldurNotableCountTownArtisanFeat => Instance._dolguldurNotableCountTownArtisan;
    public static FeatObject DolGuldurNotableCountTownGangLeaderFeat => Instance._dolguldurNotableCountTownGangLeader;
    public static FeatObject DolGuldurNotableCountVillageFeat => Instance._dolguldurNotableCountVillage;

    // Gondor
    public static FeatObject GondorGarrisonWageFeat => Instance._gondorGarrisonWage;
    public static FeatObject GondorArmyInfluenceFeat => Instance._gondorArmyInfluence;
    public static FeatObject GondorHearthGrowthFeat => Instance._gondorHearthGrowth;
    public static FeatObject GondorPartySizeFeat => Instance._gondorPartySize;
    public static FeatObject GondorLoyaltyFeat => Instance._gondorLoyalty;
    public static FeatObject GondorMoraleFeat => Instance._gondorMorale;
    public static FeatObject GondorPlainSpeedFeat => Instance._gondorPlainSpeed;

    // Mordor
    public static FeatObject MordorArmyInfluenceCostFeat => Instance._mordorArmyInfluenceCost;
    public static FeatObject MordorGrainProductionFeat => Instance._mordorGrainProduction;
    public static FeatObject MordorWageFeat => Instance._mordorWage;
    public static FeatObject MordorPartySizeFeat => Instance._mordorPartySize;
    public static FeatObject MordorRaidDamageFeat => Instance._mordorRaidDamage;
    public static FeatObject MordorPlainSpeedFeat => Instance._mordorPlainSpeed;
    public static FeatObject MordorSwampSpeedFeat => Instance._mordorSwampSpeed;
    public static FeatObject MordorNightSpeedFeat => Instance._mordorNightSpeed;
    public static FeatObject MordorVolunteerRateFeat => Instance._mordorVolunteerRate;
    public static FeatObject MordorSmithingFeat => Instance._mordorSmithing;
    public static FeatObject MordorNotableCountTownGangLeaderFeat => Instance._mordorNotableCountTownGangLeader;
    public static FeatObject MordorNotableCountVillageFeat => Instance._mordorNotableCountVillage;

    // Rohan
    public static FeatObject RohanMountedCostFeat => Instance._rohanMountedCost;
    public static FeatObject RohanMountedWageFeat => Instance._rohanMountedWage;
    public static FeatObject RohanInfantrySpeedFeat => Instance._rohanInfantrySpeed;
    public static FeatObject RohanLoyaltyFeat => Instance._rohanLoyalty;
    public static FeatObject RohanMoraleFeat => Instance._rohanMorale;
    public static FeatObject RohanPlainSpeedFeat => Instance._rohanPlainSpeed;

    // Dale
    public static FeatObject DalePlainSpeedFeat => Instance._dalePlainSpeed;
    public static FeatObject DaleTariffIncomeFeat => Instance._daleTariffIncome;
    public static FeatObject DaleRenownFeat => Instance._daleRenown;
    public static FeatObject DaleLoyaltyFeat => Instance._daleLoyalty;

    // Khand
    public static FeatObject KhandSteppeSpeedFeat => Instance._khandSteppeSpeed;
    public static FeatObject KhandRenownFeat => Instance._khandRenown;
    public static FeatObject KhandTariffIncomeFeat => Instance._khandTariffIncome;
    public static FeatObject KhandFoodConsumptionFeat => Instance._khandFoodConsumption;
    public static FeatObject KhandPartySizeFeat => Instance._khandPartySize;

    // Rhun
    public static FeatObject RhunSteppeSpeedFeat => Instance._rhunSteppeSpeed;
    public static FeatObject RhunPartySizeFeat => Instance._rhunPartySize;
    public static FeatObject RhunLoyaltyFeat => Instance._rhunLoyalty;
    public static FeatObject RhunRaidDamageFeat => Instance._rhunRaidDamage;

    // Harad
    public static FeatObject HaradDesertSpeedFeat => Instance._haradDesertSpeed;
    public static FeatObject HaradPartySizeFeat => Instance._haradPartySize;
    public static FeatObject HaradMoraleFeat => Instance._haradMorale;
    public static FeatObject HaradFoodConsumptionFeat => Instance._haradFoodConsumption;
    public static FeatObject HaradRaidDamageFeat => Instance._haradRaidDamage;
    public static FeatObject HaradArmyInfluenceCostFeat => Instance._haradArmyInfluenceCost;

    // Dunland
    public static FeatObject DunlandPlainSpeedFeat => Instance._dunlandPlainSpeed;
    public static FeatObject DunlandPartySizeFeat => Instance._dunlandPartySize;
    public static FeatObject DunlandVolunteerRateFeat => Instance._dunlandVolunteerRate;

    // Shaghana
    public static FeatObject ShaghanaDesertSpeedFeat => Instance._shaghanaDesertSpeed;

    // Abanissa
    public static FeatObject AbanissaDesertSpeedFeat => Instance._abanissaDesertSpeed;

    public static void CreateAndRegister()
    {
        _instance = new DotsCulturalFeats();
        _instance.RegisterAll();
        _instance.InitializeAll();
    }

    internal static void Reset() => _instance = null;

    private void RegisterAll()
    {
        _ereborGarrisonWage = Register("dots_erebor_garrison_wage");
        _ereborProduction = Register("dots_erebor_production");
        _ereborConstructionSpeed = Register("dots_erebor_construction_speed");
        _ereborLoyalty = Register("dots_erebor_loyalty");
        _ereborMorale = Register("dots_erebor_morale");
        _ereborSmithing = Register("dots_erebor_smithing");
        _ereborSnowSpeed = Register("dots_erebor_snow_speed");
        _ereborTariffIncome = Register("dots_erebor_tariff_income");

        _rivendellArmyInfluence = Register("dots_rivendell_army_influence");
        _rivendellHearthGrowth = Register("dots_rivendell_hearth_growth");
        _rivendellArmyInfluenceCost = Register("dots_rivendell_army_influence_cost");
        _rivendellFoodConsumption = Register("dots_rivendell_food_consumption");
        _rivendellLoyalty = Register("dots_rivendell_loyalty");
        _rivendellForestSpeed = Register("dots_rivendell_forest_speed");

        _mirkwoodForestSpeed = Register("dots_mirkwood_forest_speed");
        _mirkwoodMilitiaProduction = Register("dots_mirkwood_militia_production");
        _mirkwoodHearthGrowth = Register("dots_mirkwood_hearth_growth");
        _mirkwoodFoodConsumption = Register("dots_mirkwood_food_consumption");
        _mirkwoodMorale = Register("dots_mirkwood_morale");
        _mirkwoodArmyInfluenceCost = Register("dots_mirkwood_army_influence_cost");

        _lothlorienForestSpeed = Register("dots_lothlorien_forest_speed");
        _lothlorienGarrisonWage = Register("dots_lothlorien_garrison_wage");
        _lothlorienConstructionSpeed = Register("dots_lothlorien_construction_speed");
        _lothlorienFoodConsumption = Register("dots_lothlorien_food_consumption");
        _lothlorienLoyalty = Register("dots_lothlorien_loyalty");
        _lothlorienMorale = Register("dots_lothlorien_morale");
        _lothlorienVolunteerRate = Register("dots_lothlorien_volunteer_rate");

        _isengardCheaperRecruits = Register("dots_isengard_cheaper_recruits");
        _isengardGarrisonWage = Register("dots_isengard_garrison_wage");
        _isengardDecisionPenalty = Register("dots_isengard_decision_penalty");
        _isengardPartySize = Register("dots_isengard_party_size");
        _isengardConstructionSpeed = Register("dots_isengard_construction_speed");
        _isengardSmithing = Register("dots_isengard_smithing");
        _isengardRaidDamage = Register("dots_isengard_raid_damage");
        _isengardPlainSpeed = Register("dots_isengard_plain_speed");
        _isengardSwampSpeed = Register("dots_isengard_swamp_speed");
        _isengardNotableCountTownMerchant = Register("dots_isengard_notable_count_town_merchant");
        _isengardNotableCountTownArtisan = Register("dots_isengard_notable_count_town_artisan");
        _isengardNotableCountTownGangLeader = Register("dots_isengard_notable_count_town_gang_leader");
        _isengardNotableCountVillage = Register("dots_isengard_notable_count_village");

        _gundabadArmyInfluenceCost = Register("dots_gundabad_army_influence_cost");
        _gundabadGrainProduction = Register("dots_gundabad_grain_production");
        _gundabadWage = Register("dots_gundabad_wage");
        _gundabadPartySize = Register("dots_gundabad_party_size");
        _gundabadRaidDamage = Register("dots_gundabad_raid_damage");
        _gundabadSnowSpeed = Register("dots_gundabad_snow_speed");
        _gundabadVolunteerRate = Register("dots_gundabad_volunteer_rate");
        _gundabadNotableCountTownArtisan = Register("dots_gundabad_notable_count_town_artisan");
        _gundabadNotableCountTownGangLeader = Register("dots_gundabad_notable_count_town_gang_leader");
        _gundabadNotableCountVillage = Register("dots_gundabad_notable_count_village");
        // Goblins
        _goblinPartySize = Register("dots_goblin_party_size");
        _goblinVolunteerRate = Register("dots_goblin_volunteer_rate");
        _goblinSnowSpeed = Register("dots_goblin_snow_speed");
        _goblinFoodConsumption = Register("dots_goblin_food_consumption");
        _goblinSmithing = Register("dots_goblin_smithing");
        _goblinRaidDamage = Register("dots_goblin_raid_damage");
        // Misty Mountain Orcs
        _mistyMountainOrcsArmyInfluenceCost = Register("dots_mistymountainorcs_army_influence_cost");
        _mistyMountainOrcsPartySize = Register("dots_mistymountainorcs_party_size");
        _mistyMountainOrcsSnowSpeed = Register("dots_mistymountainorcs_snow_speed");
        _mistyMountainOrcsFoodConsumption = Register("dots_mistymountainorcs_food_consumption");
        _mistyMountainOrcsSmithing = Register("dots_mistymountainorcs_smithing");
        _mistyMountainOrcsRaidDamage = Register("dots_mistymountainorcs_raid_damage");
        _mistyMountainOrcsConstructionSpeed = Register("dots_mistymountainorcs_construction_speed");

        _umbarCheaperCaravans = Register("dots_umbar_cheaper_caravans");
        _umbarRenown = Register("dots_umbar_renown");
        _umbarWage = Register("dots_umbar_wage");
        _umbarTariffIncome = Register("dots_umbar_tariff_income");
        _umbarDesertSpeed = Register("dots_umbar_desert_speed");
        _umbarRaidDamage = Register("dots_umbar_raid_damage");
        _umbarFoodConsumption = Register("dots_umbar_food_consumption");

        _dolguldurArmyInfluenceCost = Register("dots_dolguldur_army_influence_cost");
        _dolguldurMilitiaProduction = Register("dots_dolguldur_militia_production");
        _dolguldurConstructionSpeed = Register("dots_dolguldur_construction_speed");
        _dolguldurPartySize = Register("dots_dolguldur_party_size");
        _dolguldurFoodConsumption = Register("dots_dolguldur_food_consumption");
        _dolguldurVolunteerRate = Register("dots_dolguldur_volunteer_rate");
        _dolguldurNotableCountTownMerchant = Register("dots_dolguldur_notable_count_town_merchant");
        _dolguldurNotableCountTownArtisan = Register("dots_dolguldur_notable_count_town_artisan");
        _dolguldurNotableCountTownGangLeader = Register("dots_dolguldur_notable_count_town_gang_leader");
        _dolguldurNotableCountVillage = Register("dots_dolguldur_notable_count_village");

        _gondorGarrisonWage = Register("dots_gondor_garrison_wage");
        _gondorArmyInfluence = Register("dots_gondor_army_influence");
        _gondorHearthGrowth = Register("dots_gondor_hearth_growth");
        _gondorPartySize = Register("dots_gondor_party_size");
        _gondorLoyalty = Register("dots_gondor_loyalty");
        _gondorMorale = Register("dots_gondor_morale");
        _gondorPlainSpeed = Register("dots_gondor_plain_speed");

        _mordorArmyInfluenceCost = Register("dots_mordor_army_influence_cost");
        _mordorGrainProduction = Register("dots_mordor_grain_production");
        _mordorWage = Register("dots_mordor_wage");
        _mordorPartySize = Register("dots_mordor_party_size");
        _mordorRaidDamage = Register("dots_mordor_raid_damage");
        _mordorPlainSpeed = Register("dots_mordor_plain_speed");
        _mordorSwampSpeed = Register("dots_mordor_swamp_speed");
        _mordorNightSpeed = Register("dots_mordor_night_speed");
        _mordorVolunteerRate = Register("dots_mordor_volunteer_rate");
        _mordorSmithing = Register("dots_mordor_smithing");
        _mordorNotableCountTownGangLeader = Register("dots_mordor_notable_count_town_gang_leader");
        _mordorNotableCountVillage = Register("dots_mordor_notable_count_village");

        _rohanMountedCost = Register("dots_rohan_mounted_cost");
        _rohanMountedWage = Register("dots_rohan_mounted_wage");
        _rohanInfantrySpeed = Register("dots_rohan_infantry_speed");
        _rohanLoyalty = Register("dots_rohan_loyalty");
        _rohanMorale = Register("dots_rohan_morale");
        _rohanPlainSpeed = Register("dots_rohan_plain_speed");

        _dalePlainSpeed = Register("dots_dale_plain_speed");
        _khandSteppeSpeed = Register("dots_khand_steppe_speed");
        _rhunSteppeSpeed = Register("dots_rhun_steppe_speed");
        _haradDesertSpeed = Register("dots_harad_desert_speed");
        _dunlandPlainSpeed = Register("dots_dunland_plain_speed");
        _rhunPartySize = Register("dots_rhun_party_size");
        _haradPartySize = Register("dots_harad_party_size");
        _daleTariffIncome = Register("dots_dale_tariff_income");
        _daleRenown = Register("dots_dale_renown");
        _daleLoyalty = Register("dots_dale_loyalty");
        _khandRenown = Register("dots_khand_renown");
        _khandTariffIncome = Register("dots_khand_tariff_income");
        _khandFoodConsumption = Register("dots_khand_food_consumption");
        _khandPartySize = Register("dots_khand_party_size");
        _rhunLoyalty = Register("dots_rhun_loyalty");
        _rhunRaidDamage = Register("dots_rhun_raid_damage");
        _haradMorale = Register("dots_harad_morale");
        _haradFoodConsumption = Register("dots_harad_food_consumption");
        _haradRaidDamage = Register("dots_harad_raid_damage");
        _haradArmyInfluenceCost = Register("dots_harad_army_influence_cost");
        _dunlandPartySize = Register("dots_dunland_party_size");
        _dunlandVolunteerRate = Register("dots_dunland_volunteer_rate");
        _shaghanaDesertSpeed = Register("dots_shaghana_desert_speed");
        _abanissaDesertSpeed = Register("dots_abanissa_desert_speed");
    }

    private void InitializeAll()
    {
        // Erebor — Dwarves: cheap garrisons, strong production, slow construction
        _ereborGarrisonWage.Initialize(
            "{=dots_feat_erebor_gw}Dwarven Garrison",
            "{=dots_feat_erebor_gw_desc}Garrison wages reduced by 25%.",
            -0.25f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _ereborProduction.Initialize(
            "{=dots_feat_erebor_p}Dwarven Industry",
            "{=dots_feat_erebor_p_desc}All village production increased by 10%.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _ereborConstructionSpeed.Initialize(
            "{=dots_feat_erebor_cs}Dwarven Perfectionism",
            "{=dots_feat_erebor_cs_desc}Construction speed reduced by 15%.",
            -0.15f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
        _ereborLoyalty.Initialize(
            "{=dots_feat_erebor_loy}Dwarven Honor",
            "{=dots_feat_erebor_loy_desc}Settlement loyalty increased by 1 per day.",
            1f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _ereborMorale.Initialize(
            "{=dots_feat_erebor_mor}Dwarven Stubbornness",
            "{=dots_feat_erebor_mor_desc}Party morale increased by 5.",
            5f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _ereborSmithing.Initialize(
            "{=dots_feat_erebor_sm}Master Smiths",
            "{=dots_feat_erebor_sm_desc}Smithing energy cost reduced by 30%.",
            -0.3f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _ereborSnowSpeed.Initialize(
            "{=dots_feat_erebor_ss}Mountain Folk",
            "{=dots_feat_erebor_ss_desc}Party movement speed increased by 10% in snow.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _ereborTariffIncome.Initialize(
            "{=dots_feat_erebor_ti}Dwarven Thrift",
            "{=dots_feat_erebor_ti_desc}Tariff income increased by 5%.",
            0.05f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);

        // Rivendell — High Elves: strong armies, good growth, expensive to rally
        _rivendellArmyInfluence.Initialize(
            "{=dots_feat_riv_ai}Elven Wisdom",
            "{=dots_feat_riv_ai_desc}Army influence award increased by 35%.",
            0.35f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _rivendellHearthGrowth.Initialize(
            "{=dots_feat_riv_hg}The Last Homely House",
            "{=dots_feat_riv_hg_desc}Village hearth growth increased by 20%.",
            0.2f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _rivendellArmyInfluenceCost.Initialize(
            "{=dots_feat_riv_aic}Elven Pride",
            "{=dots_feat_riv_aic_desc}Army recruitment costs 25% more influence.",
            0.25f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
        _rivendellFoodConsumption.Initialize(
            "{=dots_feat_riv_fc}Elven Frugality",
            "{=dots_feat_riv_fc_desc}Party food consumption reduced by 15%.",
            -0.15f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _rivendellLoyalty.Initialize(
            "{=dots_feat_riv_loy}Elven Wisdom",
            "{=dots_feat_riv_loy_desc}Settlement loyalty increased by 0.5 per day.",
            0.5f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _rivendellForestSpeed.Initialize(
            "{=dots_feat_riv_fs}Woodland Grace",
            "{=dots_feat_riv_fs_desc}Party movement speed increased by 10% in forests.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);

        // Mirkwood — Wood Elves: forest masters, good militia, isolated
        _mirkwoodForestSpeed.Initialize(
            "{=dots_feat_mrk_fs}Woodland Realm",
            "{=dots_feat_mrk_fs_desc}Party movement speed increased by 10% in forests.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _mirkwoodMilitiaProduction.Initialize(
            "{=dots_feat_mrk_mp}Silvan Wardens",
            "{=dots_feat_mrk_mp_desc}25% increased chance of veteran militia.",
            0.25f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _mirkwoodHearthGrowth.Initialize(
            "{=dots_feat_mrk_hg}Forest Isolation",
            "{=dots_feat_mrk_hg_desc}Village hearth growth reduced by 20%.",
            -0.2f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
        _mirkwoodFoodConsumption.Initialize(
            "{=dots_feat_mrk_fc}Woodland Sustenance",
            "{=dots_feat_mrk_fc_desc}Party food consumption reduced by 15%.",
            -0.15f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _mirkwoodMorale.Initialize(
            "{=dots_feat_mrk_mor}Woodland Bonds",
            "{=dots_feat_mrk_mor_desc}Party morale increased by 3.",
            3f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _mirkwoodArmyInfluenceCost.Initialize(
            "{=dots_feat_mrk_aic}Isolationist Court",
            "{=dots_feat_mrk_aic_desc}Army recruitment costs 15% more influence.",
            0.15f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);

        // Lothlorien — Golden Wood: forest speed, cheap garrisons, slow building
        _lothlorienForestSpeed.Initialize(
            "{=dots_feat_loth_fs}Golden Wood",
            "{=dots_feat_loth_fs_desc}Party movement speed increased by 10% in forests.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _lothlorienGarrisonWage.Initialize(
            "{=dots_feat_loth_gw}Wardens of Lorien",
            "{=dots_feat_loth_gw_desc}Garrison wages reduced by 20%.",
            -0.2f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _lothlorienConstructionSpeed.Initialize(
            "{=dots_feat_loth_cs}Timeless Craft",
            "{=dots_feat_loth_cs_desc}Construction speed reduced by 10%.",
            -0.1f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
        _lothlorienFoodConsumption.Initialize(
            "{=dots_feat_loth_fc}Lembas Bread",
            "{=dots_feat_loth_fc_desc}Party food consumption reduced by 15%.",
            -0.15f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _lothlorienLoyalty.Initialize(
            "{=dots_feat_loth_loy}Elven Grace",
            "{=dots_feat_loth_loy_desc}Settlement loyalty increased by 0.5 per day.",
            0.5f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _lothlorienMorale.Initialize(
            "{=dots_feat_loth_mor}Elven Harmony",
            "{=dots_feat_loth_mor_desc}Party morale increased by 3.",
            3f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _lothlorienVolunteerRate.Initialize(
            "{=dots_feat_loth_vr}Fading Light",
            "{=dots_feat_loth_vr_desc}Village volunteer respawn rate reduced by 15%.",
            -0.15f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);

        // Isengard — Saruman: cheap mounted recruits, cheap garrisons, decision penalty
        _isengardCheaperRecruits.Initialize(
            "{=dots_feat_isen_cr}War Machine",
            "{=dots_feat_isen_cr_desc}Mounted troop recruitment and upgrade costs reduced by 15%.",
            -0.15f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _isengardGarrisonWage.Initialize(
            "{=dots_feat_isen_gw}Orthanc Garrison",
            "{=dots_feat_isen_gw_desc}Garrison wages reduced by 20%.",
            -0.2f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _isengardDecisionPenalty.Initialize(
            "{=dots_feat_isen_dp}Saruman's Grip",
            "{=dots_feat_isen_dp_desc}Kingdom decision relationship penalties increased by 25%.",
            0.25f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
        _isengardPartySize.Initialize(
            "{=dots_feat_isen_ps}Uruk-hai Legions",
            "{=dots_feat_isen_ps_desc}Party size limit increased by 20%.",
            0.2f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _isengardConstructionSpeed.Initialize(
            "{=dots_feat_isen_cs}Industrial Might",
            "{=dots_feat_isen_cs_desc}Construction speed increased by 15%.",
            0.15f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _isengardSmithing.Initialize(
            "{=dots_feat_isen_sm}Industrial Forges",
            "{=dots_feat_isen_sm_desc}Smithing energy cost reduced by 20%.",
            -0.2f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _isengardRaidDamage.Initialize(
            "{=dots_feat_isen_rd}War Machine Raids",
            "{=dots_feat_isen_rd_desc}Raid damage increased by 20%.",
            0.2f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _isengardPlainSpeed.Initialize(
            "{=dots_feat_isen_ps2}Forced March",
            "{=dots_feat_isen_ps2_desc}Party movement speed increased by 10% on plains.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _isengardSwampSpeed.Initialize(
            "{=dots_feat_isen_sws}Fenland Drillmasters",
            "{=dots_feat_isen_sws_desc}Party movement speed increased by 10% in swamps.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        // Per-occupation town notable counts (AdditionType.Add — flat extras above vanilla base).
        // Vanilla town target: 2 Merchant + 1 Artisan + 2 Gang Leader = 5. Isengard target: 4/2/14 = 20.
        _isengardNotableCountTownMerchant.Initialize(
            "{=dots_feat_isen_nctm}Orthanc Quartermasters",
            "{=dots_feat_isen_nctm_desc}+2 merchant notables in towns.",
            2f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _isengardNotableCountTownArtisan.Initialize(
            "{=dots_feat_isen_ncta}Industrial Forges",
            "{=dots_feat_isen_ncta_desc}+1 artisan notable in towns.",
            1f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _isengardNotableCountTownGangLeader.Initialize(
            "{=dots_feat_isen_nctg}Uruk-hai Captains",
            "{=dots_feat_isen_nctg_desc}+12 gang-leader notables in towns (Isengard's single town becomes a major recruitment hub).",
            12f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _isengardNotableCountVillage.Initialize(
            "{=dots_feat_isen_ncv}Iron Press",
            "{=dots_feat_isen_ncv_desc}Notable count in villages increased by 10%.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);

        // Gundabad — Orcs: cheap armies, grain production, expensive wages
        _gundabadArmyInfluenceCost.Initialize(
            "{=dots_feat_gun_aic}Orc Horde",
            "{=dots_feat_gun_aic_desc}Army recruitment costs 40% less influence.",
            -0.4f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _gundabadGrainProduction.Initialize(
            "{=dots_feat_gun_gp}Plundered Stores",
            "{=dots_feat_gun_gp_desc}Grain production increased by 15%.",
            0.15f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _gundabadWage.Initialize(
            "{=dots_feat_gun_w}Plunder Demands",
            "{=dots_feat_gun_w_desc}Party wages increased by 10%.",
            0.1f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
        _gundabadPartySize.Initialize(
            "{=dots_feat_gun_ps}Mountain Swarm",
            "{=dots_feat_gun_ps_desc}Party size limit increased by 20%.",
            0.2f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _gundabadRaidDamage.Initialize(
            "{=dots_feat_gun_rd}Orc Pillagers",
            "{=dots_feat_gun_rd_desc}Raid damage increased by 25%.",
            0.25f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _gundabadSnowSpeed.Initialize(
            "{=dots_feat_gun_ss}Mountain Marauders",
            "{=dots_feat_gun_ss_desc}Party movement speed increased by 10% in snow.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _gundabadVolunteerRate.Initialize(
            "{=dots_feat_gun_vr}Mountain Levies",
            "{=dots_feat_gun_vr_desc}Village volunteer respawn rate increased by 20%.",
            0.2f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        // Per-occupation town notable counts. Gundabad target: 2 Merchant + 2 Artisan + 5 Gang Leader = 9.
        _gundabadNotableCountTownArtisan.Initialize(
            "{=dots_feat_gun_ncta}Bone-Smiths",
            "{=dots_feat_gun_ncta_desc}+1 artisan notable in towns.",
            1f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _gundabadNotableCountTownGangLeader.Initialize(
            "{=dots_feat_gun_nctg}Pale Warband Chieftains",
            "{=dots_feat_gun_nctg_desc}+3 gang-leader notables in towns.",
            3f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _gundabadNotableCountVillage.Initialize(
            "{=dots_feat_gun_ncv}Bone Camps",
            "{=dots_feat_gun_ncv_desc}Notable count in villages increased by 10%.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        // Goblins — vast weak swarms that breed endlessly and eat everything
        _goblinPartySize.Initialize(
            "{=dots_feat_gob_ps}Goblin Swarm",
            "{=dots_feat_gob_ps_desc}Party size limit increased by 40%.",
            0.4f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _goblinVolunteerRate.Initialize(
            "{=dots_feat_gob_vr}Endless Spawn",
            "{=dots_feat_gob_vr_desc}Village volunteer respawn rate increased by 25%.",
            0.25f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _goblinSnowSpeed.Initialize(
            "{=dots_feat_gob_ss}Tunnel-Runners",
            "{=dots_feat_gob_ss_desc}Party movement speed increased by 10% in snow.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _goblinFoodConsumption.Initialize(
            "{=dots_feat_gob_fc}Ravenous Swarm",
            "{=dots_feat_gob_fc_desc}Party food consumption increased by 20%.",
            0.2f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
        _goblinSmithing.Initialize(
            "{=dots_feat_gob_sm}Captured-Weapon Hoard",
            "{=dots_feat_gob_sm_desc}Smithing energy cost reduced by 10%.",
            -0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _goblinRaidDamage.Initialize(
            "{=dots_feat_gob_rd}Goblin Ambush",
            "{=dots_feat_gob_rd_desc}Raid damage increased by 10%.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        // Misty Mountain Orcs — large weak war-host, cheap to muster, hungry on the march
        _mistyMountainOrcsArmyInfluenceCost.Initialize(
            "{=dots_feat_mmo_aic}Orc Horde",
            "{=dots_feat_mmo_aic_desc}Army recruitment costs 40% less influence.",
            -0.4f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _mistyMountainOrcsPartySize.Initialize(
            "{=dots_feat_mmo_ps}Mountain Host",
            "{=dots_feat_mmo_ps_desc}Party size limit increased by 30%.",
            0.3f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _mistyMountainOrcsSnowSpeed.Initialize(
            "{=dots_feat_mmo_ss}Mountain-Bred",
            "{=dots_feat_mmo_ss_desc}Party movement speed increased by 10% in snow.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _mistyMountainOrcsFoodConsumption.Initialize(
            "{=dots_feat_mmo_fc}Hungry Host",
            "{=dots_feat_mmo_fc_desc}Party food consumption increased by 15%.",
            0.15f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
        _mistyMountainOrcsSmithing.Initialize(
            "{=dots_feat_mmo_sm}Looted Forges",
            "{=dots_feat_mmo_sm_desc}Smithing energy cost reduced by 15%.",
            -0.15f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _mistyMountainOrcsRaidDamage.Initialize(
            "{=dots_feat_mmo_rd}Cave Troll Levy",
            "{=dots_feat_mmo_rd_desc}Raid damage increased by 15%.",
            0.15f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _mistyMountainOrcsConstructionSpeed.Initialize(
            "{=dots_feat_mmo_cs}Echoing Halls",
            "{=dots_feat_mmo_cs_desc}Construction speed reduced by 10%.",
            -0.1f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);

        // Umbar — Corsairs: cheap caravans, battle renown, expensive wages
        _umbarCheaperCaravans.Initialize(
            "{=dots_feat_umb_cc}Corsair Trade",
            "{=dots_feat_umb_cc_desc}Caravan formation cost reduced by 25%.",
            -0.25f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _umbarRenown.Initialize(
            "{=dots_feat_umb_r}Corsair Glory",
            "{=dots_feat_umb_r_desc}Renown from battles increased by 8%.",
            0.08f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _umbarWage.Initialize(
            "{=dots_feat_umb_w}Corsair Greed",
            "{=dots_feat_umb_w_desc}Party wages increased by 8%.",
            0.08f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
        _umbarTariffIncome.Initialize(
            "{=dots_feat_umb_ti}Corsair Trade Networks",
            "{=dots_feat_umb_ti_desc}Tariff income increased by 15%.",
            0.15f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _umbarDesertSpeed.Initialize(
            "{=dots_feat_umb_ds}Desert Corsairs",
            "{=dots_feat_umb_ds_desc}Party movement speed increased by 10% in deserts.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _umbarRaidDamage.Initialize(
            "{=dots_feat_umb_rd}Corsair Raid Doctrine",
            "{=dots_feat_umb_rd_desc}Raid damage increased by 20%.",
            0.2f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _umbarFoodConsumption.Initialize(
            "{=dots_feat_umb_fc}Black Numenorean Endurance",
            "{=dots_feat_umb_fc_desc}Party food consumption reduced by 10%.",
            -0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);

        // Dol Guldur — Shadow: cheap armies, veteran militia, slow construction
        _dolguldurArmyInfluenceCost.Initialize(
            "{=dots_feat_dg_aic}Shadow Command",
            "{=dots_feat_dg_aic_desc}Army recruitment costs 50% less influence.",
            -0.5f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _dolguldurMilitiaProduction.Initialize(
            "{=dots_feat_dg_mp}Dark Conscription",
            "{=dots_feat_dg_mp_desc}20% increased chance of veteran militia.",
            0.2f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _dolguldurConstructionSpeed.Initialize(
            "{=dots_feat_dg_cs}Ruinous Works",
            "{=dots_feat_dg_cs_desc}Construction speed reduced by 20%.",
            -0.2f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
        _dolguldurPartySize.Initialize(
            "{=dots_feat_dg_ps}Dark Legions",
            "{=dots_feat_dg_ps_desc}Party size limit increased by 20%.",
            0.2f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _dolguldurFoodConsumption.Initialize(
            "{=dots_feat_dg_fc}Voracious Hordes",
            "{=dots_feat_dg_fc_desc}Party food consumption increased by 10%.",
            0.1f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
        _dolguldurVolunteerRate.Initialize(
            "{=dots_feat_dg_vr}Dark Conscripts",
            "{=dots_feat_dg_vr_desc}Village volunteer respawn rate increased by 20%.",
            0.2f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        // Per-occupation town notable counts. Dol Guldur target: 3 Merchant + 2 Artisan + 15 Gang Leader = 20.
        _dolguldurNotableCountTownMerchant.Initialize(
            "{=dots_feat_dg_nctm}Shadow Brokers",
            "{=dots_feat_dg_nctm_desc}+1 merchant notable in towns.",
            1f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _dolguldurNotableCountTownArtisan.Initialize(
            "{=dots_feat_dg_ncta}Dark Smithies",
            "{=dots_feat_dg_ncta_desc}+1 artisan notable in towns.",
            1f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _dolguldurNotableCountTownGangLeader.Initialize(
            "{=dots_feat_dg_nctg}Shadow Captains",
            "{=dots_feat_dg_nctg_desc}+13 gang-leader notables in towns (Dol Guldur's shadow command center).",
            13f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _dolguldurNotableCountVillage.Initialize(
            "{=dots_feat_dg_ncv}Hidden Hovels",
            "{=dots_feat_dg_ncv_desc}Notable count in villages increased by 10%.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);

        // Gondor — Men of the West: cheap garrisons, army influence, depleted hearths
        _gondorGarrisonWage.Initialize(
            "{=dots_feat_gon_gw}Tower Guard",
            "{=dots_feat_gon_gw_desc}Garrison wages reduced by 20%.",
            -0.2f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _gondorArmyInfluence.Initialize(
            "{=dots_feat_gon_ai}Gondorian Discipline",
            "{=dots_feat_gon_ai_desc}Army influence award increased by 30%.",
            0.3f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _gondorHearthGrowth.Initialize(
            "{=dots_feat_gon_hg}War-Depleted Lands",
            "{=dots_feat_gon_hg_desc}Village hearth growth reduced by 15%.",
            -0.15f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
        _gondorPartySize.Initialize(
            "{=dots_feat_gon_ps}Standing Armies",
            "{=dots_feat_gon_ps_desc}Party size limit increased by 2.5%.",
            0.025f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _gondorLoyalty.Initialize(
            "{=dots_feat_gon_loy}Tower Guard Discipline",
            "{=dots_feat_gon_loy_desc}Settlement loyalty increased by 1 per day.",
            1f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _gondorMorale.Initialize(
            "{=dots_feat_gon_mor}Gondorian Resolve",
            "{=dots_feat_gon_mor_desc}Party morale increased by 5.",
            5f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _gondorPlainSpeed.Initialize(
            "{=dots_feat_gon_ps2}Men of the Fields",
            "{=dots_feat_gon_ps2_desc}Party movement speed increased by 10% on plains.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);

        // Mordor — Dark Lord: very cheap armies, grain production, very expensive wages
        _mordorArmyInfluenceCost.Initialize(
            "{=dots_feat_mor_aic}The Dark Lord's Will",
            "{=dots_feat_mor_aic_desc}Army recruitment costs 60% less influence.",
            -0.6f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _mordorGrainProduction.Initialize(
            "{=dots_feat_mor_gp}Nurn Farmlands",
            "{=dots_feat_mor_gp_desc}Grain production increased by 20%.",
            0.2f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _mordorWage.Initialize(
            "{=dots_feat_mor_w}Dark Tribute",
            "{=dots_feat_mor_w_desc}Party wages increased by 20%.",
            0.2f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
        _mordorPartySize.Initialize(
            "{=dots_feat_mor_ps}Sauron's Hordes",
            "{=dots_feat_mor_ps_desc}Party size limit increased by 10%.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _mordorRaidDamage.Initialize(
            "{=dots_feat_mor_rd}Sauron's Wrath",
            "{=dots_feat_mor_rd_desc}Raid damage increased by 25%.",
            0.25f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _mordorPlainSpeed.Initialize(
            "{=dots_feat_mor_pls}Shadow March",
            "{=dots_feat_mor_pls_desc}Party movement speed increased by 5% on plains.",
            0.05f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _mordorSwampSpeed.Initialize(
            "{=dots_feat_mor_sws}Dead Marshes",
            "{=dots_feat_mor_sws_desc}Party movement speed increased by 5% in swamps.",
            0.05f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _mordorNightSpeed.Initialize(
            "{=dots_feat_mor_ns}Creatures of the Dark",
            "{=dots_feat_mor_ns_desc}Party movement speed increased by 10% at night.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _mordorVolunteerRate.Initialize(
            "{=dots_feat_mor_vr}Sauron's Levy",
            "{=dots_feat_mor_vr_desc}Village volunteer respawn rate increased by 20%.",
            0.2f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _mordorSmithing.Initialize(
            "{=dots_feat_mord_sm}Dark Smithing",
            "{=dots_feat_mord_sm_desc}Smithing energy cost reduced by 15%.",
            -0.15f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        // Per-occupation town notable counts. Mordor target: 2 Merchant + 1 Artisan + 4 Gang Leader = 7.
        _mordorNotableCountTownGangLeader.Initialize(
            "{=dots_feat_mor_nctg}Black Speech Heralds",
            "{=dots_feat_mor_nctg_desc}+2 gang-leader notables in towns.",
            2f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _mordorNotableCountVillage.Initialize(
            "{=dots_feat_mor_ncv}Slave Drivers",
            "{=dots_feat_mor_ncv_desc}Notable count in villages increased by 5%.",
            0.05f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);

        // Rohan — Horse-lords: cheap mounted troops, slow without cavalry
        _rohanMountedCost.Initialize(
            "{=dots_feat_roh_mc}Horse-lord Heritage",
            "{=dots_feat_roh_mc_desc}Mounted troop recruitment and upgrade costs reduced by 15%.",
            -0.15f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _rohanMountedWage.Initialize(
            "{=dots_feat_roh_mw}Riders of the Mark",
            "{=dots_feat_roh_mw_desc}Mounted troop wages reduced by 15%.",
            -0.15f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _rohanInfantrySpeed.Initialize(
            "{=dots_feat_roh_is}Cavalry Dependent",
            "{=dots_feat_roh_is_desc}Party speed reduced by 10% when majority infantry.",
            -0.1f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
        _rohanLoyalty.Initialize(
            "{=dots_feat_roh_loy}Horse-lord Fellowship",
            "{=dots_feat_roh_loy_desc}Settlement loyalty increased by 0.5 per day.",
            0.5f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _rohanMorale.Initialize(
            "{=dots_feat_roh_mor}Riders' Spirit",
            "{=dots_feat_roh_mor_desc}Party morale increased by 5.",
            5f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _rohanPlainSpeed.Initialize(
            "{=dots_feat_roh_pls}Riders of the Plains",
            "{=dots_feat_roh_pls_desc}Party movement speed increased by 10% on plains.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);

        // Dale — Men of Dale: traders of the vale
        _dalePlainSpeed.Initialize(
            "{=dots_feat_dale_pls}Vale Traders",
            "{=dots_feat_dale_pls_desc}Party movement speed increased by 10% on plains.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _daleTariffIncome.Initialize(
            "{=dots_feat_dale_ti}Dwarven Trade Alliance",
            "{=dots_feat_dale_ti_desc}Tariff income increased by 10%.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _daleRenown.Initialize(
            "{=dots_feat_dale_r}Black Arrow Tradition",
            "{=dots_feat_dale_r_desc}Battle renown increased by 10%.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _daleLoyalty.Initialize(
            "{=dots_feat_dale_loy}Small Territory Exposure",
            "{=dots_feat_dale_loy_desc}Settlement loyalty reduced by 0.5 per day.",
            -0.5f, isPositiveEffect: false, FeatObject.AdditionType.Add);

        // Khand — Variags: steppe charioteers
        _khandSteppeSpeed.Initialize(
            "{=dots_feat_khand_sts}Steppe Charioteers",
            "{=dots_feat_khand_sts_desc}Party movement speed increased by 10% on steppes.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _khandRenown.Initialize(
            "{=dots_feat_khand_r}Mercenary Premium",
            "{=dots_feat_khand_r_desc}Battle renown increased by 8%.",
            0.08f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _khandTariffIncome.Initialize(
            "{=dots_feat_khand_ti}Tribute to Mordor",
            "{=dots_feat_khand_ti_desc}Tariff income reduced by 10%.",
            -0.1f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);
        _khandFoodConsumption.Initialize(
            "{=dots_feat_khand_fc}Steppe Endurance",
            "{=dots_feat_khand_fc_desc}Party food consumption reduced by 10%.",
            -0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _khandPartySize.Initialize(
            "{=dots_feat_khand_ps}Charioteer Mobility",
            "{=dots_feat_khand_ps_desc}Party size limit increased by 5%.",
            0.05f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);

        // Rhun — Easterlings: mounted outriders
        _rhunSteppeSpeed.Initialize(
            "{=dots_feat_rhun_sts}Easterling Outriders",
            "{=dots_feat_rhun_sts_desc}Party movement speed increased by 10% on steppes.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _rhunPartySize.Initialize(
            "{=dots_feat_rhun_ps}Easterling Host",
            "{=dots_feat_rhun_ps_desc}Party size limit increased by 5%.",
            0.05f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _rhunLoyalty.Initialize(
            "{=dots_feat_rhun_loy}Easterling Tribute",
            "{=dots_feat_rhun_loy_desc}Settlement loyalty reduced by 0.5 per day.",
            -0.5f, isPositiveEffect: false, FeatObject.AdditionType.Add);
        _rhunRaidDamage.Initialize(
            "{=dots_feat_rhun_rd}Steppe Raider Doctrine",
            "{=dots_feat_rhun_rd_desc}Raid damage increased by 15%.",
            0.15f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);

        // Harad — Haradrim: sons of the sun
        _haradDesertSpeed.Initialize(
            "{=dots_feat_har_ds}Sons of the Sun",
            "{=dots_feat_har_ds_desc}Party movement speed increased by 10% in deserts.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _haradPartySize.Initialize(
            "{=dots_feat_har_ps}Haradrim Warbands",
            "{=dots_feat_har_ps_desc}Party size limit increased by 5%.",
            0.05f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _haradMorale.Initialize(
            "{=dots_feat_harad_mor}Mumakil Drivers",
            "{=dots_feat_harad_mor_desc}Party morale increased by 5.",
            5f, isPositiveEffect: true, FeatObject.AdditionType.Add);
        _haradFoodConsumption.Initialize(
            "{=dots_feat_harad_fc}Desert Endurance",
            "{=dots_feat_harad_fc_desc}Party food consumption reduced by 15%.",
            -0.15f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _haradRaidDamage.Initialize(
            "{=dots_feat_harad_rd}Far Harad Savagery",
            "{=dots_feat_harad_rd_desc}Raid damage increased by 15%.",
            0.15f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _haradArmyInfluenceCost.Initialize(
            "{=dots_feat_harad_aic}Divided Tribes",
            "{=dots_feat_harad_aic_desc}Army recruitment costs 15% more influence.",
            0.15f, isPositiveEffect: false, FeatObject.AdditionType.AddFactor);

        // Dunland — Hill-men: hill marchers
        _dunlandPlainSpeed.Initialize(
            "{=dots_feat_dun_pls}Hill Marchers",
            "{=dots_feat_dun_pls_desc}Party movement speed increased by 10% on plains.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _dunlandPartySize.Initialize(
            "{=dots_feat_dun_ps}Hill-Tribe Levy",
            "{=dots_feat_dun_ps_desc}Party size limit increased by 5%.",
            0.05f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
        _dunlandVolunteerRate.Initialize(
            "{=dots_feat_dun_vr}Hill-Tribe Recruitment",
            "{=dots_feat_dun_vr_desc}Village volunteer respawn rate increased by 10%.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);

        // Shaghana — southern Haradrim: dune wanderers
        _shaghanaDesertSpeed.Initialize(
            "{=dots_feat_shg_ds}Dune Wanderers",
            "{=dots_feat_shg_ds_desc}Party movement speed increased by 10% in deserts.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);

        // Abanissa — deep-south Haradrim: deep desert nomads
        _abanissaDesertSpeed.Initialize(
            "{=dots_feat_aba_ds}Deep Desert Nomads",
            "{=dots_feat_aba_ds_desc}Party movement speed increased by 10% in deserts.",
            0.1f, isPositiveEffect: true, FeatObject.AdditionType.AddFactor);
    }

    private static FeatObject Register(string stringId)
        => Game.Current.ObjectManager.RegisterPresumedObject(new FeatObject(stringId));

    internal static IEnumerable<FeatObject> GetAllFeats()
    {
        if (_instance == null)
            yield break;

        yield return _instance._ereborGarrisonWage;
        yield return _instance._ereborProduction;
        yield return _instance._ereborConstructionSpeed;
        yield return _instance._ereborLoyalty;
        yield return _instance._ereborMorale;
        yield return _instance._ereborSmithing;
        yield return _instance._ereborSnowSpeed;
        yield return _instance._ereborTariffIncome;
        yield return _instance._rivendellArmyInfluence;
        yield return _instance._rivendellHearthGrowth;
        yield return _instance._rivendellArmyInfluenceCost;
        yield return _instance._rivendellFoodConsumption;
        yield return _instance._rivendellLoyalty;
        yield return _instance._rivendellForestSpeed;
        yield return _instance._mirkwoodForestSpeed;
        yield return _instance._mirkwoodMilitiaProduction;
        yield return _instance._mirkwoodHearthGrowth;
        yield return _instance._mirkwoodFoodConsumption;
        yield return _instance._mirkwoodMorale;
        yield return _instance._mirkwoodArmyInfluenceCost;
        yield return _instance._lothlorienForestSpeed;
        yield return _instance._lothlorienGarrisonWage;
        yield return _instance._lothlorienConstructionSpeed;
        yield return _instance._lothlorienFoodConsumption;
        yield return _instance._lothlorienLoyalty;
        yield return _instance._lothlorienMorale;
        yield return _instance._lothlorienVolunteerRate;
        yield return _instance._isengardCheaperRecruits;
        yield return _instance._isengardGarrisonWage;
        yield return _instance._isengardDecisionPenalty;
        yield return _instance._isengardPartySize;
        yield return _instance._isengardConstructionSpeed;
        yield return _instance._isengardSmithing;
        yield return _instance._isengardRaidDamage;
        yield return _instance._isengardPlainSpeed;
        yield return _instance._isengardSwampSpeed;
        yield return _instance._isengardNotableCountTownMerchant;
        yield return _instance._isengardNotableCountTownArtisan;
        yield return _instance._isengardNotableCountTownGangLeader;
        yield return _instance._isengardNotableCountVillage;
        yield return _instance._gundabadArmyInfluenceCost;
        yield return _instance._gundabadGrainProduction;
        yield return _instance._gundabadWage;
        yield return _instance._gundabadPartySize;
        yield return _instance._gundabadRaidDamage;
        yield return _instance._gundabadSnowSpeed;
        yield return _instance._gundabadVolunteerRate;
        yield return _instance._gundabadNotableCountTownArtisan;
        yield return _instance._gundabadNotableCountTownGangLeader;
        yield return _instance._gundabadNotableCountVillage;
        yield return _instance._goblinPartySize;
        yield return _instance._goblinVolunteerRate;
        yield return _instance._goblinSnowSpeed;
        yield return _instance._goblinFoodConsumption;
        yield return _instance._goblinSmithing;
        yield return _instance._goblinRaidDamage;
        yield return _instance._mistyMountainOrcsArmyInfluenceCost;
        yield return _instance._mistyMountainOrcsPartySize;
        yield return _instance._mistyMountainOrcsSnowSpeed;
        yield return _instance._mistyMountainOrcsFoodConsumption;
        yield return _instance._mistyMountainOrcsSmithing;
        yield return _instance._mistyMountainOrcsRaidDamage;
        yield return _instance._mistyMountainOrcsConstructionSpeed;
        yield return _instance._umbarCheaperCaravans;
        yield return _instance._umbarRenown;
        yield return _instance._umbarWage;
        yield return _instance._umbarTariffIncome;
        yield return _instance._umbarDesertSpeed;
        yield return _instance._umbarRaidDamage;
        yield return _instance._umbarFoodConsumption;
        yield return _instance._dolguldurArmyInfluenceCost;
        yield return _instance._dolguldurMilitiaProduction;
        yield return _instance._dolguldurConstructionSpeed;
        yield return _instance._dolguldurPartySize;
        yield return _instance._dolguldurFoodConsumption;
        yield return _instance._dolguldurVolunteerRate;
        yield return _instance._dolguldurNotableCountTownMerchant;
        yield return _instance._dolguldurNotableCountTownArtisan;
        yield return _instance._dolguldurNotableCountTownGangLeader;
        yield return _instance._dolguldurNotableCountVillage;
        yield return _instance._gondorGarrisonWage;
        yield return _instance._gondorArmyInfluence;
        yield return _instance._gondorHearthGrowth;
        yield return _instance._gondorPartySize;
        yield return _instance._gondorLoyalty;
        yield return _instance._gondorMorale;
        yield return _instance._gondorPlainSpeed;
        yield return _instance._mordorArmyInfluenceCost;
        yield return _instance._mordorGrainProduction;
        yield return _instance._mordorWage;
        yield return _instance._mordorPartySize;
        yield return _instance._mordorRaidDamage;
        yield return _instance._mordorPlainSpeed;
        yield return _instance._mordorSwampSpeed;
        yield return _instance._mordorNightSpeed;
        yield return _instance._mordorVolunteerRate;
        yield return _instance._mordorSmithing;
        yield return _instance._mordorNotableCountTownGangLeader;
        yield return _instance._mordorNotableCountVillage;
        yield return _instance._rohanMountedCost;
        yield return _instance._rohanMountedWage;
        yield return _instance._rohanInfantrySpeed;
        yield return _instance._rohanLoyalty;
        yield return _instance._rohanMorale;
        yield return _instance._rohanPlainSpeed;
        yield return _instance._dalePlainSpeed;
        yield return _instance._daleTariffIncome;
        yield return _instance._daleRenown;
        yield return _instance._daleLoyalty;
        yield return _instance._khandSteppeSpeed;
        yield return _instance._khandRenown;
        yield return _instance._khandTariffIncome;
        yield return _instance._khandFoodConsumption;
        yield return _instance._khandPartySize;
        yield return _instance._rhunSteppeSpeed;
        yield return _instance._rhunPartySize;
        yield return _instance._rhunLoyalty;
        yield return _instance._rhunRaidDamage;
        yield return _instance._haradDesertSpeed;
        yield return _instance._haradPartySize;
        yield return _instance._haradMorale;
        yield return _instance._haradFoodConsumption;
        yield return _instance._haradRaidDamage;
        yield return _instance._haradArmyInfluenceCost;
        yield return _instance._dunlandPlainSpeed;
        yield return _instance._dunlandPartySize;
        yield return _instance._dunlandVolunteerRate;
        yield return _instance._shaghanaDesertSpeed;
        yield return _instance._abanissaDesertSpeed;
    }
}
