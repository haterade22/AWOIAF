namespace DOTS.Features.TimeAcceleration;

public class TimeAccelerationSettingsProvider : ITimeAccelerationSettingsProvider
{
    public int FastForwardMultiplier => DotsSettings.Instance?.FastForwardMultiplier ?? 4;
    public int ExtraFastForwardMultiplier => DotsSettings.Instance?.ExtraFastForwardMultiplier ?? 8;
    public int CtrlSpaceMultiplier => DotsSettings.Instance?.CtrlSpaceMultiplier ?? 16;
}
