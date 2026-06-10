namespace DOTS.Features.Diplomacy.Hooks;

public interface IOnPeaceAction
{
    bool ShouldPreventPeace(string factionAId, string factionBId);
}
