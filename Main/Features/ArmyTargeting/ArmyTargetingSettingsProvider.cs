namespace DOTS.Features.ArmyTargeting;

public class ArmyTargetingSettingsProvider : IArmyTargetingSettingsProvider
{
    public bool EnableArmyStrategicIntelligence => DotsSettings.Instance?.EnableArmyStrategicIntelligence ?? true;
    public float CommitmentMultiplier           => DotsSettings.Instance?.ArmyCommitmentMultiplier        ?? 4.0f;
    public float MaxPriorityBoost               => DotsSettings.Instance?.ArmyPriorityBoost               ?? 3.0f;
    public float EvilAggressionScale            => DotsSettings.Instance?.EvilFactionAggressionScale      ?? 1.0f;
    public float LongRangePriorityBoostScale    => DotsSettings.Instance?.LongRangePriorityBoostScale     ?? 1.0f;
    public float BorderProximityFloor           => DotsSettings.Instance?.ArmyBorderProximityFloor        ?? 0.15f;
}
