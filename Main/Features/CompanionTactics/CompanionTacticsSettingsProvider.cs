namespace DOTS.Features.CompanionTactics;

/// <summary>
/// Reads CompanionTactics-related properties off <see cref="DotsSettings"/>. Falls back to
/// the documented defaults when the global instance is unavailable (e.g., headless tests
/// without MCM bootstrapped).
/// </summary>
public sealed class CompanionTacticsSettingsProvider : ICompanionTacticsSettingsProvider
{
    public bool EnableCompanionRoleTooltips => DotsSettings.Instance?.EnableCompanionRoleTooltips ?? true;
    public bool EnableOOBRoleDisplay => DotsSettings.Instance?.EnableOOBRoleDisplay ?? true;
    public bool CompanionRolesDebug => DotsSettings.Instance?.CompanionRolesDebug ?? false;

    public bool EnableFormationPresets => DotsSettings.Instance?.EnableFormationPresets ?? true;
    public int MaxFormationPresets => DotsSettings.Instance?.MaxFormationPresets ?? 10;
    public bool FormationPresetsDebug => DotsSettings.Instance?.FormationPresetsDebug ?? false;

    public bool EnableBattleActionBar => DotsSettings.Instance?.EnableBattleActionBar ?? true;
    public bool CancelStanceOnMove => DotsSettings.Instance?.CancelStanceOnMove ?? true;
    public bool EnableVolleyFire => DotsSettings.Instance?.EnableVolleyFire ?? true;
    public bool BattleActionBarDebug => DotsSettings.Instance?.BattleActionBarDebug ?? false;
}
