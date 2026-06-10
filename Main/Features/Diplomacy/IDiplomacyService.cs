using DOTS.Features.Diplomacy.Models;

namespace DOTS.Features.Diplomacy;

public interface IDiplomacyService
{
    AllianceTier GetRelationshipTier(string kingdomAId, string kingdomBId);
    float GetAllianceScoreModifier(string kingdomAId, string kingdomBId);
    bool IsAllianceAllowed(string kingdomAId, string kingdomBId);
    bool IsWarAllowed(string kingdomAId, string kingdomBId);
    void EstablishInitialAlliances();
    void EnforcePermanentAlliances();
}
