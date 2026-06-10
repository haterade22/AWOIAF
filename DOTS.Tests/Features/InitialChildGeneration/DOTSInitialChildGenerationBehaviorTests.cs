using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using DOTS.Features.InitialChildGeneration;

namespace DOTS.Tests.Features.InitialChildGeneration;

[TestClass]
public class DotsInitialChildGenerationBehaviorTests
{
    private IInitialChildGenerationService _service;
    private DotsInitialChildGenerationBehavior _sut;

    [TestInitialize]
    public void Setup()
    {
        _service = Substitute.For<IInitialChildGenerationService>();
        _sut = new DotsInitialChildGenerationBehavior(_service);
    }

    [TestMethod]
    public void OnNewGameCreatedPartialFollowUp_Index0_CallsService()
    {
        _sut.OnNewGameCreatedPartialFollowUp(null, 0);

        _service.Received(1).GenerateInitialChildren();
    }

    [TestMethod]
    public void OnNewGameCreatedPartialFollowUp_IndexNot0_DoesNotCallService()
    {
        _sut.OnNewGameCreatedPartialFollowUp(null, 1);
        _sut.OnNewGameCreatedPartialFollowUp(null, 5);

        _service.DidNotReceive().GenerateInitialChildren();
    }
}
