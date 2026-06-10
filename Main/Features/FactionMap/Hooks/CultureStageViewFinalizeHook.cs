namespace DOTS.Features.FactionMap.Hooks;

public class CultureStageViewFinalizeHook : IOnCultureStageViewFinalize
{
    public void OnFinalize()
    {
        CultureStageViewCreatedHook.CurrentVM?.OnFinalize();
        CultureStageViewCreatedHook.Cleanup();
    }
}
