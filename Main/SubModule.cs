using Bannerlord.UIExtenderEx;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;
using DOTS.Features;
using DOTS.Features.BannerInjection;
using DOTS.Core.Infrastructure;
using DOTS.Core.Logging;
using DOTS.Features.CharacterCreation;
using DOTS.Features.FactionMap;
using DOTS.Features.InitialChildGeneration;
using DOTS.Adapters;
using DOTS.Features.Diplomacy;
using DOTS.Features.Diplomacy.Hooks;
using DOTS.Features.Diplomacy.Models;
using DOTS.Features.Execution;
using DOTS.Features.Execution.Hooks;
using DOTS.Features.Execution.Models;
using DOTS.Features.StartupResources;
using DOTS.Features.NamedCompanions;
using DOTS.Features.TroopProgression;
using DOTS.Features.TroopWeight;
using DOTS.Features.TroopWeight.Hooks;
using DOTS.Features.AtmospherePersistence.Hooks;
using DOTS.Features.TroopProgression.Models;
using DOTS.Features.AdvancedCombat;
using DOTS.Features.CulturalFeats.Models;
using DOTS.Features.CustomBattles;
using DOTS.Features.CustomBattles.Hooks;
using DOTS.Features.BattleBalance;
using DOTS.Features.BattleBalance.Models;
using DOTS.Features.Arena.Models;
using DOTS.Features.Encyclopedia;
using DOTS.Features.Encyclopedia.Models;
using DOTS.Features.MainMenuCustomizer;
using DOTS.Features.ShaderPrecompilation;
using DOTS.Features.Siege;
using DOTS.Features.Siege.Models;
using DOTS.Features.ArmyTargeting;
using DOTS.Features.ArmyTargeting.Models;
using DOTS.Features.TimeAcceleration;
using DOTS.Features.BannerColorPersistence;
using DOTS.Features.BannerColorPersistence.Hooks;
using DOTS.Features.LocalizationOverride;
using DOTS.Features.LocalizationOverride.Hooks;
using DOTS.Features.SpecialResources;
using DOTS.Features.SpecialResources.Hooks;
using DOTS.Features.CareerSystem;
using DOTS.Features.CareerSystem.Models;
using DOTS.Features.SettlementGuards;
using DOTS.Features.SettlementGuards.Hooks;
using DOTS.Features.RevoltTuning;
using DOTS.Features.BanditManagement;
using DOTS.Features.BanditManagement.Models;
using DOTS.Features.CastleRecruitment;
using DOTS.Features.CastleRecruitment.Hooks;
using DOTS.Features.SiegeDismount.Hooks;
using DOTS.Features.MixedFormations.Hooks;
using DOTS.Features.SmartCavalryAI.Hooks;
using DOTS.Features.FiefManagement;
using DOTS.Features.FiefManagement.Hooks;
using DOTS.Features.SettlementNameplateFade;
using DOTS.Features.SettlementNameplateFade.Hooks;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;
using BehaviorTreeWrapper;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace DOTS;

public class SubModule : MBSubModuleBase
{
    private Harmony _harmony;
    private UIExtender? _uiExtender;
    private ITimeAccelerationService? _timeAccelerationService;
    private static float _shaderTickAccumulator;
    private static int _lastShaderCount = -1;
    private static bool _missionTimePatchesApplied;

