using DOTS.Core.Logging;

namespace DOTS.Features.MainMenuCustomizer;

public class MainMenuCustomizerService : IMainMenuCustomizerService
{
    private readonly IModuleMenuAdapter _moduleMenuAdapter;
    private readonly IModLogger _logger;

    public MainMenuCustomizerService(IModuleMenuAdapter moduleMenuAdapter, IModLogger logger)
    {
        _moduleMenuAdapter = moduleMenuAdapter;
        _logger = logger;
    }

    public void CustomizeMenu()
    {
        _moduleMenuAdapter.HideOption("StoryModeNewGame");
        _moduleMenuAdapter.RenameOption("SandBoxNewGame", "{=dots_main_menu_new_game}Enter The Age Of Men");
        _logger.LogInfo("MainMenuCustomizer: menu customization applied");
    }
}
