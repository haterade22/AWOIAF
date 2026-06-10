using DryIoc;
using DOTS.Core.Logging;
using DOTS.Features.ShaderPrecompilation.Hooks;

namespace DOTS.Features.ShaderPrecompilation;

public static class ShaderPrecompilationIoC
{
    public static void RegisterShaderPrecompilationFeature(IContainer container)
    {
        container.Register<IShaderPrecompilationService, ShaderPrecompilationService>(Reuse.Singleton);
    }

    public static void InitializeHooks(IModLogger logger)
    {
        LoadingScreen_ShaderProgress_Patch.Initialize(logger);
    }
}
