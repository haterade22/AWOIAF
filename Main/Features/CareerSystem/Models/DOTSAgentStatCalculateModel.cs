using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using DOTS.Features.CareerSystem.Abilities;
using DOTS.Features.CareerSystem.Domain;

namespace DOTS.Features.CareerSystem.Models;

// Thin boundary per gamemodels.md rule 4. All branching/stat-mutation logic
// lives in ICareerAgentStatService. This file extracts primitives from the sealed Agent
// at the boundary and delegates.
public class DotsAgentStatCalculateModel : SandboxAgentStatCalculateModel
{
    private readonly ICareerPassiveService _passiveService;
    private readonly ICareerAgentStatService _agentStatService;

    public DotsAgentStatCalculateModel(ICareerPassiveService passiveService, ICareerAgentStatService agentStatService)
    {
        _passiveService = passiveService;
        _agentStatService = agentStatService;
    }

    public override float GetEffectiveMaxHealth(Agent agent)
    {
        var baseHealth = base.GetEffectiveMaxHealth(agent);
        if (!agent.IsHero) return baseHealth;
        var heroId = (agent.Character as CharacterObject)?.HeroObject?.StringId;
        if (heroId == null) return baseHealth;
        return baseHealth + _passiveService.GetPassiveMagnitude(heroId, PassiveEffectType.Health);
    }

    public override void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
    {
        base.UpdateAgentStats(agent, agentDrivenProperties);
        _agentStatService.ApplyAgentStatModifiers(
            heroId: (agent.Character as CharacterObject)?.HeroObject?.StringId,
            agentIndex: agent.Index,
            isHuman: agent.IsHuman,
            isHero: agent.IsHero,
            agentDrivenProperties);
    }
}
