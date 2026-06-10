using TaleWorlds.MountAndBlade.GauntletUI.Mission.Singleplayer;

namespace DOTS.Features.CompanionTactics.FormationPresets;

/// <summary>
/// Owns the OOBButtonsOverlay GauntletLayer lifecycle + reflection on the OOB UI handler's
/// private <c>_dataSource</c> and <c>_isActive</c> fields. Boundary class — sealed type
/// in signature is allowed because the only callers are Harmony patches.
/// </summary>
public interface IOOBOverlayService
{
    void OnTick(MissionGauntletOrderOfBattleUIHandler handler);
    void Detach();
}
