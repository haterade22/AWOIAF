using DOTS.Features.QuickActions.Models;

namespace DOTS.Features.QuickActions;

public sealed class QuickActionsSettingsProvider : IQuickActionsSettingsProvider
{
    public bool EnableQuickActions => DotsSettings.Instance?.EnableQuickActions ?? true;
    public bool EnableInventorySearch => DotsSettings.Instance?.EnableInventorySearch ?? true;

    public DamagedQualityPreset DamagedPreset =>
        DamagedQualityPresetExtensions.FromDropdownIndex(
            DotsSettings.Instance?.DamagedQualityDropdown?.SelectedIndex ?? 2);

    public float CustomDamagedThreshold => DotsSettings.Instance?.DamagedThreshold ?? -0.20f;
    public bool UseCustomThreshold => DotsSettings.Instance?.UseCustomThreshold ?? false;
    public bool SellDamagedEquipped => DotsSettings.Instance?.SellDamagedEquipped ?? false;
    public bool ExcludeDamagedHorses => DotsSettings.Instance?.ExcludeDamagedHorses ?? true;

    public int LowValueThreshold => DotsSettings.Instance?.LowValueThreshold ?? 100;
    public bool SellLowValueEquipped => DotsSettings.Instance?.SellLowValueEquipped ?? false;
    public bool ExcludeLowValueFood => DotsSettings.Instance?.ExcludeLowValueFood ?? true;
    public bool ExcludeLowValueHorses => DotsSettings.Instance?.ExcludeLowValueHorses ?? true;
    public bool ExcludeLowValueTradeGoods => DotsSettings.Instance?.ExcludeLowValueTradeGoods ?? false;

    public bool ShowConfirmation => DotsSettings.Instance?.QuickActionsShowConfirmation ?? true;
    public bool PlaySounds => DotsSettings.Instance?.QuickActionsPlaySounds ?? true;
    public bool IsDebugMode => DotsSettings.Instance?.QuickActionsDebug ?? false;

    public float ResolveDamagedThreshold() =>
        UseCustomThreshold ? CustomDamagedThreshold : DamagedPreset.ToThreshold();
}
