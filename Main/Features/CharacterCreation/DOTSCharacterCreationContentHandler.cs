using TaleWorlds.CampaignSystem.CharacterCreationContent;
using DOTS.Core.Logging;

namespace DOTS.Features.CharacterCreation;

public class DotsCharacterCreationContentHandler : ICharacterCreationContentHandler
{
    private readonly ICharacterCreationContentService _contentService;
    private readonly IModLogger _logger;

    public DotsCharacterCreationContentHandler(
        ICharacterCreationContentService contentService,
        IModLogger logger)
    {
        _contentService = contentService;
        _logger = logger;
    }

    public void InitializeContent(CharacterCreationManager characterCreationManager)
    {
        // SandBox handler (priority 800) runs first and registers vanilla cultures + stages.
        // We do nothing here — all DOTS work happens in AfterInitializeContent.
    }

    public void AfterInitializeContent(CharacterCreationManager characterCreationManager)
    {
        _contentService.RegisterCustomCultures(characterCreationManager);
        _contentService.RegisterNarrativeMenus(characterCreationManager);
        _contentService.RegisterCareerMenu(characterCreationManager);
        _logger.LogInfo("DOTS character creation content initialized");
    }

    public void OnStageCompleted(CharacterCreationStageBase stage)
    {
        // Future: track stage completions if needed
    }

    public void OnCharacterCreationFinalize(CharacterCreationManager characterCreationManager)
    {
        _contentService.OnCharacterCreationFinalize(characterCreationManager);
    }
}
