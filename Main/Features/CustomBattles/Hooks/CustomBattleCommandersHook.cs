using System.Collections.Generic;
using System.Linq;
using DOTS.Adapters;
using DOTS.Core.Logging;
using TaleWorlds.Core;

namespace DOTS.Features.CustomBattles.Hooks;

public class CustomBattleCommandersHook : IOnGetCustomBattleCommanders
{
    private readonly ICustomBattleService _service;
    private readonly IObjectManagerAdapter _objectManager;
    private readonly IModLogger _logger;

    public CustomBattleCommandersHook(
        ICustomBattleService service,
        IObjectManagerAdapter objectManager,
        IModLogger logger)
    {
        _service = service;
        _objectManager = objectManager;
        _logger = logger;
    }

    public void OnGetCustomBattleCommanders(ref IEnumerable<BasicCharacterObject> commanders)
    {
        var commanderIds = _service.GetCommanderIds();
        var resolved = commanderIds
            .Select(id => _objectManager.GetBasicCharacter(id))
            .Where(c => c != null)
            .ToList();

        _logger.LogInfo($"CustomBattleCommandersHook: Loaded {resolved.Count} DOTS commanders");
        commanders = resolved;
    }
}
