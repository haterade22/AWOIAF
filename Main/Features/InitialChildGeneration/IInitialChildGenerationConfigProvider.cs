using DOTS.Features.InitialChildGeneration.Config;

namespace DOTS.Features.InitialChildGeneration;

public interface IInitialChildGenerationConfigProvider
{
    InitialChildGenerationConfig LoadConfig();
}
