using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using DOTS.Adapters;
using DOTS.Features.FiefManagement.Models;
using DOTS.Features.FiefManagement.UI;

namespace DOTS.Features.FiefManagement.Hooks;

[HarmonyPatch(typeof(GameStateScreenManager), "CreateScreen")]
[HarmonyPatchCategory("Patch36_FiefManagement")]
public static class Patch36_GameStateScreenManager
{
    private static IRemoteFiefSettlementSwapper _swapper;

    [HarmonyPrefix]
    public static bool Prefix(GameState state, ref ScreenBase __result)
    {
        if (state is FiefManagementGameState fmState)
        {
            var swapper = _swapper ??= IoC.Resolve<IRemoteFiefSettlementSwapper>();
            __result = new GauntletFiefManagementScreen(fmState, swapper);
            return false;
        }
        return true;
    }
}
