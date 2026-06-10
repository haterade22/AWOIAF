using DOTS.Features.StartupResources.Config;

namespace DOTS.Features.StartupResources;

public interface IStartupResourcesConfigProvider
{
    StartupResourcesConfig LoadConfig();
}
