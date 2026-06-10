using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using DOTS.Core.Infrastructure;

namespace DOTS.Tests.Core.Infrastructure.Path;

[TestClass]
public class PathServiceTests
{
    private PathService _sut;
    private IModulePathAdapter _modulePathAdapter;

    [TestInitialize]
    public void Setup()
    {
        _modulePathAdapter = Substitute.For<IModulePathAdapter>();
        _modulePathAdapter.GetModuleFullPath("DOTS").Returns("C:/Game/Modules/DOTS/");
        _sut = new PathService(_modulePathAdapter);
    }

    [TestMethod]
    public void ModuleRootPath_ReturnsAdapterPath()
    {
        var result = _sut.ModuleRootPath;

        Assert.AreEqual("C:/Game/Modules/DOTS/", result);
    }

    [TestMethod]
    public void ModuleDataPath_AppendsModuleData()
    {
        var result = _sut.ModuleDataPath;

        Assert.AreEqual("C:/Game/Modules/DOTS/ModuleData/", result);
    }

    [TestMethod]
    public void ConfigPath_AppendsConfigs()
    {
        var result = _sut.ConfigPath;

        Assert.AreEqual("C:/Game/Modules/DOTS/ModuleData/configs/", result);
    }

    [TestMethod]
    public void ModuleRootPath_CallsAdapterWithCorrectModuleName()
    {
        _ = _sut.ModuleRootPath;

        _modulePathAdapter.Received(1).GetModuleFullPath("DOTS");
    }
}
