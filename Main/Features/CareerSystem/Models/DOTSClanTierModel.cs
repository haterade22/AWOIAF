using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using DOTS.Features.CareerSystem.Domain;

namespace DOTS.Features.CareerSystem.Models;

public class DotsClanTierModel : DefaultClanTierModel
{
    private readonly ICareerPassiveService _passiveService;

    public DotsClanTierModel(ICareerPassiveService passiveService)
    {
        _passiveService = passiveService;
    }

    public override int GetCompanionLimit(Clan clan)
    {
        var baseLimit = base.GetCompanionLimit(clan);
        // Phase 9b — defensive `_passiveService == null` guard removed: DryIoc resolves
        // the service unconditionally at SubModule registration, so a null reference
        // here would be a wiring bug, not a runtime state. Closes deferred audit-issue
        // #142 unreachable-null-guard P2.
        var leaderId = clan?.Leader?.StringId;
        if (leaderId == null) return baseLimit;
        var bonus = _passiveService.GetPassiveMagnitude(leaderId, PassiveEffectType.CompanionLimit);
        return baseLimit + (int)bonus;
    }
}
