using DOTS.Features.Diplomacy.Models;

namespace DOTS.Features.Diplomacy;

public interface IWarOfTheRingService
{
    WarPhase CurrentPhase { get; }
    bool IsWarOfTheRingActive { get; }
    bool ShouldBlockPeace(string kingdomAId, string kingdomBId);
    void CheckPhaseTransition(float elapsedDays);
    // Phase 9b #129 P1 — SyncData hook so behavior can restore phase across save-load.
    void SetPhaseFromSave(WarPhase phase);
}
