using DOTS.Features.SiegeDismount.Models;

namespace DOTS.Features.SiegeDismount;

public interface ISiegeDismountSettingsProvider
{
    bool IsEnabled { get; }
    SiegeMountBehaviorType MountBehavior { get; }
    bool IsDebugMode { get; }
}
