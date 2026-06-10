using DryIoc;
using System.Collections.Generic;
using DOTS.Adapters;
using DOTS.Core.Infrastructure;
using DOTS.Core.Logging;
using DOTS.Features.BannerColorPersistence;
using DOTS.Features.BannerInjection;
using DOTS.Features.CharacterCreation;
using DOTS.Features.FactionMap;
using DOTS.Features.InitialChildGeneration;
using DOTS.Features.Diplomacy;
using DOTS.Features.Encyclopedia;
using DOTS.Features.Execution;
using DOTS.Features.StartupResources;
using DOTS.Features.TroopProgression;
using DOTS.Features.AdvancedCombat;
using DOTS.Features.CustomBattles;
using DOTS.Features.TroopWeight;
using DOTS.Features.BattleBalance;
using DOTS.Features.MainMenuCustomizer;
using DOTS.Features.Messengers;
using DOTS.Features.ShaderPrecompilation;
using DOTS.Features.Siege;
using DOTS.Features.ArmyTargeting;
using DOTS.Features.TimeAcceleration;
using DOTS.Features.SpecialResources;
using DOTS.Features.CareerSystem;
using DOTS.Features.SettlementGuards;
using DOTS.Features.NamedCompanions;
using DOTS.Features.RevoltTuning;
using DOTS.Features.BanditManagement;
using DOTS.Features.CastleRecruitment;
using DOTS.Features.SiegeDismount;
using DOTS.Features.MixedFormations;
using DOTS.Features.SmartCavalryAI;
using DOTS.Features.QuickActions;
using DOTS.Features.CompanionTactics;
using DOTS.Features.FiefManagement;
using DOTS.Features.EquipPresets;
using DOTS.Features.EditorCacheRebuild;
using DOTS.Features.CulturalFeats;
using DOTS.Features.CultureMarketplace;
using DOTS.Features.CultureConversion;
using DOTS.Features.MissionDiagnostic;
using DOTS.Features.CrashReport;
using DOTS.Features.BattleLoadDiagnostics;
using DOTS.Features.SettlementNameplateFade;

namespace DOTS;

public static class IoC
{
    private static IContainer _container;

    public static void Configure()
    {
        var container = new Container();

        container.RegisterInstance<IContainer>(container);

        RegisterCoreServices(container);
        RegisterLoggingServices(container);

        BannerInjectionIoC.RegisterBannerInjectionFeature(container);
        BannerColorPersistenceIoC.RegisterBannerColorPersistenceFeature(container);
        TroopProgressionIoC.RegisterTroopProgressionFeature(container);
        Features.Arena.ArenaIoC.RegisterArenaFeature(container);
        FactionMapIoC.RegisterFactionMapFeature(container);
        CharacterCreationIoC.RegisterCharacterCreationFeature(container);
        InitialChildGenerationIoC.RegisterInitialChildGenerationFeature(container);
        DiplomacyIoC.RegisterDiplomacyFeature(container);
        ExecutionIoC.RegisterExecutionFeature(container);
        EncyclopediaIoC.RegisterEncyclopediaFeature(container);
        StartupResourcesIoC.RegisterStartupResourcesFeature(container);
        TroopWeightIoC.RegisterTroopWeightFeature(container);
        AdvancedCombatIoC.RegisterAdvancedCombatFeature(container);
        CustomBattlesIoC.RegisterCustomBattlesFeature(container);
        BattleBalanceIoC.RegisterBattleBalanceFeature(container);
        MainMenuCustomizerIoC.RegisterMainMenuCustomizerFeature(container);
        ShaderPrecompilationIoC.RegisterShaderPrecompilationFeature(container);
        SiegeDefenseIoC.RegisterSiegeDefenseFeature(container);
        ArmyTargetingIoC.RegisterArmyTargetingFeature(container);
        TimeAccelerationIoC.RegisterTimeAccelerationFeature(container);
        SpecialResourcesIoC.RegisterSpecialResourcesFeature(container);
        CareerSystemIoC.RegisterCareerSystemFeature(container);
        SettlementGuardsIoC.RegisterSettlementGuardsFeature(container);
        NamedCompanionIoC.RegisterNamedCompanionsFeature(container);
        RevoltTuningIoC.RegisterRevoltTuningFeature(container);
        BanditManagementIoC.RegisterBanditManagementFeature(container);
        CastleRecruitmentIoC.RegisterCastleRecruitmentFeature(container);
        SiegeDismountIoC.RegisterSiegeDismountFeature(container);
        MixedFormationsIoC.RegisterMixedFormationsFeature(container);
        SmartCavalryAIIoC.RegisterSmartCavalryAIFeature(container);
        QuickActionsIoC.RegisterQuickActionsFeature(container);
        MessengerIoC.RegisterMessengerFeature(container);
        EquipPresetsIoC.RegisterEquipPresetsFeature(container);
        CompanionTacticsIoC.RegisterCompanionTacticsFeature(container);
        FiefManagementIoC.RegisterFiefManagementFeature(container);
        EditorCacheRebuildIoC.RegisterEditorCacheRebuildFeature(container);
        CulturalFeatsIoC.RegisterCulturalFeatsFeature(container);
        CultureMarketplaceIoC.RegisterCultureMarketplaceFeature(container);
        CultureConversionIoC.RegisterCultureConversionFeature(container);
        MissionDiagnosticIoC.RegisterMissionDiagnosticFeature(container);
        CrashReportIoC.RegisterCrashReportFeature(container);
        BattleLoadDiagnosticsIoC.RegisterBattleLoadDiagnosticsFeature(container);
        NameplateFadeIoC.RegisterSettlementNameplateFadeFeature(container);

        _container = container;

        // Post-registration initialization
        CareerSystemIoC.InitializeCalculators(container.Resolve<Features.CareerSystem.Mutations.IMutationCalculatorRegistry>());
    }

    private static void RegisterCoreServices(IContainer container)
    {
        container.Register<IModulePathAdapter, ModulePathAdapter>(Reuse.Singleton);
        container.Register<IFaceGenAdapter, FaceGenAdapter>(Reuse.Singleton);
        container.Register<IPathService, PathService>(Reuse.Singleton);
        container.Register<IReflectionService, ReflectionService>(Reuse.Singleton);
        container.Register<IHeroRosterAdapter, HeroRosterAdapter>(Reuse.Singleton);
        // Phase 9b #148 P2 — IVolunteerContextAdapter moved to TroopProgressionIoC for cohesion
        // (only consumer is DotsVolunteerModel inside that feature).
        container.Register<IMissionAdapterFactory, MissionAdapterFactory>(Reuse.Singleton);
        container.Register<IObjectManagerAdapter, ObjectManagerAdapter>(Reuse.Singleton);
        container.Register<ICampaignSessionAdapter, CampaignSessionAdapter>(Reuse.Singleton);
    }

    private static void RegisterLoggingServices(IContainer container)
    {
        container.Register<IModLogger, FileLogger>(Reuse.Singleton);
    }

    public static T Resolve<T>()
    {
        return _container.Resolve<T>();
    }

    public static IEnumerable<T> ResolveAll<T>()
    {
        return _container.ResolveMany<T>();
    }

    public static void Dispose()
    {
        _container?.Dispose();
        _container = null;
    }
}
