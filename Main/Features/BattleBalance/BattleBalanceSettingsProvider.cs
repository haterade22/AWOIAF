namespace DOTS.Features.BattleBalance;

public class BattleBalanceSettingsProvider : IBattleBalanceSettingsProvider
{
    public bool EnableCustomTroopPower      => DotsSettings.Instance?.EnableCustomTroopPower      ?? true;
    public bool OverrideVanillaTierPower    => DotsSettings.Instance?.OverrideVanillaTierPower    ?? false;
    public float Tier7Power                 => DotsSettings.Instance?.Tier7Power                  ?? 2.91f;
    public float Tier8Power                 => DotsSettings.Instance?.Tier8Power                  ?? 3.26f;
    public float Tier9Power                 => DotsSettings.Instance?.Tier9Power                  ?? 3.61f;
    public float Tier10Power                => DotsSettings.Instance?.Tier10Power                 ?? 3.96f;
    public float HeroMultiplier             => DotsSettings.Instance?.HeroMultiplier              ?? 1.5f;
    public float MountedMultiplier          => DotsSettings.Instance?.MountedMultiplier           ?? 1.2f;

    public bool EnableCustomCasualtyRatios  => DotsSettings.Instance?.EnableCustomCasualtyRatios  ?? true;
    public float PlayerBluntDamageChance    => DotsSettings.Instance?.PlayerBluntDamageChance     ?? 0.30f;
    public float AIBluntDamageChance        => DotsSettings.Instance?.AIBluntDamageChance         ?? 0.10f;
    public bool EnableCulturalSurvivalBonuses => DotsSettings.Instance?.EnableCulturalSurvivalBonuses ?? true;
}
