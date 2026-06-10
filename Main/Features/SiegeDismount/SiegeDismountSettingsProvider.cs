using DOTS.Features.SiegeDismount.Models;

namespace DOTS.Features.SiegeDismount;

public class SiegeDismountSettingsProvider : ISiegeDismountSettingsProvider
{
    public bool IsEnabled => DotsSettings.Instance?.EnableSiegeDismount ?? true;

    public SiegeMountBehaviorType MountBehavior =>
        (SiegeMountBehaviorType)(DotsSettings.Instance?.SiegeMountBehavior ?? (int)SiegeMountBehaviorType.AutoRemountAfter);

    public bool IsDebugMode => DotsSettings.Instance?.SiegeDismountDebug ?? false;
}
