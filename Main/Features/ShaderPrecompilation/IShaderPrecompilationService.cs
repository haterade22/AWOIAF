using System.Collections.Generic;

namespace DOTS.Features.ShaderPrecompilation;

public interface IShaderPrecompilationService
{
    IReadOnlyList<string> GetCharacterIdsForShaderBattle();
    IReadOnlyList<string> GetCultureIdsForShaderBattle();
}
