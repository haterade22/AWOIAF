namespace DOTS.Features.Diplomacy;

public class DotsSettingsProvider : IDotsSettingsProvider
{
    public bool IsAvailable => DotsSettings.Instance != null;
    public bool WarOfTheRingEnabled => DotsSettings.Instance?.WarOfTheRingEnabled ?? true;
    public int Phase1TriggerDay => DotsSettings.Instance?.Phase1TriggerDay ?? 30;
    public int Phase2TriggerDay => DotsSettings.Instance?.Phase2TriggerDay ?? 45;
    public bool TestMode => DotsSettings.Instance?.TestMode ?? false;
}
