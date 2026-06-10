using DOTS.Features.MixedFormations.Models;

namespace DOTS.Features.MixedFormations;

public interface IMixedFormationsSettingsProvider
{
    bool IsEnabled { get; }
    FormationLayoutType DefaultLayout { get; }
    string CycleHotkey { get; }
    bool IsDebugMode { get; }
}
