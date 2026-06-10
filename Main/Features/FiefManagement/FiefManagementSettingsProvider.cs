namespace DOTS.Features.FiefManagement;

public class FiefManagementSettingsProvider : IFiefManagementSettingsProvider
{
    public bool EnableFiefManagement => DotsSettings.Instance?.EnableFiefManagement ?? true;
    public bool AllowRemoteBuildingQueue => DotsSettings.Instance?.AllowRemoteBuildingQueue ?? true;
    public bool IsDebugMode => DotsSettings.Instance?.FiefManagementDebug ?? false;
}
