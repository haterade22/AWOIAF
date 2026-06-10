namespace DOTS.Features.SettlementNameplateFade;

/// <summary>
/// Production wire-up of <see cref="INameplateFadeSettingsProvider"/> reading from the MCM
/// <c>DotsSettings</c> singleton. Trivial bridge — defensive validation (NaN, ordering, bounds)
/// lives in <see cref="NameplateFadeService"/> so it can be exercised by unit tests without
/// having to spin up MCM. Defaults match the slider defaults in DotsSettings so a missing
/// instance can't accidentally disable the feature.
///
/// HOT PATH NOTE: each property is read up to once per frame per visible settlement nameplate
/// (~3000 calls/sec on a populated map). The <c>DotsSettings.Instance</c> singleton accessor
/// is cached in <see cref="_settings"/> at construction time to avoid 3× static-singleton
/// dereferences per call. The cached reference still picks up live MCM edits because the
/// values are read through the reference, not snapshotted.
/// </summary>
public class NameplateFadeSettingsProvider : INameplateFadeSettingsProvider
{
    private readonly DotsSettings _settings;

    public NameplateFadeSettingsProvider()
    {
        _settings = DotsSettings.Instance;
    }

    public bool Enabled => _settings?.EnableNameplateFade ?? true;
    public float NearDistance => _settings?.NameplateFadeNearDistance ?? 80f;
    public float FarDistance => _settings?.NameplateFadeFarDistance ?? 200f;
}
