using DOTS.Features;

namespace DOTS.Features.CultureConversion;

/// <summary>
/// Merges MCM live values (<c>DotsSettings.Instance</c>) over JSON defaults. Mirrors
/// <c>CastleRecruitmentSettingsProvider</c>. <c>DotsSettings.Instance</c> can be null early in
/// startup or if MCM fails — the <c>?? default</c> fallback keeps every read safe. MinLoyaltyToConvert
/// and ConvertPlayerOwnedSettlements are JSON-only (advanced); the rest have MCM knobs.
/// </summary>
public sealed class CultureConversionSettingsProvider : ICultureConversionSettingsProvider
{
    private const int MinHoldDays = 1;
    private const int MaxHoldDays = 100000;

    private readonly CultureConversionConfig _defaults;

    public CultureConversionSettingsProvider(ICultureConversionConfigProvider configProvider)
    {
        _defaults = configProvider.GetConfig();
    }

    public bool IsEnabled => DotsSettings.Instance?.EnableCultureConversion ?? _defaults.Enabled;

    public int RequiredHoldDays =>
        SafeClampInt(DotsSettings.Instance?.CultureConversionHoldDays, _defaults.RequiredHoldDays, MinHoldDays, MaxHoldDays);

    public bool RequireStableLoyalty =>
        DotsSettings.Instance?.CultureConversionRequireStableLoyalty ?? _defaults.RequireStableLoyalty;

    public float MinLoyaltyToConvert => _defaults.MinLoyaltyToConvert;

    public bool ConvertPlayerOwnedSettlements => _defaults.ConvertPlayerOwnedSettlements;

    private static int SafeClampInt(int? value, int defaultValue, int min, int max)
    {
        var v = value ?? defaultValue;
        return v < min ? min : v > max ? max : v;
    }
}
