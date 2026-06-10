namespace DOTS.Features.Diplomacy;

public interface IDotsSettingsProvider
{
    bool IsAvailable { get; }
    bool WarOfTheRingEnabled { get; }
    int Phase1TriggerDay { get; }
    int Phase2TriggerDay { get; }
    bool TestMode { get; }
}
