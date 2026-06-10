namespace DOTS.Features.Diplomacy.Hooks;

public interface IOnAllianceAction
{
    bool ShouldPreventAllianceEnd(string kingdomAId, string kingdomBId);
    bool ShouldPreventWarDeclaration(string factionAId, string factionBId);
}
