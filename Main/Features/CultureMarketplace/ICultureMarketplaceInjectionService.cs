using System.Collections.Generic;

namespace DOTS.Features.CultureMarketplace;

public interface ICultureMarketplaceInjectionService
{
    IReadOnlyList<string> SelectItems(string cultureId, int currentRosterCount, System.Random rng);
}