    protected override void OnSubModuleLoad()
    {
        base.OnSubModuleLoad();

        IoC.Configure();

        // Codex review #46 (2026-05-25) MED-01: attach Patch37_CrashReport IMMEDIATELY
        // after IoC.Configure() so its Finalizers cover the rest of OnSubModuleLoad
        // (UIExtender init, time-acceleration resolve, downstream PatchCategory calls).
        // Previous order left lines 88-107 uncatchable. The only unavoidable blind spot
        // is the IoC.Configure() call itself — if THAT throws, the entire feature is
        // unreachable. Split CrashReport bootstrap doesn't fix this without re-implementing
        // a manual DI container; accept and document the residual.
        _harmony = new Harmony("com.dots.mod");
        if ((DOTS.Features.CrashReport.CrashReportSettings.Instance?.EnableCrashCapture) ?? true)
        {
            try
            {
                _harmony.PatchCategory("Patch37_CrashReport");
                IoC.Resolve<DOTS.Features.CrashReport.Hooks.AppDomainExceptionHook>().Subscribe();
                if ((DOTS.Features.CrashReport.CrashReportSettings.Instance?.EnableNativeToManagedCapture) ?? true)
                {
                    IoC.Resolve<DOTS.Features.CrashReport.Hooks.Native2ManagedPatcher>().AttachAll(_harmony);
                }
            }
            catch (System.Exception ex)
            {
                IoC.Resolve<IModLogger>().LogError($"[CrashReport] init failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        _uiExtender = UIExtender.Create("DOTS");
        _uiExtender.Register(typeof(SubModule).Assembly);
        _uiExtender.Enable();

        // Patch41_McmLayoutFix — flip MCM's embedded options-screen prefabs from VerticalBottomToTop
        // to VerticalTopToBottom (v1.4.0 layout regression). MCM's prefabs are embedded in
        // Bannerlord.MBOptionScreen and load via WidgetFactoryManager.CreateAndRegister, which bypasses
        // UIExtenderEx's [PrefabExtension] hook — so this is a Harmony Postfix, not a PrefabExtension.
        // MUST be applied here in OnSubModuleLoad: MCM's ResourceInjector.Inject() runs at
        // OnBeforeInitialModuleScreenSetAsRoot (after every module's OnSubModuleLoad), so the Postfix
        // must already be attached when MCM calls CreateAndRegister.
        _harmony.PatchCategory("Patch41_McmLayoutFix");

        _timeAccelerationService = IoC.Resolve<ITimeAccelerationService>();

        // Must be first — intercepts GetLocalizedText before any game texts are resolved.
        // Loads English string overrides from dots_module_strings.xml (removes hardcoded "The" articles).
        _harmony.PatchCategory("Patch25_LocalizationOverride");
        var pathService0 = IoC.Resolve<IPathService>();
        var logger0 = IoC.Resolve<IModLogger>();
        var xmlPath = System.IO.Path.Combine(pathService0.ModuleDataPath, "dots_module_strings.xml");
        try
        {
            var overrides = LocalizationOverrideLoader.ParseOverridesFromFile(xmlPath);
            foreach (var kvp in overrides)
                MBTextManager_GetLocalizedText_Patch.RegisterOverride(kvp.Key, kvp.Value);
            logger0.LogInfo($"[LocalizationOverride] Registered {overrides.Count} English string overrides");
        }
        catch (System.Exception ex)
        {
            logger0.LogError($"[LocalizationOverride] Failed to load overrides: {ex.Message}");
        }

        _harmony.PatchCategory("Patch18_CulturalFeats");
        _harmony.PatchCategory("Patch19_CustomBattles");
        // Patch0_BattleScenes: loads DOTS's sp_battle_scenes.xml (full 0-255 map_indices coverage) so the
        // DOTS_Map Main_map grid's extended indices (158-255) resolve to real battle terrains instead of
        // FailedAsserting against vanilla's 1-157 table. Re-enabled 2026-06-01 (DOTS_Map ships Main_map +
        // the extended XML exists; 3 patch targets verified against installed 1.4.5). In-game grid validation
        // pending the worldmap_battle_scene_grid re-author. See docs/reference/worldmap-battle-scene-grid.md.
        _harmony.PatchCategory("Patch0_BattleScenes");
        // Remaining patches applied in OnGameInitializationFinished — View assembly must be initialized first

        var pathService = IoC.Resolve<IPathService>();
        var logger = IoC.Resolve<IModLogger>();
        FactionMapPaths.Initialize(pathService.ModuleRootPath, logger);

        var allianceHook = IoC.Resolve<IOnAllianceAction>();
        var peaceHook = IoC.Resolve<IOnPeaceAction>();
        DiplomacyIoC.InitializeHooks(allianceHook, peaceHook);
        AllianceCampaignBehavior_EndAlliance_Patch.Initialize(logger);
        AllianceCampaignBehavior_AddAllianceDecision_Patch.Initialize(logger);
        DeclareWarAction_ApplyInternal_Patch.Initialize(logger);
        MakePeaceAction_ApplyInternal_Patch.Initialize(logger);

        var executionHook = IoC.Resolve<IOnExecutionAction>();
        ExecutionIoC.InitializeHooks(executionHook);

        TroopWeightIoC.InitializeHooks(
            IoC.Resolve<IOnPartyBaseNumberOfAllMembers>(),
            IoC.Resolve<IOnPartyBaseNumberOfRegularMembers>(),
            IoC.Resolve<IOnRecruitmentVMRefreshPartyProperties>(),
            IoC.Resolve<IOnPartyVMPopulatePartyListLabel>(),
            IoC.Resolve<TroopWeightDisplayHook>());

        CustomBattlesIoC.InitializeHooks(
            IoC.Resolve<IOnGetCustomBattleCommanders>(),
            IoC.Resolve<IOnGetCustomBattleFactions>(),
            IoC.Resolve<IOnGetDefaultTroopOfFormation>(),
            IoC.Resolve<ISideCommanderFilter>(),
            logger);

        _harmony.PatchCategory("Patch21_ShaderPrecompilation");
        ShaderPrecompilationIoC.InitializeHooks(logger);

        _harmony.PatchCategory("Patch22_ArmyTargeting");
        _harmony.PatchCategory("Patch30_MixedFormations");
        // Patch_MissionTime_SetMovementOrder (shared by Patch31_SmartCavalryAI +
        // Patch35_CompanionTactics' Formation.SetMovementOrder hook) is applied in
        // OnMissionBehaviorInitialize — MovementOrder.cctor reads Mission.Current.CurrentTime,
        // which is null during OnSubModuleLoad and would crash JIT prep with NRE.

        var bannerColorConfig = IoC.Resolve<IBannerColorConfigProvider>();
        var bannerColorService = IoC.Resolve<IBannerColorService>();
        var bannerHeroAdapter = IoC.Resolve<IBannerHeroAdapter>();

        Banner_TryGetBannerDataFromCode_Transpiler.Initialize(bannerColorConfig, logger);
        Clan_UpdateBannerColorsAccordingToKingdom_Patch.Initialize(bannerColorService);
        Clan_UpdateBannerColorsAccordingToKingdom_Patch.Initialize(logger);
        Clan_UpdateBannerColor_Patch.Initialize(bannerColorService, bannerHeroAdapter);
        Banner_GetFirstIconColor_Patch.Initialize(bannerColorService);
        BannerEditorView_OnTick_Patch.Initialize(bannerColorService, logger);
        CampaignUIHelper_GetCharacterCode_Patch.Initialize(bannerColorService, bannerHeroAdapter);
        SandBoxUIHelper_GetCharacterCode_Patch.Initialize(bannerColorService, bannerHeroAdapter);
        SPInventoryVM_UpdateCurrentCharacterIfPossible_Patch.Initialize(bannerColorService, bannerHeroAdapter);
        PartyVM_RefreshCurrentCharacterInformation_Patch.Initialize(bannerColorService, bannerHeroAdapter);
        HeroViewModel_FillFrom_Patch.Initialize(bannerColorService, bannerHeroAdapter);
        PartyCharacterVM_GetCharacterCode_Patch.Initialize(bannerColorService, bannerHeroAdapter);
        ClanPartyItemVM_GetCharacterCode_Patch.Initialize(bannerColorService, bannerHeroAdapter);
        CampaignSceneNotificationHelper_CreateNotificationCharacter_Transpiler.Initialize(bannerColorService);
        var agentColorStore = IoC.Resolve<IAgentColorStore>();
        Mission_SpawnAgent_Patch.Initialize(bannerColorService, bannerHeroAdapter, agentColorStore);
        Agent_EquipItemsFromSpawnEquipment_Patch.Initialize(bannerColorService, bannerHeroAdapter, agentColorStore);
        AgentVisuals_Create_Patch.Initialize(bannerColorService);
        MapConversationTableau_SpawnOpponentLeader_Patch.Initialize(bannerColorService, bannerHeroAdapter);
        MapConversationTableau_SpawnOpponentBodyguard_Patch.Initialize(bannerColorService, bannerHeroAdapter);
        MobilePartyVisual_AddCharacterToPartyIcon_Patch.Initialize(bannerColorService, bannerHeroAdapter);
        OrderOfBattleHeroItemVM_RefreshInformation_Patch.Initialize(bannerColorService, bannerHeroAdapter);

        Mission_Initialize_Patch.Initialize(logger);

        // Patch42_CastleRecruitment — castle notable recruitment. Targets RecruitmentCampaignBehavior
        // + AiVisitSettlementBehavior (both in TaleWorlds.CampaignSystem, no View/Mission.cctor
        // dependency, safe in OnSubModuleLoad). The transpilers swap the AI IsCastle gate to a runtime
        // toggle; the postfix invokes the private CheckRecruiting for castles. All fail-safe.
        var castleRecruitmentSettings = IoC.Resolve<ICastleRecruitmentSettingsProvider>();
        CastleAiToggle.Initialize(castleRecruitmentSettings);
        Patch42_AiHourlyTick_Transpiler.Initialize(logger);
        Patch42_FillSettlements_Transpiler.Initialize(logger);
        Patch42_HourlyTickParty_Postfix.Initialize(castleRecruitmentSettings, logger);
        _harmony.PatchCategory("Patch42_CastleRecruitment");

        InformationManager.DisplayMessage(new InformationMessage("DOTS loaded successfully!", Colors.Green));
    }

    protected override void OnBeforeInitialModuleScreenSetAsRoot()
    {
        base.OnBeforeInitialModuleScreenSetAsRoot();
        IoC.Resolve<IMainMenuCustomizerService>().CustomizeMenu();

        // DISABLED 2026-05-22: Pre-compile Shaders main-menu button hidden — feature isn't 100% reliable yet.
        // The service, IoC registration, Harmony Patch21_ShaderPrecompilation, and the OnApplicationTick
        // in-game progress reporter (which uses _shaderTickAccumulator / _lastShaderCount) all remain
        // wired up — only this menu entry is hidden. Re-enable by removing the surrounding block-comment.
        /*
        if (Module.CurrentModule.GetInitialStateOptionWithId("DotsPrecompileShaders") == null)
        {
            var shaderService = IoC.Resolve<IShaderPrecompilationService>();
            var shaderLogger = IoC.Resolve<IModLogger>();
            Module.CurrentModule.AddInitialStateOption(new InitialStateOption(
                id:                  "DotsPrecompileShaders",
                name:                new TextObject("{=dots_precompile_shaders}Pre-compile Shaders"),
                orderIndex:          100,
                action:              () => InformationManager.ShowInquiry(new InquiryData(
                    "Shader Pre-compilation",
                    "This will load a battle scene with all DOTS troops to pre-compile shaders.\n\n" +
                    "THIS WILL TAKE A LONG TIME (20-70 minutes).\n\n" +
                    "This is a one-time process that eliminates in-game stutter and reduces crashes.\n" +
                    "When you see the deployment phase, the process is complete!",
                    true, true, "Start", "Cancel",
                    () =>
                    {
                        _shaderTickAccumulator = 0f;
                        _lastShaderCount = -1;
                        MBGameManager.StartNewGame(new DotsShaderGameManager(shaderService, shaderLogger));
                    },
                    () => InformationManager.HideInquiry())),
                isDisabledAndReason: () => (false, new TextObject("")),
                enabledHint:         new TextObject("{=dots_precompile_hint}Pre-compiles shaders to eliminate in-game stutter. Run once after installing DOTS."),
                isHidden:            null));
        }
        */
    }

    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
        base.OnGameStart(game, gameStarterObject);

        // Session-level diagnostic snapshot: OS / CLR / mod list / mod-stack
        // assembly versions / campaign context. Runs once per session and is
        // idempotent so OnGameStart on save-load doesn't spam.
        try
        {
            IoC.Resolve<Features.MissionDiagnostic.IMissionDiagnosticService>()?.LogSessionSnapshot();
        }
        catch { /* diagnostic is best-effort, never break OnGameStart */ }

        if (gameStarterObject is CampaignGameStarter campaignStarter)
        {
            var bannerInjectionService = IoC.Resolve<IBannerInjectionService>();
            var bannerExclusionService = IoC.Resolve<IBannerExclusionService>();
            campaignStarter.AddBehavior(new BannerInjectionBehavior(bannerInjectionService, bannerExclusionService));

            var ccContentService = IoC.Resolve<ICharacterCreationContentService>();
            var ccLogger = IoC.Resolve<IModLogger>();
            campaignStarter.AddBehavior(new CharacterCreationRegistrationBehavior(ccContentService, ccLogger));

            campaignStarter.RemoveBehaviors<InitialChildGenerationCampaignBehavior>();
            var childGenService = IoC.Resolve<IInitialChildGenerationService>();
            campaignStarter.AddBehavior(new DotsInitialChildGenerationBehavior(childGenService));

            var costService = IoC.Resolve<ITroopCostService>();
            // Phase 9b #173 — careerPassives resolved once for the whole CulturalFeats + CareerSystem
            // + TroopProgression model registration block. Replaces all CareerPassiveHelper static
            // calls with instance-injected ICareerPassiveService.
            var careerPassives = IoC.Resolve<DOTS.Features.CareerSystem.ICareerPassiveService>();
            // Phase 9b #180 / partial #148 — IWageModifierService extraction. Hoists garrison-wage
            // feat loop + Mordor/Gundabad/Umbar party-wage feats + Rohan mounted-wage scaling +
            // recruitment-cost feats out of the model body, satisfying gamemodels.md rule 4.
            var wageModifiers = IoC.Resolve<IWageModifierService>();
            var volunteerService = IoC.Resolve<IVolunteerTierService>();
            var recruitmentService = IoC.Resolve<IVolunteerRecruitmentService>();
            var volunteerContextAdapter = IoC.Resolve<IVolunteerContextAdapter>();
            // Hoisted: DotsVolunteerModel now consumes ICulturalFeatsService for the village
            // volunteer-respawn-rate feats (Dunland/Gundabad/Dol Guldur/Mordor). The later
            // "Cultural feat models" block reuses this same `culturalFeats` reference.
            var culturalFeats = IoC.Resolve<DOTS.Features.CulturalFeats.ICulturalFeatsService>();
            campaignStarter.AddModel(new DotsCharacterStatsModel());
            campaignStarter.AddModel(new DotsPartyWageModel(costService, careerPassives, wageModifiers));
            campaignStarter.AddModel(new DotsVolunteerModel(volunteerService, recruitmentService, volunteerContextAdapter, culturalFeats));

            var diplomacyService = IoC.Resolve<IDiplomacyService>();
            var wotrService = IoC.Resolve<IWarOfTheRingService>();
            var diplomacyLogger = IoC.Resolve<IModLogger>();
            campaignStarter.AddBehavior(new DiplomacyBehavior(diplomacyService, diplomacyLogger));
            campaignStarter.AddModel(new DotsDiplomacyModel(wotrService));

            var wotrLogger = IoC.Resolve<IModLogger>();
            campaignStarter.AddBehavior(new WarOfTheRingBehavior(wotrService, wotrLogger));

            var siegeDefenseService = IoC.Resolve<ISiegeDefenseService>();
            var siegeDefenseLogger = IoC.Resolve<IModLogger>();
            campaignStarter.AddBehavior(new SiegeDefenseBehavior(siegeDefenseService, siegeDefenseLogger));
            campaignStarter.AddModel(new DotsSiegeEventModel(IoC.Resolve<ISiegeEngineAvailabilityService>()));

            var executionRelationService = IoC.Resolve<IExecutionRelationService>();
            var playerContext = IoC.Resolve<IPlayerContextAdapter>();
            campaignStarter.AddModel(new DotsExecutionRelationModel(executionRelationService, playerContext));

            // Cultural feat models — Phase 9b #144/#176: dispatch logic extracted to
            // ICulturalFeatsService. Each model is now a thin boundary that converts
            // CultureObject → ICultureFeatAdapter and delegates (gamemodels.md rule 4).
            // `culturalFeats` is resolved above (hoisted for DotsVolunteerModel).
            campaignStarter.AddModel(new DotsArmyManagementModel(culturalFeats));
            campaignStarter.AddModel(new DotsPartySpeedModel(culturalFeats, careerPassives));
            campaignStarter.AddModel(new DotsSettlementProsperityModel(culturalFeats));
            campaignStarter.AddModel(new DotsSettlementMilitiaModel(culturalFeats));
            campaignStarter.AddModel(new DotsBuildingConstructionModel(culturalFeats));
            campaignStarter.AddModel(new DotsVillageProductionModel(culturalFeats));
            campaignStarter.AddModel(new DotsCaravanModel(culturalFeats));
            campaignStarter.AddModel(new DotsBattleRewardModel(culturalFeats, careerPassives));
            campaignStarter.AddModel(new DotsTournamentModel(IoC.Resolve<DOTS.Features.Arena.ITournamentService>()));
            campaignStarter.AddModel(new DotsPartyTroopUpgradeModel(culturalFeats, careerPassives));
            campaignStarter.AddModel(new DotsPartySizeModel(culturalFeats, careerPassives));
            campaignStarter.AddModel(new DotsFoodConsumptionModel(culturalFeats));
            campaignStarter.AddModel(new DotsSettlementLoyaltyModel(culturalFeats, IoC.Resolve<IRevoltTuningConfigProvider>()));
            campaignStarter.AddModel(new DotsBanditDensityModel(IoC.Resolve<IBanditScalingService>()));
            campaignStarter.AddModel(new DotsPartyMoraleModel(culturalFeats, careerPassives));
            campaignStarter.AddModel(new DotsSmithingModel(culturalFeats, careerPassives));
            campaignStarter.AddModel(new DotsClanFinanceModel(culturalFeats));
            campaignStarter.AddModel(new DotsRaidModel(culturalFeats, careerPassives));
            campaignStarter.AddModel(new DotsNotableSpawnModel(culturalFeats));

            // Battle balance models
            var battleBalanceSettings = IoC.Resolve<IBattleBalanceSettingsProvider>();
            var battleBalanceConfig = IoC.Resolve<IBattleBalanceConfigProvider>();
            campaignStarter.AddModel(new DotsMilitaryPowerModel(battleBalanceSettings, battleBalanceConfig));
            campaignStarter.AddModel(new DotsCombatSimulationModel(battleBalanceSettings));
            campaignStarter.AddModel(new DotsPartyHealingModel(battleBalanceSettings, battleBalanceConfig));

            campaignStarter.AddModel(new DotsInformationRestrictionModel(IoC.Resolve<IEncyclopediaSettingsProvider>()));

            var armyTargetingService = IoC.Resolve<IArmyTargetingService>();
            campaignStarter.AddModel(new DotsTargetScoreModel(armyTargetingService));

            var specialResourceService = IoC.Resolve<ISpecialResourceService>();
            var specialResourceStorage = IoC.Resolve<ISpecialResourceStorageService>();
            var specialResourceConfig = IoC.Resolve<ISpecialResourceConfigProvider>();
            var specialResourceLogger = IoC.Resolve<IModLogger>();
            var specialResourceBehavior = new SpecialResourcesBehavior(
                specialResourceService, specialResourceStorage, specialResourceConfig, specialResourceLogger);
            campaignStarter.AddBehavior(specialResourceBehavior);
            PartyScreenLogic_AddCommand_Patch.SetBehavior(specialResourceBehavior);

            var careerDataService = IoC.Resolve<ICareerDataService>();
            var careerRegistry = IoC.Resolve<ICareerRegistry>();
            var careerPassiveService = IoC.Resolve<ICareerPassiveService>();
            var careerLogger = IoC.Resolve<IModLogger>();
            campaignStarter.AddBehavior(new CareerPersistenceBehavior(careerDataService, careerLogger));
            var careerCreationHandler = IoC.Resolve<ICareerCreationHandler>();
            var careerAbilityServiceForBehavior = IoC.Resolve<Features.CareerSystem.Abilities.ICareerAbilityService>();
            campaignStarter.AddBehavior(new CareerCampaignBehavior(
                careerDataService, careerRegistry, careerPassiveService, careerCreationHandler, careerAbilityServiceForBehavior, careerLogger));

            var careerAdapterFactory = IoC.Resolve<ICareerHeroAdapterFactory>();
            // CareerSwitchDialogueBehavior used to take ICareerSwitchService too; that dependency
            // moved to GauntletCareerScreen.OnChooseSwitchTarget (Codex Review #32 cleanup).
            campaignStarter.AddBehavior(new CareerSwitchDialogueBehavior(
                careerDataService, careerRegistry, careerAdapterFactory, careerLogger));

            // Career-tied quest system (Phase 6) — offers/starts tier quests; CareerQuest : QuestBase
            // is registered for saving by the auto-discovered CareerQuestSaveableTypeDefiner.
            var careerQuestService = IoC.Resolve<Features.CareerSystem.ICareerQuestService>();
            campaignStarter.AddBehavior(new Features.CareerSystem.Quests.CareerQuestCampaignBehavior(
                careerDataService, careerQuestService, careerLogger));

            // Career system GameModels — reuse careerPassiveService resolved above (line 334).
            // Phase 9b #142 — agent-stat extraction: DotsAgentStatCalculateModel /
            // DotsAgentApplyDamageModel now delegate UpdateAgentStats + damage-amp/red +
            // shrug-off logic to ICareerAgentStatService (gamemodels.md rule 4).
            var careerAgentStat = IoC.Resolve<Features.CareerSystem.Abilities.ICareerAgentStatService>();
            campaignStarter.AddModel(new DotsMapVisibilityModel(careerPassives));
            campaignStarter.AddModel(new DotsInventoryCapacityModel(careerPassives));
            campaignStarter.AddModel<AgentStatCalculateModel>(new DotsAgentStatCalculateModel(careerPassiveService, careerAgentStat));
            campaignStarter.AddModel<AgentApplyDamageModel>(new DotsAgentApplyDamageModel(careerAgentStat));
            campaignStarter.AddModel(new DotsClanTierModel(careerPassiveService));

            var goldService = IoC.Resolve<IStartupGoldService>();
            var influenceService = IoC.Resolve<IStartupInfluenceService>();
            var startupLogger = IoC.Resolve<IModLogger>();
            campaignStarter.AddBehavior(new StartupResourcesBehavior(goldService, influenceService, startupLogger));

            var namedCompanionService = IoC.Resolve<INamedCompanionService>();
            campaignStarter.AddBehavior(new NamedCompanionBehavior(namedCompanionService));

            // QuickActions: per-save inventory-search-box persistence (SyncData round-trips
            // even when EnableInventorySearch is OFF — disabled = inert, not absent).
            campaignStarter.AddBehavior(IoC.Resolve<DOTS.Features.QuickActions.Hooks.InventorySearchCampaignBehavior>());

            // EquipPresets: per-save preset persistence + orphan pruning. Unconditional registration
            // so the SyncData round-trip preserves presets even when EnableEquipmentPresets is OFF
            // (the MCM hint promises "existing presets are inert (preserved in save)").
            campaignStarter.AddBehavior(IoC.Resolve<DOTS.Features.EquipPresets.Hooks.EquipmentPresetCampaignBehavior>());

            // FiefManagement (Patch36) — register UNCONDITIONALLY so the menu is always present
            // and the EnableFiefManagement MCM toggle takes effect immediately at runtime.
            campaignStarter.AddBehavior(new FiefHubCampaignBehavior(
                IoC.Resolve<IFiefHubMenuPresenter>(),
                IoC.Resolve<IFiefManagementSettingsProvider>()));

            // CompanionTactics (Patch35) — FormationPresets persistence behavior. Registered
            // unconditionally so SyncData round-trips even when EnableFormationPresets is OFF.
            campaignStarter.AddBehavior(new Features.CompanionTactics.FormationPresets.Hooks.FormationPresetCampaignBehavior(
                IoC.Resolve<Features.CompanionTactics.FormationPresets.IFormationPresetService>(),
                IoC.Resolve<IModLogger>()));

            // Messengers — paid messenger dispatch + dialog hooks + per-save SyncData persistence.
            // Registered unconditionally so saves round-trip pending messengers even when
            // EnableMessengers is OFF (disabled = inert, not absent).
            campaignStarter.AddBehavior(IoC.Resolve<DOTS.Features.Messengers.MessengerCampaignBehavior>());

            // CultureMarketplace (#207) — daily injection of LOTRLOME items into town markets
            // keyed by owner culture. No SyncData (stock lives in vanilla Settlement.ItemRoster).
            campaignStarter.AddBehavior(new Features.CultureMarketplace.CultureMarketplaceBehavior(
                IoC.Resolve<Features.CultureMarketplace.ICultureItemPoolService>(),
                IoC.Resolve<Features.CultureMarketplace.ICultureMarketplaceInjectionService>(),
                IoC.Resolve<Features.CultureMarketplace.ICultureMarketplaceMaintenanceService>(),
                IoC.Resolve<ITownRosterAdapter>(),
                IoC.Resolve<Features.CultureMarketplace.Domain.MarketplaceTuning>(),
                IoC.Resolve<IModLogger>()));

            // CastleRecruitment (Patch42) — castle notable population + maintenance + volunteer fill +
            // player "Recruit troops" castle menu + issue/quest suppression for castle notables.
            // Registered unconditionally so the MCM master toggle takes effect at runtime.
            campaignStarter.AddBehavior(new CastleRecruitmentBehavior(
                IoC.Resolve<ICastleRecruitmentService>(),
                IoC.Resolve<IModLogger>()));

            // CultureConversion — conquered cross-culture fiefs gradually adopt the new owner's culture
            // (troops, militia, identity). Registered unconditionally so SyncData round-trips conversion
            // records and completed overrides re-apply on load even when the MCM toggle is off.
            campaignStarter.AddBehavior(new Features.CultureConversion.Hooks.CultureConversionBehavior(
                IoC.Resolve<Features.CultureConversion.ICultureConversionService>(),
                IoC.Resolve<Features.CultureConversion.ICultureConversionStore>(),
                IoC.Resolve<IModLogger>()));
        }
    }

    public override void OnGameInitializationFinished(Game game)
    {
        base.OnGameInitializationFinished(game);

        _harmony.PatchCategory("Patch1_FirstTimeInit");
        _harmony.PatchCategory("Patch2_RefreshTableau");
        _harmony.PatchCategory("Patch4_CharacterSpawner");
        _harmony.PatchCategory("Patch5_FaceGen");
        _harmony.PatchCategory("Late_Transpiler");
        _harmony.PatchCategory("Late_ActionSetOverride");
        _harmony.PatchCategory("Patch6_BannerEditor");
        _harmony.PatchCategory("Patch7_FactionMap");
        _harmony.PatchCategory("Patch20_NarrativeHorseGuard");
        _harmony.PatchCategory("Patch8_SiegeCampGuard");
        _harmony.PatchCategory("Patch10_WeatherBoundsGuard");
        _harmony.PatchCategory("Patch11_Diplomacy");
        _harmony.PatchCategory("Patch12_WarOfTheRing");

        _harmony.PatchCategory("Patch14_Execution");
        _harmony.PatchCategory("Patch15_BannerLayerLimit");
        _harmony.PatchCategory("Patch16_AtmospherePersistence");
        _harmony.PatchCategory("Patch17_TroopWeight");
        _harmony.PatchCategory("Patch23_BannerColorPersistence");
        _harmony.PatchCategory("Patch24_BannerDriftGuard");
        _harmony.PatchCategory("Patch39_BanditPartySize");
        _harmony.PatchCategory("Patch40_HideoutDescription");

        var resourceHook = IoC.Resolve<IOnPartyUpgradeResourceCheck>();
        var specResLogger = IoC.Resolve<IModLogger>();
        PartyCharacterVM_InitializeUpgrades_Patch.Initialize(resourceHook, specResLogger);
        PartyScreenLogic_UpgradeTroop_Patch.Initialize(resourceHook, specResLogger);
        PartyScreenLogic_AddCommand_Patch.Initialize(resourceHook, specResLogger);
        _harmony.PatchCategory("Patch26_SpecialResources");
        _harmony.PatchCategory("Patch27_CareerSystem");
        _harmony.PatchCategory("Patch29_CCBodyProperties");
        _harmony.PatchCategory("Patch44_CCNameAutofill");
        _harmony.PatchCategory("Patch33_EquipPresets");
        _harmony.PatchCategory("Patch34_QuickActions");
        _harmony.PatchCategory("Patch35_CompanionTactics");
        _harmony.PatchCategory("Patch36_FiefManagement");
        SettlementNameplateWidget_DetermineTargetAlphaValue_Patch.Initialize(IoC.Resolve<INameplateFadeService>());
        _harmony.PatchCategory("Patch38_SettlementNameplateFade");

        // BattleLoadDiagnostics — phase-stamp the attack->battle-playable lifecycle so an
        // intermittent battle-load hang leaves a log whose last line names the stuck phase
        // (and, for the equipment phase, the agent + the item whose bo_ collision mesh is
        // missing). The background stall watchdog auto-triggers a crash bundle on a freeze.
        var battleLoadSvc = IoC.Resolve<Features.BattleLoadDiagnostics.IBattleLoadDiagnosticsService>();
        var equipSnapshotAdapter = IoC.Resolve<IEquipmentSnapshotAdapter>();
        Features.BattleLoadDiagnostics.Hooks.PlayerEncounter_Start_Patch.Initialize(battleLoadSvc);
        Features.BattleLoadDiagnostics.Hooks.MissionState_OpenNew_Patch.Initialize(battleLoadSvc);
        Features.BattleLoadDiagnostics.Hooks.BattleSceneSelection_Patch.Initialize(battleLoadSvc);
        Features.BattleLoadDiagnostics.Hooks.Mission_Initialize_BattleLoad_Patch.Initialize(battleLoadSvc);
        Features.BattleLoadDiagnostics.Hooks.Agent_EquipItemsFromSpawnEquipment_BattleLoad_Patch.Initialize(battleLoadSvc, equipSnapshotAdapter);
        _harmony.PatchCategory("Patch43_BattleLoadDiagnostics");
        IoC.Resolve<Features.BattleLoadDiagnostics.BattleLoadStallWatchdog>().Start();

        // CompanionTactics — manual patch for the PRIVATE method
        // OrderOfBattleHeroItemVM.GetCaptainTooltip (private in v1.3.15, can't use
        // [HarmonyPatch] attribute binding).
        var captainTooltipTarget = AccessTools.Method(typeof(OrderOfBattleHeroItemVM), "GetCaptainTooltip");
        if (captainTooltipTarget != null)
            _harmony.Patch(captainTooltipTarget, postfix: new HarmonyMethod(
                typeof(Features.CompanionTactics.Roles.Hooks.Patch35_OOBHeroItem_GetCaptainTooltip),
                nameof(Features.CompanionTactics.Roles.Hooks.Patch35_OOBHeroItem_GetCaptainTooltip.Postfix)));
        else
            IoC.Resolve<IModLogger>().LogWarning("[CompanionTactics] OrderOfBattleHeroItemVM.GetCaptainTooltip not found — captain tooltip role hint will not appear");

        var settlementGuardService = IoC.Resolve<ISettlementGuardService>();
        GuardsCampaignBehavior_TakeGuardAgentData_Patch.Initialize(settlementGuardService);
        GuardsCampaignBehavior_GetSuitableSpear_Patch.Initialize(settlementGuardService);

        // Manual patches for private GuardsCampaignBehavior methods (SandBox.dll)
        var takeGuardTarget = GuardsCampaignBehavior_TakeGuardAgentData_Patch.TargetMethod();
        if (takeGuardTarget != null)
            _harmony.Patch(takeGuardTarget, prefix: new HarmonyMethod(
                typeof(GuardsCampaignBehavior_TakeGuardAgentData_Patch),
                nameof(GuardsCampaignBehavior_TakeGuardAgentData_Patch.Prefix)));
        else
            IoC.Resolve<IModLogger>().LogWarning("[SettlementGuards] TakeGuardAgentDataFromGarrisonTroopList not found — custom guards will not apply");

        var spearTarget = GuardsCampaignBehavior_GetSuitableSpear_Patch.TargetMethod();
        if (spearTarget != null)
            _harmony.Patch(spearTarget, prefix: new HarmonyMethod(
                typeof(GuardsCampaignBehavior_GetSuitableSpear_Patch),
                nameof(GuardsCampaignBehavior_GetSuitableSpear_Patch.Prefix)));
        else
            IoC.Resolve<IModLogger>().LogWarning("[SettlementGuards] GetSuitableSpear not found — culture-specific spears will not apply");

        // Manual patch for private MobilePartyVisual method (SandBox.View.dll)
        var mobilePartyTarget = MobilePartyVisual_AddCharacterToPartyIcon_Patch.TargetMethod();
        if (mobilePartyTarget != null)
            _harmony.Patch(mobilePartyTarget, postfix: new HarmonyMethod(
                typeof(MobilePartyVisual_AddCharacterToPartyIcon_Patch),
                nameof(MobilePartyVisual_AddCharacterToPartyIcon_Patch.Postfix)));
        else
            IoC.Resolve<IModLogger>().LogWarning("[BannerColor] MobilePartyVisual.AddCharacterToPartyIcon not found — party icon colors will not persist");

        // Manual patch for AgentVisuals.Create (TaleWorlds.MountAndBlade.View.dll)
        var agentVisualsCreateTarget = AgentVisuals_Create_Patch.TargetMethod();
        if (agentVisualsCreateTarget != null)
            _harmony.Patch(agentVisualsCreateTarget, prefix: new HarmonyMethod(
                typeof(AgentVisuals_Create_Patch),
                nameof(AgentVisuals_Create_Patch.Prefix)));
        else
            IoC.Resolve<IModLogger>().LogWarning("[BannerColor] AgentVisuals.Create not found — clan color randomness suppression will not apply");

        // Manual patches for MapConversationTableau (private methods in SandBox.View.dll)
        var leaderTarget = MapConversationTableau_SpawnOpponentLeader_Patch.TargetMethod();
        if (leaderTarget != null)
            _harmony.Patch(leaderTarget, postfix: new HarmonyMethod(
                typeof(MapConversationTableau_SpawnOpponentLeader_Patch),
                nameof(MapConversationTableau_SpawnOpponentLeader_Patch.Postfix)));
        else
            IoC.Resolve<IModLogger>().LogWarning("[BannerColor] MapConversationTableau.SpawnOpponentLeader not found — conversation tableau leader colors will not apply");

        var bodyguardTarget = MapConversationTableau_SpawnOpponentBodyguard_Patch.TargetMethod();
        if (bodyguardTarget != null)
            _harmony.Patch(bodyguardTarget, postfix: new HarmonyMethod(
                typeof(MapConversationTableau_SpawnOpponentBodyguard_Patch),
                nameof(MapConversationTableau_SpawnOpponentBodyguard_Patch.Postfix)));
        else
            IoC.Resolve<IModLogger>().LogWarning("[BannerColor] MapConversationTableau.SpawnOpponentBodyguardCharacter not found — conversation tableau bodyguard colors will not apply");
    }

    public override void OnMissionBehaviorInitialize(Mission mission)
    {
        base.OnMissionBehaviorInitialize(mission);

        // Apply Formation.SetMovementOrder patches (Patch31_SmartCavalryAI + Patch35
        // CancelStanceOnMove) only once Mission.Current is non-null — MovementOrder's
        // type initializer constructs static fields whose ctor reads
        // Mission.Current.CurrentTime. Applying earlier crashes JIT prep with NRE.
        if (!_missionTimePatchesApplied)
        {
            _missionTimePatchesApplied = true;
            _harmony.PatchCategory("Patch_MissionTime_SetMovementOrder");
        }

        mission.AddMissionBehavior(new AdvancedCombatBehavior());
        mission.AddMissionBehavior(new BehaviorTreeMissionLogic());
        mission.AddMissionBehavior(new AutonomousMovementPlayerController());
        mission.AddMissionBehavior(new SiegeDismountMissionBehavior());
        mission.AddMissionBehavior(new MixedFormationsMissionBehavior());
        mission.AddMissionBehavior(new SmartCavalryAIMissionBehavior());
        mission.AddMissionBehavior(new Features.CompanionTactics.BattleActionBar.Hooks.BattleActionBarMissionView());

        var colorStore = IoC.Resolve<IAgentColorStore>();
        if (colorStore != null)
            mission.AddMissionBehavior(new AgentColorStoreCleanupBehavior(colorStore));

        // MissionDiagnostic: added LAST so it sees all behaviors added by DOTS AND
        // every other mod in the load chain. Dumps MissionBehaviors + MissionLogics
        // on first OnMissionTick to dots_debug_*.log so user-uploaded crash logs
        // contain enough data to identify mod-conflict bugs (BehaviorType=Logic +
        // !MissionLogic null-cast offenders) and action-set anomalies.
        var diagSvc = IoC.Resolve<Features.MissionDiagnostic.IMissionDiagnosticService>();
        var diagLogger = IoC.Resolve<IModLogger>();
        if (diagSvc != null && diagLogger != null)
            mission.AddMissionBehavior(new Features.MissionDiagnostic.Hooks.MissionDiagnosticBehavior(diagSvc, diagLogger));

        // BattleLoadDiagnostics phase-6: "battle playable" marker on first tick + closes
        // the loading window so the stall watchdog stands down and phase-5 stops logging.
        var battleLoadDiagSvc = IoC.Resolve<Features.BattleLoadDiagnostics.IBattleLoadDiagnosticsService>();
        if (battleLoadDiagSvc != null && battleLoadDiagSvc.IsEnabled)
            mission.AddMissionBehavior(new Features.BattleLoadDiagnostics.Hooks.BattleLoadPhaseBehavior(battleLoadDiagSvc));

        // Dev-trigger behavior watches the CrashReport MCM toggle and throws a tagged
        // DotsDevTriggerException on the next OnMissionTick when the player flips
        // "Throw On Next Mission Tick". QA only — no-op in normal play.
        mission.AddMissionBehavior(new Features.CrashReport.DevTriggers.CrashReportDevTriggerMissionBehavior());

        var careerAbilityService = IoC.Resolve<Features.CareerSystem.Abilities.ICareerAbilityService>();
        if (careerAbilityService != null && Campaign.Current != null)
        {
            mission.AddMissionBehavior(new Features.CareerSystem.CareerPerkMissionBehavior(
                IoC.Resolve<ICareerDataService>(),
                careerAbilityService,
                IoC.Resolve<Features.CareerSystem.Abilities.IAbilityActivationController>(),
                IoC.Resolve<Features.CareerSystem.UI.IAbilityHudController>(),
                IoC.Resolve<Features.CareerSystem.Abilities.IAbilityEffectExecutor>(),
                IoC.Resolve<IModLogger>()));
        }
    }

    protected override void OnApplicationTick(float dt)
    {
        _timeAccelerationService?.OnTick();

        _shaderTickAccumulator += dt;
        if (_shaderTickAccumulator >= 1f)
        {
            _shaderTickAccumulator = 0f;

            if (!LoadingWindow.IsLoadingWindowActive)
            {
                int count = Utilities.GetNumberOfShaderCompilationsInProgress();
                if (count > 0 && count != _lastShaderCount)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Shader compilation in progress. Remaining: {count}"));
                }
                _lastShaderCount = count;
            }
        }
    }

    protected override void OnSubModuleUnloaded()
    {
        base.OnSubModuleUnloaded();
        // Detach the AppDomain.UnhandledException subscription BEFORE IoC disposal so
        // the hook doesn't hold a stale reference to a disposed CrashReportService
        // across game-restart-in-same-process. Deep-review INC 3 (2026-05-25).
        try { IoC.Resolve<DOTS.Features.CrashReport.Hooks.AppDomainExceptionHook>()?.Unsubscribe(); }
        catch { /* IoC may already be torn down — best-effort */ }

        _harmony?.UnpatchAll("com.dots.mod");
        IoC.Dispose();

        // Codex review #46 (2026-05-25) HIGH-01: clear the static service cache in
        // the patch helper so the next module load resolves a fresh service graph from
        // the new IoC container. Without this, Finalizers fire against a disposed
        // FileLogger after reload and silently drop every log line.
        DOTS.Features.CrashReport.Hooks.CrashReportPatchHelper.ResetForUnload();
    }
}
