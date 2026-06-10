using System.Collections.Generic;
using DOTS.Features.NamedCompanions.Domain;

namespace DOTS.Features.NamedCompanions;

public interface INamedCompanionConfigProvider
{
    IReadOnlyList<NamedCompanionDefinition> GetCompanions();
}
