using TaleWorlds.MountAndBlade;

namespace DOTS.Features.CareerSystem.Abilities;

public class MissionTimeProvider : IMissionTimeProvider
{
    public float CurrentTime => Mission.Current?.CurrentTime ?? 0f;
}
