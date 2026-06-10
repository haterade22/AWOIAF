using TaleWorlds.ModuleManager;

namespace DOTS.Core.Infrastructure;

public class ModulePathAdapter : IModulePathAdapter
{
    public string GetModuleFullPath(string moduleName)
    {
        return ModuleHelper.GetModuleFullPath(moduleName);
    }
}
