using System.Collections.Generic;

namespace DOTS.Features.ArmyTargeting;

public class ArmyTargetingConfig
{
    public Dictionary<string, List<string>> FactionPriorityTargets { get; set; } = new();
    public Dictionary<string, float> FactionAggressionMultipliers { get; set; } = new();
    public Dictionary<string, float> FactionDistanceRangeMultipliers { get; set; } = new();
}
