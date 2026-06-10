using TaleWorlds.CampaignSystem;

namespace DOTS.Features.Siege.Models;

public class ActiveSiegeDefenseEvent
{
    public string SettlementId { get; set; }
    public string DefenderFactionId { get; set; }
    public CampaignTime Deadline { get; set; }
    public bool PlayerAccepted { get; set; }
    public bool RewardClaimed { get; set; }
}
