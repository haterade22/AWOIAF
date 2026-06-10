using DOTS.Features.Siege.Models;

namespace DOTS.Features.Siege;

public interface ISiegeDefenseConfigProvider
{
    SiegeDefenseConfig LoadConfig();
}
