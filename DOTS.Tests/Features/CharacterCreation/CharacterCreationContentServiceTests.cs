using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using DOTS.Adapters;
using DOTS.Core.Logging;
using DOTS.Features.CharacterCreation;
using DOTS.Features.CharacterCreation.Models;
using DOTS.Features.StartupResources;

namespace DOTS.Tests.Features.CharacterCreation;

[TestClass]
public class CharacterCreationContentServiceTests
{
    private ICultureCreationDataProvider _dataProvider;
    private INarrativeDataProvider _narrativeDataProvider;
    private IHeroRosterAdapter _heroRosterAdapter;
    private IEquipmentRosterProvider _equipmentRosterProvider;
    private ICareerMenuService _careerMenuService;
    private IPlayerStartupGoldService _playerStartupGoldService;
    private IPlayerEquipmentService _playerEquipmentService;
    private IModLogger _logger;
    private CharacterCreationContentService _sut;

    [TestInitialize]
    public void Setup()
    {
        _dataProvider = Substitute.For<ICultureCreationDataProvider>();
        _narrativeDataProvider = Substitute.For<INarrativeDataProvider>();
        _heroRosterAdapter = Substitute.For<IHeroRosterAdapter>();
        _equipmentRosterProvider = Substitute.For<IEquipmentRosterProvider>();
        _careerMenuService = Substitute.For<ICareerMenuService>();
        _playerStartupGoldService = Substitute.For<IPlayerStartupGoldService>();
        _playerEquipmentService = Substitute.For<IPlayerEquipmentService>();
        var careerStartingEquipmentService = Substitute.For<ICareerStartingEquipmentService>();
        _logger = Substitute.For<IModLogger>();
        // Phase 9b #125 — career deps constructor-injected (formerly IoC.Resolve).
        var careerHandler = Substitute.For<DOTS.Features.CareerSystem.ICareerCreationHandler>();
        var careerRegistry = Substitute.For<DOTS.Features.CareerSystem.ICareerRegistry>();

        _sut = new CharacterCreationContentService(
            _dataProvider,
            _narrativeDataProvider,
            _heroRosterAdapter,
            _equipmentRosterProvider,
            _careerMenuService,
            _playerStartupGoldService,
            _playerEquipmentService,
            careerStartingEquipmentService,
            careerHandler,
            careerRegistry,
            _logger);
    }

    // --- SetPlayerRace ---
    // GoT is human-only: SetPlayerRace always sets raceId=0 regardless of culture/input.

    [TestMethod]
    public void SetPlayerRace_AnyCulture_SetsHumanRace()
    {
        var cultureData = new CultureCreationData { CultureId = "baratheon" };

        _sut.SetPlayerRace(cultureData, "main_hero_id");

        _heroRosterAdapter.Received(1).SetHeroRace("main_hero_id", 0);
    }

    [TestMethod]
    public void SetPlayerRace_LogsHumanAssignment()
    {
        var cultureData = new CultureCreationData { CultureId = "stark" };

        _sut.SetPlayerRace(cultureData, "hero_id");

        _logger.Received().LogInfo(Arg.Is<string>(s =>
            s.Contains("human") && s.Contains("0")));
    }

    [TestMethod]
    [DataRow("empire")]
    [DataRow("vlandia")]
    [DataRow("sturgia")]
    [DataRow("aserai")]
    [DataRow("battania")]
    [DataRow("khuzait")]
    public void SetPlayerRace_VanillaCulture_SetsHumanRace(string cultureId)
    {
        var cultureData = new CultureCreationData { CultureId = cultureId };

        _sut.SetPlayerRace(cultureData, "hero_id");

        _heroRosterAdapter.Received(1).SetHeroRace("hero_id", 0);
    }

    [TestMethod]
    public void SetPlayerRace_NullHeroId_SkipsWithoutSetting()
    {
        var cultureData = new CultureCreationData { CultureId = "lannister" };

        _sut.SetPlayerRace(cultureData, null);

        _heroRosterAdapter.DidNotReceive().SetHeroRace(Arg.Any<string>(), Arg.Any<int>());
    }

    [TestMethod]
    public void SetPlayerRace_EmptyHeroId_SkipsWithoutSetting()
    {
        var cultureData = new CultureCreationData { CultureId = "lannister" };

        _sut.SetPlayerRace(cultureData, "");

        _heroRosterAdapter.DidNotReceive().SetHeroRace(Arg.Any<string>(), Arg.Any<int>());
    }
}
